using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QTKD.Api.Data;

namespace QTKD.Tests.Support;

// Khởi động API thật trong bộ nhớ, nhưng trỏ sang database kiểm thử.
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString = TestConfig.GetTestConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Ghi đè ConnectionStrings:Default bằng chuỗi kết nối của database kiểm thử
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _connectionString
            });
        });
    }

    // Chạy một lần khi host được tạo: xóa database kiểm thử cũ, chạy lại toàn bộ migration.
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();

        db.Database.EnsureDeleted();
        db.Database.Migrate();

        // Dữ liệu mẫu (HasData) chèn Id cố định nên cần đặt lại bộ đếm Id,
        // nếu không lần INSERT đầu tiên sẽ trùng khóa chính.
        db.Database.ExecuteSqlRaw(@"SELECT setval(pg_get_serial_sequence('""Customers""', 'Id'), (SELECT MAX(""Id"") FROM ""Customers""))");
        db.Database.ExecuteSqlRaw(@"SELECT setval(pg_get_serial_sequence('""Contracts""', 'Id'), (SELECT MAX(""Id"") FROM ""Contracts""))");
        db.Database.ExecuteSqlRaw(@"SELECT setval(pg_get_serial_sequence('""PriceLists""', 'Id'), (SELECT MAX(""Id"") FROM ""PriceLists""))");

        return host;
    }
}
