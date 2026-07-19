using AlahiaBackup;
using AlahiaBackup.Configuration;
using AlahiaBackup.Interfaces;
using AlahiaBackup.Services;


var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.Configure<BackupSettings>(
    builder.Configuration.GetSection("Backup"));

builder.Services.AddSingleton<IBackupService, BackupService>();
builder.Services.AddSingleton<ISqlBackupService, SqlBackupService>();
var host = builder.Build();
host.Run();
