using Microsoft.Extensions.Configuration;
using Npgsql;

namespace QTKD.Tests.Support;

public static class TestConfig
{
    // Lấy chuỗi kết nối của database KIỂM THỬ từ user-secrets của project test
    // (hoặc biến môi trường ConnectionStrings__Test). Không bao giờ lưu mật khẩu trong code.
    public static string GetTestConnectionString()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets(typeof(TestConfig).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("Test")
            ?? throw new InvalidOperationException(
                "Thiếu ConnectionStrings:Test. Hãy chạy: dotnet user-secrets set \"ConnectionStrings:Test\" " +
                "\"Host=localhost;Port=5432;Database=qtkd_test;Username=postgres;Password=...\" --project tests/QTKD.Tests");

        // Bảo vệ: bộ test sẽ XÓA và tạo lại database này, nên tên database bắt buộc chứa chữ "test".
        var databaseName = new NpgsqlConnectionStringBuilder(connectionString).Database ?? string.Empty;
        if (!databaseName.Contains("test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Database '{databaseName}' không phải database kiểm thử (tên phải chứa 'test'). " +
                "Bộ test từ chối chạy để tránh xóa nhầm dữ liệu thật.");
        }

        return connectionString;
    }
}
