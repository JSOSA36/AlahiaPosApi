using AlahiaPos.DataAccess.Seguridad;
using AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto;
using AlahiaPos.DataAccess.Servicios.FiscalGateway.Transmission;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DgiiDirectoSettings>(opts =>
{
    builder.Configuration.GetSection("DgiiDirecto").Bind(opts);
    var dgii = builder.Configuration.GetSection("DGII");
    if (string.IsNullOrWhiteSpace(opts.P12Path))
        opts.P12Path = dgii["P12Path"];
    if (string.IsNullOrWhiteSpace(opts.P12Password))
        opts.P12Password = dgii["P12Password"];
    opts.PreferSettingsCertificate = false;
});

var connectionString = builder.Configuration.GetConnectionString("Default");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<AlahiaPos.DataAccess.Data.AlahiaPosContext>(options =>
    {
        options.UseSqlServer(connectionString);
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    });
}

builder.Services.Configure<Alahia.eCF.Api.Security.AlahiaEcfApiSettings>(
    builder.Configuration.GetSection("AlahiaEcfApi"));

builder.Services.AddScoped<IECFSigner, EcfSigner>();
builder.Services.AddScoped<DgiiCertificadoResolver>(sp =>
    new DgiiCertificadoResolver(
        sp.GetRequiredService<IOptions<DgiiDirectoSettings>>(),
        sp.GetRequiredService<ILogger<DgiiCertificadoResolver>>(),
        sp.GetService<AlahiaPos.DataAccess.Data.AlahiaPosContext>()));
builder.Services.AddScoped<DgiiXmlBuilder>();
builder.Services.AddScoped<DgiiRfceBuilder>();
builder.Services.AddHttpClient<DgiiAuthService>();
builder.Services.AddHttpClient<DgiiRecepcionClient>();
builder.Services.AddScoped<DgiiDirectoGateway>();
builder.Services.AddScoped<IFiscalGateway>(sp => sp.GetRequiredService<DgiiDirectoGateway>());
builder.Services.AddSingleton<IFiscalDocumentoValidator, AlahiaPos.DataAccess.Servicios.FiscalGateway.FiscalDocumentoValidator>();
builder.Services.AddAlahiaTransmissionEngine(builder.Configuration);
builder.Services.AddHostedService<Alahia.eCF.Api.Services.TransmissionWorkerHostedService>();
builder.Services.AddScoped<Alahia.eCF.Api.Services.ReceiptOrchestrator>();
builder.Services.AddScoped<AlahiaPos.DataAccess.Servicios.FacturacionElectronica.ICertecfAcecfSender,
    AlahiaPos.DataAccess.Servicios.FacturacionElectronica.CertecfAcecfDirectSender>();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        o.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Alahia e-CF API (DGII Directo)", Version = "v1" });
    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "API Key en header X-Api-Key (mismo patrón que PG.eInvoicing)",
        Name = "X-Api-Key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<Alahia.eCF.Api.Security.ApiKeyMiddleware>();
app.MapControllers();
app.Run();
