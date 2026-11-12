# QTKD - Phase 2 (tuần 05-07)

Web API quản lý khách hàng, hợp đồng và bảng giá cho hệ thống QTKD.
Công nghệ: C#, ASP.NET Core Web API (.NET 9), Entity Framework Core, PostgreSQL, xUnit.

## Yêu cầu

- .NET SDK 9
- PostgreSQL (đã tạo sẵn user và biết mật khẩu, cổng mặc định 5432)
- Công cụ `dotnet-ef`: `dotnet tool install --global dotnet-ef`

## 1. Cấu hình database

Chuỗi kết nối không được commit lên Git. Đặt bằng user-secrets (thay `MAT_KHAU` và cổng cho đúng máy của bạn):

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=qtkd;Username=postgres;Password=MAT_KHAU" --project src/QTKD.Api
```

Database `qtkd` sẽ được tự tạo khi chạy migration.

## 2. Chạy migration

```bash
dotnet ef database update --project src/QTKD.Api
```

Migration tạo các bảng và dữ liệu mẫu: 2 Customer, 1 Contract `HD-2026-018` ở trạng thái `Draft`, 1 PriceList có 2 PriceItem.

## 3. Khởi động API

```bash
dotnet run --project src/QTKD.Api
```

Xem địa chỉ trong console (dòng `Now listening on: ...`). Mở Swagger tại `<địa chỉ>/swagger`.

## 4. Thử hai endpoint

| Endpoint | Kết quả mong đợi |
|---|---|
| `GET /api/customers` | 200, danh sách 2 khách hàng |
| `GET /api/contracts/1` | 200, hợp đồng `HD-2026-018`, trạng thái `Draft` |
| `GET /api/contracts/999` | 404, thông báo không tìm thấy |

Đối chiếu kết quả với bảng `Customers` và `Contracts` trong PostgreSQL (database `qtkd`).

## 5. Chạy test

Test dùng một database **riêng** (`qtkd_test`). Bộ test xóa và tạo lại database này mỗi lần chạy, nên tên database bắt buộc chứa chữ `test`.

```bash
dotnet user-secrets init --project tests/QTKD.Tests
dotnet user-secrets set "ConnectionStrings:Test" "Host=localhost;Port=5432;Database=qtkd_test;Username=postgres;Password=MAT_KHAU" --project tests/QTKD.Tests
dotnet test
```

- Không cần tạo trước database `qtkd_test`, bộ test tự tạo bằng migration.
- Đóng các cửa sổ pgAdmin đang kết nối tới `qtkd_test` trước khi chạy, nếu không PostgreSQL không cho xóa database.
- Chi tiết các test và kết quả: [docs/test-report.md](docs/test-report.md).

## Demo từ đầu (clone, cấu hình, migration, chạy API, chạy test)

```bash
git clone <link repository> && cd QTKD
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=qtkd;Username=postgres;Password=MAT_KHAU" --project src/QTKD.Api
dotnet ef database update --project src/QTKD.Api
dotnet run --project src/QTKD.Api
dotnet user-secrets set "ConnectionStrings:Test" "Host=localhost;Port=5432;Database=qtkd_test;Username=postgres;Password=MAT_KHAU" --project tests/QTKD.Tests
dotnet test
```

## Cấu trúc thư mục

```
src/QTKD.Api/   Controllers, Services, Data, Entities, Dtos
tests/QTKD.Tests/  unit test và integration test (xUnit)
docs/           tài liệu thiết kế
```

## Tài liệu

- [Ghi chú thiết kế, giả định, thay đổi so với Phase 1](docs/design-notes.md)
- [Các thành phần của solution](docs/solution-overview.md)
- [Tài liệu API](docs/api.md)
- [Báo cáo kiểm thử](docs/test-report.md)
