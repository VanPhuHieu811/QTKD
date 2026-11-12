using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QTKD.Api.Data;
using QTKD.Api.Dtos;
using QTKD.Api.Entities;

namespace QTKD.Tests.Support;

public abstract class IntegrationTestBase
{
    protected ApiFactory Factory { get; }
    protected HttpClient Client { get; }

    protected IntegrationTestBase(ApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
    }

    // ---------- HTTP ----------
    protected Task<HttpResponseMessage> PostAsync<T>(string url, T body) => Client.PostAsJsonAsync(url, body, TestJson.Options);

    protected Task<HttpResponseMessage> PutAsync<T>(string url, T body) => Client.PutAsJsonAsync(url, body, TestJson.Options);

    protected Task<HttpResponseMessage> SubmitAsync(int contractId) => Client.PostAsync($"/api/contracts/{contractId}/submit", null);

    protected static Task<T?> ReadAsync<T>(HttpResponseMessage response) => response.Content.ReadFromJsonAsync<T>(TestJson.Options);

    // ---------- Truy cập database kiểm thử để kiểm tra dữ liệu thật sau khi gọi API ----------
    protected async Task<T> WithDbAsync<T>(Func<QtkdDbContext, Task<T>> action)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
        return await action(db);
    }

    protected Task<ContractStatus> GetStatusFromDbAsync(int contractId) =>
        WithDbAsync(db => db.Contracts.AsNoTracking().Where(c => c.Id == contractId).Select(c => c.Status).SingleAsync());

    protected Task<int> CountApprovalsAsync(int contractId) =>
        WithDbAsync(db => db.ApprovalRequests.CountAsync(a => a.ContractId == contractId));

    protected Task<int> CountHistoriesAsync(int contractId) =>
        WithDbAsync(db => db.ContractHistories.CountAsync(h => h.ContractId == contractId));

    // ---------- Dữ liệu dùng chung ----------
    protected static string NewContractNo() => "HD-" + Guid.NewGuid().ToString("N")[..12];

    protected async Task<CustomerDto> CreateCustomerAsync(string name = "Công ty Test", CustomerStatus? status = null)
    {
        var response = await PostAsync("/api/customers", new CreateCustomerRequest(name, status));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadAsync<CustomerDto>(response))!;
    }

    // Hợp đồng Draft mới tạo: có ServiceGroup nhưng CHƯA có Content và CHƯA có tệp đính kèm
    protected async Task<ContractDto> CreateContractAsync(int customerId)
    {
        var request = new CreateContractRequest(
            NewContractNo(), customerId,
            new DateOnly(2026, 10, 1), new DateOnly(2027, 9, 30),
            "Dịch vụ cảng", null);

        var response = await PostAsync("/api/contracts", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadAsync<ContractDto>(response))!;
    }

    protected static UpdateContractRequest ToUpdateRequest(ContractDto contract, string? content) =>
        new(contract.CustomerId, contract.StartDate, contract.EndDate, contract.ServiceGroup, content);

    // Hợp đồng Draft đủ điều kiện CTR-02: khách hàng Active, ngày hợp lệ, có nội dung, có 1 tệp
    protected async Task<ContractDto> CreateReadyContractAsync()
    {
        var customer = await CreateCustomerAsync();
        var contract = await CreateContractAsync(customer.Id);

        var update = await PutAsync($"/api/contracts/{contract.Id}", ToUpdateRequest(contract, "Nội dung hợp đồng test"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var attach = await PostAsync($"/api/contracts/{contract.Id}/attachments", new AddAttachmentRequest("hop-dong.pdf"));
        Assert.Equal(HttpStatusCode.OK, attach.StatusCode);

        return (await ReadAsync<ContractDto>(attach))!;
    }
}
