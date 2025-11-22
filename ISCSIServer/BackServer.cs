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
                logger.LogDebug("Starting iSCSI Server at {IPAddress}:{Port}",serverOption.IPAddress,serverOption.Port);
                ISCSIServer server = new ISCSIServer();
                List<ISCSITarget> targets = new List<ISCSITarget>();

                foreach (var targetOption in serverOption.Target)
                {
                    logger.LogDebug("Adding Target {TargetName}",targetOption.TargetName);
                    List<Disk> disks = new List<Disk>();
                    foreach (var disk in targetOption.Disk)
                    {
                        logger.LogDebug("Adding Disk {DiskPath}",disk.Path);
                        string diskPath = disk.Path;
                        if(!System.IO.Path.Exists(diskPath))
                        {
                            if(!CreateDisk(disk))
                            {
                                logger.LogError("Disk {diskPath} creation failed.",diskPath);
                                continue;
                            }
                        }
                        Disk diskImage = DiskImage.GetDiskImage(diskPath,disk.ReadOnly);
                        disks.Add(diskImage);
                    }

                    ISCSITarget target = new ISCSITarget(targetOption.TargetName, disks);
                    if(targetOption.Initiator.Count > 0)
                    {
                        target.OnStandardInquiry += new EventHandler<StandardInquiryEventArgs>(OnStandardInquiry);
                        target.OnUnitSerialNumberInquiry += new EventHandler<UnitSerialNumberInquiryEventArgs>(OnUnitSerialNumberInquiry);
                        target.OnDeviceIdentificationInquiry += new EventHandler<DeviceIdentificationInquiryEventArgs>(OnDeviceIdentificationInquiry);
                        target.OnAuthorizationRequest += new EventHandler<AuthorizationRequestArgs>(OnAuthorizationRequest);
                    }
                    
                    targets.Add(target);
                }
                server.AddTargets(targets);
                server.Start(new IPEndPoint(IPAddress.Parse(serverOption.IPAddress), serverOption.Port));
                _servers.Add(server);

            }
            logger.LogInformation("iSCSI Servers started.");
            return Task.CompletedTask;
        }

        private void OnDeviceIdentificationInquiry(object? sender, DeviceIdentificationInquiryEventArgs e)
        {
           
        }

        private void OnUnitSerialNumberInquiry(object? sender, UnitSerialNumberInquiryEventArgs e)
        {
            
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
