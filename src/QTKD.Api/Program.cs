using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using QTKD.Api.Data;
using QTKD.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Chuỗi kết nối lấy từ user-secrets (Development) hoặc biến môi trường, không commit mật khẩu
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Thiếu ConnectionStrings:Default. Hãy cấu hình bằng dotnet user-secrets.");

builder.Services.AddDbContext<QtkdDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<ContractService>();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // Trả Status dạng chữ ("Draft") thay vì số
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
