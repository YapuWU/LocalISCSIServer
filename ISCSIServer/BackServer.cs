using DiskAccessLibrary;
using ISCSI.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SCSI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DiskServer
{
    internal class BackServer(ILogger<BackServer> logger,
        IOptions<SCSIOption> options) : IHostedService

    {

        private List<ISCSI.Server.ISCSIServer> _servers = new List<ISCSIServer>();

        public Task StartAsync(CancellationToken cancellationToken)
        {
            SCSIOption scsiOption = options.Value;
            logger.LogInformation("Starting iSCSI Servers...");
            foreach (var serverOption in scsiOption.Servers)
            {
                var server = StartAServer(serverOption);
                if(server!=null)
                {
                    _servers.Add(server);
                }
            }
            logger.LogInformation("iSCSI Servers started.");
            return Task.CompletedTask;
        }

       

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping iSCSI Servers...");
            foreach (var server in _servers)
            {
                server.Stop();
            }
            logger.LogInformation("iSCSI Servers stopped.");
            return Task.CompletedTask;
        }
        private ISCSIServer? StartAServer(ServerOption serverOption)
        {
            ISCSIServer? server = null;
            List<ISCSITarget> targets = new();
            logger.LogInformation("Staring  ISCSIServer at {ip}:{port}", serverOption.IPAddress, serverOption.Port);
            foreach (var target in serverOption.Target)
            {
                var t = StartATarget(target);
                if(t != null)
                {
                    targets.Add(t);
                }
            }
            if(targets.Count > 0)
            {
                server = new ISCSIServer();
                server.AddTargets(targets);
                server.Start(new IPEndPoint(IPAddress.Parse(serverOption.IPAddress), serverOption.Port));
            }

            return server;
        }
        private ISCSITarget? StartATarget(TargetOption targetOption)
        {
            ISCSITarget? target = null;
            List<Disk> disks = new();

            logger.LogInformation("Starting ISCSITarget {targetname}", targetOption.TargetName);
            foreach (var diskOption in targetOption.Disk)
            {
                var disk = StartADisk(diskOption);
                if(disk != null)
                {
                    disks.Add(disk);
                }
            }
            if(disks.Count > 0)
            {
                target = new ISCSITarget(targetOption.TargetName, disks);
                target.OnStandardInquiry += new EventHandler<StandardInquiryEventArgs>(OnStandardInquiry);
                target.OnUnitSerialNumberInquiry += new EventHandler<UnitSerialNumberInquiryEventArgs>(OnUnitSerialNumberInquiry);
                target.OnDeviceIdentificationInquiry += new EventHandler<DeviceIdentificationInquiryEventArgs>(OnDeviceIdentificationInquiry);
                if (targetOption.Initiator.Count > 0)
                {
                    target.OnAuthorizationRequest += new EventHandler<AuthorizationRequestArgs>(OnAuthorizationRequest);
                }
            }
            return target;
        }
        private Disk? StartADisk(DiskOption diskOption)
        {
            string diskPath = diskOption.Path;
            logger.LogInformation("Starting disk {disk}", diskPath);
            if (!System.IO.Path.Exists(diskPath))
            {
                if (!CreateDisk(diskOption))
                {
                    logger.LogError("Disk {diskPath} creation failed.", diskPath);
                    return null;
                }
            }
            Disk diskImage = DiskImage.GetDiskImage(diskPath, diskOption.ReadOnly);

            return diskImage;
        }
        private bool CreateDisk(DiskOption disk)
        {
            if(!disk.AutoCreate)
            {
                return false;
            }
            try
            {
                long diskSizeInBytes = disk.GetDiskSizeInBytes();
                if (disk.Path.EndsWith(".vhd", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (disk.Dynamic)
                    {
                        VirtualHardDisk.CreateDynamicDisk(disk.Path, diskSizeInBytes);
                    }
                    else
                    {
                        VirtualHardDisk.CreateFixedDisk(disk.Path, diskSizeInBytes);
                    }
                }
                else if (disk.Path.EndsWith(".vmdk", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (disk.Dynamic)
                    {
                        VirtualMachineDisk.CreateMonolithicSparse(disk.Path, diskSizeInBytes);
                    }
                    else
                    {
                        VirtualMachineDisk.CreateMonolithicFlat(disk.Path, diskSizeInBytes);
                    }
                }
                else
                {
                    RawDiskImage.Create(disk.Path, diskSizeInBytes);
                }
            }
            catch(Exception )
            {                
                return false;
            }
            return true;
        }
        private void OnStandardInquiry(object? sender, StandardInquiryEventArgs args)
        {
            string targetName = ((ISCSITarget)sender!).TargetName;
            SCSIOption scsiOption = options.Value;
            var a = from server in scsiOption.Servers
                    from target in server.Target
                    where target.TargetName == targetName
                    select target.VendorIdentification;
            var vendorID = a.FirstOrDefault();
            if(!string.IsNullOrEmpty(vendorID))
            {
                args.Data.VendorIdentification = vendorID;
            }
        }
        private void OnDeviceIdentificationInquiry(object? sender, DeviceIdentificationInquiryEventArgs e)
        {

        }

        private void OnUnitSerialNumberInquiry(object? sender, UnitSerialNumberInquiryEventArgs e)
        {
            string targetName = ((ISCSITarget)sender!).TargetName;
            SCSIOption scsiOption = options.Value;
            var a = from server in scsiOption.Servers
                    from target in server.Target
                    where target.TargetName == targetName
                    from disk in target.Disk
                    where  disk.Name == e.LUN.SingleLevelLUN
                    select disk.SerialNumber;
            var serialNumber = a.FirstOrDefault();
            if (!string.IsNullOrEmpty(serialNumber))
            {
                e.Page.ProductSerialNumber   = serialNumber;
            }
        }
        private void OnAuthorizationRequest(object? sender, AuthorizationRequestArgs e)
        {
            SCSIOption scsiOption = options.Value;
            string targetName = ((ISCSITarget)sender!).TargetName;

            var a = from server in scsiOption.Servers
                    from target in server.Target
                    where target.TargetName == targetName
                    from initiator in target.Initiator
                    where initiator == e.InitiatorName
                    select initiator;


            var targetOption = a.FirstOrDefault();
            if (targetOption == null)
            {
                e.Accept = false;
                return;
            }
            e.Accept = true;
        }

        private void OnTextRequest(object? sender, TextRequestArgs e)
        {
        }
    }
}
