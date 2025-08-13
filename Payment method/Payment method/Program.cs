using Microsoft.EntityFrameworkCore;
using Payment_method.Data;
using Payment_method.Services;

var builder = WebApplication.CreateBuilder(args);

// Register DbContext using connection string from appsettings.json
builder.Services.AddDbContext<Payment_methodDb>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default"))
);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for React frontend
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:3000", "http://127.0.0.1:3000",
                  "http://localhost:3001", "http://127.0.0.1:3001")
     .AllowAnyHeader()
     .AllowAnyMethod()
));
 // make sure this using exists

builder.Services.AddScoped<IPaymentService, PaymentService>();

// Build the app
var app = builder.Build();

// Auto apply migrations (creates tables if missing)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Payment_methodDb>();
    db.Database.Migrate();
}

// Middleware
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
