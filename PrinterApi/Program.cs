using Microsoft.Extensions.Hosting.WindowsServices;
using PrinterApi;
using PrinterApi.Interfaz;
using PrinterApi.Servicios;

var contentRoot = AppContext.BaseDirectory;

using var mutex = new Mutex(true, @"Global\AlahiaPrinterApi", out var createdNew);
if (!createdNew)
{
    return;
}

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = contentRoot,
    ApplicationName = "AlahiaPrinterApi"
});

var programDataConfig = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "Alahia",
    "PrinterApi",
    "appsettings.Local.json");
builder.Configuration.AddJsonFile(programDataConfig, optional: true, reloadOnChange: true);

if (WindowsServiceHelpers.IsWindowsService())
{
    builder.Host.UseWindowsService(options =>
    {
        options.ServiceName = PrinterAgentInfo.ServiceName;
    });
}

builder.Services.AddControllers();
builder.Services.AddSingleton<IPrinterLocalSettings, PrinterLocalSettings>();
builder.Services.AddHttpClient<PrinterTicketServices>();
builder.Services.AddHttpClient("PrinterUpdate", client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
});
builder.Services.AddScoped<IPrinterTicket, PrinterTicketServices>();
builder.Services.AddHostedService<PrinterUpdateChecker>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("PolicyConfig", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var port = builder.Configuration.GetValue("Host:Port", PrinterAgentInfo.DefaultPort);
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

app.Use(async (ctx, next) =>
{
    if (HttpMethods.IsOptions(ctx.Request.Method)
        && ctx.Request.Headers.ContainsKey("Access-Control-Request-Private-Network"))
    {
        ctx.Response.Headers["Access-Control-Allow-Private-Network"] = "true";
    }

    ctx.Response.OnStarting(() =>
    {
        if (!ctx.Response.Headers.ContainsKey("Access-Control-Allow-Private-Network"))
        {
            ctx.Response.Headers["Access-Control-Allow-Private-Network"] = "true";
        }
        return Task.CompletedTask;
    });

    await next();
});

app.UseCors("PolicyConfig");
app.UseAuthorization();
app.MapControllers();

app.Run();
