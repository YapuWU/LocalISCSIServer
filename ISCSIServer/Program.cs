using DiskServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

HostApplicationBuilder host = Host.CreateApplicationBuilder();


host.Services.AddWindowsService();
host.Services.AddSystemd();
//host.Configuration.AddXmlFile("Config.xml");

host.Services.AddOptions<SCSIOption>()
    .Bind(host.Configuration.GetSection("ISCSI"));
var a = host.Configuration.GetSection("ISCSI");
host.Services.AddHostedService<BackServer>();



var app = host.Build();


await app.RunAsync();