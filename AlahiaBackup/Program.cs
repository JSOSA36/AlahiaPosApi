using AlahiaBackup;
using AlahiaBackup.Configuration;
using AlahiaBackup.Interfaces;
using AlahiaBackup.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "AlahiaBackup";
});

builder.Services.Configure<BackupSettings>(builder.Configuration.GetSection("Backup"));
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddSingleton<IBackupService, BackupService>();
builder.Services.AddSingleton<ISqlBackupService, SqlBackupService>();
builder.Services.AddSingleton<IBackupEmailService, BackupEmailService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
