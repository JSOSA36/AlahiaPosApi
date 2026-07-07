using PrinterApi.Interfaz;

var builder = WebApplication.CreateBuilder(args);

// =============================
// 🔹 SERVICES
// =============================
builder.Services.AddControllers();
builder.Services.AddHttpClient<PrinterTicketServices>();
builder.Services.AddScoped<IPrinterTicket, PrinterTicketServices>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =============================
// 🔥 CORS (CLAVE)
// =============================
builder.Services.AddCors(options =>
{
    options.AddPolicy("PolicyConfig", policy =>
    {
        policy
        .SetIsOriginAllowed(_ => true)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials(); // 🔥 ESTA ES LA CLAVE
    });
});
builder.WebHost.UseUrls(
    "http://0.0.0.0:5045"
);
var app = builder.Build();

// =============================
// 🔹 DEV
// =============================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// =============================
// 🔥 PIPELINE (ORDEN IMPORTA)
// =============================

// ❌ QUITAMOS ESTO (IMPORTANTE)
// app.UseHttpsRedirection();

app.UseRouting();

// 🔥 ACTIVAR CORS
app.UseCors("PolicyConfig");

app.UseAuthorization();

app.MapControllers();

app.Run();