using AlahiaPos.DataAccess.Data;
using AlahiaPos.DataAccess.Repository;
using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using PrinterServices;
using PrinterServices.Interfaces;
using PrinterServices.Servicios;

var builder = Host.CreateApplicationBuilder(args);

// 🔥 conexión DB
builder.Services.AddDbContext<AlahiaPosContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"));
});

// 🔥 repositorio genérico
builder.Services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));

// 🔥 servicio que usa el printer
builder.Services.AddScoped<IFacturaHeaderServices>();

// 🔥 printer
builder.Services.AddScoped<IPrinter, PrinterTicketServices>();

// 🔥 worker loop
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();