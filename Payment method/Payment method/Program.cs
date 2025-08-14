using Microsoft.EntityFrameworkCore;
using Payment_method.Data;
using Payment_method.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["Key"]!)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// ---- Swagger add JWT “Authorize” button ----
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Payments API", Version = "v1" });
    var jwtScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter JWT token only (without Bearer prefix)."
    };
    c.AddSecurityDefinition("Bearer", jwtScheme);
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { jwtScheme, Array.Empty<string>() }
    });
});

var app = builder.Build();

// auto-migrate (you already have this)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Payment_method.Data.Payment_methodDb>();
    db.Database.Migrate();
}
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
// Auto apply migrations (creates tables if missing)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Payment_methodDb>();
    db.Database.Migrate();
}

// Middleware
app.UseAuthentication();
app.UseAuthorization();
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
