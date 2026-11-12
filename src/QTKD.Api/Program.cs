using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using QTKD.Api.Data;
using QTKD.Api.Middleware;
using QTKD.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Chuỗi kết nối được đọc lúc tạo DbContext (không đọc sớm), để project test có thể ghi đè
// sang database kiểm thử. Mật khẩu lấy từ user-secrets hoặc biến môi trường, không commit lên Git.
builder.Services.AddDbContext<QtkdDbContext>((serviceProvider, options) =>
{
    var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Default")
        ?? throw new InvalidOperationException("Thiếu ConnectionStrings:Default. Hãy cấu hình bằng dotnet user-secrets.");
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<ContractService>();
builder.Services.AddScoped<PriceListService>();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // Nhận và trả Status dạng chữ ("Draft") thay vì số
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware bắt lỗi nghiệp vụ, đặt đầu tiên để bao toàn bộ pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

// Cho phép project test dùng WebApplicationFactory<Program>
public partial class Program { }
