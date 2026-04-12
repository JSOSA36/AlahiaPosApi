using AlahiaPos.DataAccess.Data;
using AlahiaPos.DGII.Sync;
using AlahiaPos.DGII.Sync.Interfaces;
using AlahiaPos.DGII.Sync.Servicios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

// 🔥 DB CONTEXT
builder.Services.AddDbContext<AlahiaPosContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"))
);

// 🔥 HTTP CLIENT
builder.Services.AddHttpClient<IDgiiDownloader, DgiiDownloader>();
// 🔥 SERVICIOS DGII
builder.Services.AddScoped<IDgiiExtractor, DgiiExtractor>();
builder.Services.AddScoped<IDgiiProcessor, DgiiProcessor>();
builder.Services.AddScoped<IDgiiSyncService, DgiiSyncService>();

// 🔥 WORKER
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();