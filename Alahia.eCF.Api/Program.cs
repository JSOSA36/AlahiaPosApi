using Alahia.eCF.Api.Interfaces;
using Alahia.eCF.Api.Services;
using Alahia.eCF.Api.Setting;

var builder = WebApplication.CreateBuilder(args);

// 🔥 CONFIGURACIÓN DGII
builder.Services.Configure<DgiiSettings>(
    builder.Configuration.GetSection("DGII")
);

// 🔥 SERVICIOS CORE
builder.Services.AddScoped<IExcelMapperService, ExcelMapperService>();

builder.Services.AddScoped<IXmlGeneratorService, XmlGeneratorService>();

builder.Services.AddScoped<IXmlFirmaService, XmlFirmaService>();

builder.Services.AddHttpClient<IDgiiService, DgiiService>();

builder.Services.AddScoped<IEcfService, EcfService>();

// 🔥 CONTROLADORES
builder.Services.AddControllers();

// 🔥 SWAGGER
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 🔥 PIPELINE
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();