using System.Net;
using Microsoft.EntityFrameworkCore;
using QTKD.Api.Dtos;
using QTKD.Api.Entities;
using QTKD.Api.Middleware;
using QTKD.Tests.Support;

namespace QTKD.Tests.Integration;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class CustomerApiTests : IntegrationTestBase
{
    public CustomerApiTests(ApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task TC01_CreateValidCustomer_IsCreatedAndReadableFromDatabase()
    {
        var name = "Công ty TC01 " + Guid.NewGuid().ToString("N")[..6];

        var response = await PostAsync("/api/customers", new CreateCustomerRequest(name, null));

        // 1. HTTP status
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // 2. Nội dung trả về
        var created = await ReadAsync<CustomerDto>(response);
        Assert.NotNull(created);
        Assert.True(created!.Id > 0);
        Assert.Equal(name, created.Name);
        Assert.Equal(CustomerStatus.Active, created.Status);

        // 3. Đọc lại bằng API
        var read = await Client.GetAsync($"/api/customers/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(name, (await ReadAsync<CustomerDto>(read))!.Name);

        // 4. Dữ liệu thật trong database
        var inDb = await WithDbAsync(db => db.Customers.AsNoTracking().SingleAsync(c => c.Id == created.Id));
        Assert.Equal(name, inDb.Name);
        Assert.Equal(CustomerStatus.Active, inDb.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TC02_CreateCustomerWithoutName_IsRejected_AndNothingIsSaved(string? name)
    {
        var countBefore = await WithDbAsync(db => db.Customers.CountAsync());

        var response = await PostAsync("/api/customers", new CreateCustomerRequest(name, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadAsync<ErrorResponse>(response);
        Assert.Equal("VALIDATION_FAILED", error!.Code);
        Assert.Contains("Name là bắt buộc.", error.Errors);

        var countAfter = await WithDbAsync(db => db.Customers.CountAsync());
        Assert.Equal(countBefore, countAfter);
    }

    [Fact]
    public async Task GetCustomer_NotFound_Returns404()
    {
        var response = await Client.GetAsync("/api/customers/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("NOT_FOUND", (await ReadAsync<ErrorResponse>(response))!.Code);
    }
}
