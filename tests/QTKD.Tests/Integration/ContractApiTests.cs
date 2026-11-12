using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using QTKD.Api.Dtos;
using QTKD.Api.Entities;
using QTKD.Api.Middleware;
using QTKD.Tests.Support;

namespace QTKD.Tests.Integration;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class ContractApiTests : IntegrationTestBase
{
    public ContractApiTests(ApiFactory factory) : base(factory)
    {
    }

    // ---------------------------------------------------------------- TC03
    [Fact]
    public async Task TC03_UpdateDraftContract_Succeeds()
    {
        var customer = await CreateCustomerAsync();
        var contract = await CreateContractAsync(customer.Id);
        Assert.Equal(ContractStatus.Draft, contract.Status);

        var response = await PutAsync($"/api/contracts/{contract.Id}",
            ToUpdateRequest(contract, "Nội dung đã cập nhật"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Nội dung đã cập nhật", (await ReadAsync<ContractDto>(response))!.Content);

        var contentInDb = await WithDbAsync(db =>
            db.Contracts.AsNoTracking().Where(c => c.Id == contract.Id).Select(c => c.Content).SingleAsync());
        Assert.Equal("Nội dung đã cập nhật", contentInDb);
        Assert.Equal(ContractStatus.Draft, await GetStatusFromDbAsync(contract.Id));
    }

    // ---------------------------------------------------------------- TC04
    [Fact]
    public async Task TC04_UpdatePendingApprovalContract_IsRejected_AndDataUnchanged()
    {
        var contract = await CreateReadyContractAsync();
        var submit = await SubmitAsync(contract.Id);
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

        var response = await PutAsync($"/api/contracts/{contract.Id}",
            ToUpdateRequest(contract, "Nội dung bị sửa trái phép"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("CTR-01", (await ReadAsync<ErrorResponse>(response))!.Code);

        // Dữ liệu giữ nguyên trong database
        var contentInDb = await WithDbAsync(db =>
            db.Contracts.AsNoTracking().Where(c => c.Id == contract.Id).Select(c => c.Content).SingleAsync());
        Assert.Equal("Nội dung hợp đồng test", contentInDb);
        Assert.Equal(ContractStatus.PendingApproval, await GetStatusFromDbAsync(contract.Id));
    }

    [Fact]
    public async Task Ctr01_AddAttachmentToPendingApprovalContract_IsRejected()
    {
        var contract = await CreateReadyContractAsync();
        Assert.Equal(HttpStatusCode.OK, (await SubmitAsync(contract.Id)).StatusCode);

        var response = await PostAsync($"/api/contracts/{contract.Id}/attachments", new AddAttachmentRequest("them-sau-khi-gui.pdf"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("CTR-01", (await ReadAsync<ErrorResponse>(response))!.Code);

        var attachmentCount = await WithDbAsync(db => db.ContractAttachments.CountAsync(a => a.ContractId == contract.Id));
        Assert.Equal(1, attachmentCount);
    }

    // ---------------------------------------------------------------- TC05
    [Fact]
    public async Task TC05_SubmitContractMissingCtr02Conditions_IsRejected_AndStaysDraft()
    {
        var customer = await CreateCustomerAsync();
        var contract = await CreateContractAsync(customer.Id); // thiếu Content và thiếu tệp

        var response = await SubmitAsync(contract.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await ReadAsync<ErrorResponse>(response);
        Assert.Equal("CTR-02", error!.Code);
        Assert.Equal(2, error.Errors.Count);
        Assert.Contains(error.Errors, e => e.Contains("nội dung"));
        Assert.Contains(error.Errors, e => e.Contains("tệp"));

        // Dữ liệu không đổi: vẫn Draft, không có ApprovalRequest, không có lịch sử
        Assert.Equal(ContractStatus.Draft, await GetStatusFromDbAsync(contract.Id));
        Assert.Equal(0, await CountApprovalsAsync(contract.Id));
        Assert.Equal(0, await CountHistoriesAsync(contract.Id));
    }

    [Fact]
    public async Task Ctr02_SubmitContractOfInactiveCustomer_IsRejected()
    {
        var customer = await CreateCustomerAsync(status: CustomerStatus.Inactive);
        var contract = await CreateContractAsync(customer.Id);
        await PutAsync($"/api/contracts/{contract.Id}", ToUpdateRequest(contract, "Nội dung"));
        await PostAsync($"/api/contracts/{contract.Id}/attachments", new AddAttachmentRequest("hop-dong.pdf"));

        var response = await SubmitAsync(contract.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await ReadAsync<ErrorResponse>(response);
        Assert.Equal("CTR-02", error!.Code);
        Assert.Single(error.Errors);
        Assert.Contains("Khách hàng", error.Errors[0]);
        Assert.Equal(ContractStatus.Draft, await GetStatusFromDbAsync(contract.Id));
    }

    // ---------------------------------------------------------------- TC06
    [Fact]
    public async Task TC06_SubmitValidContract_MovesToPendingApproval()
    {
        var contract = await CreateReadyContractAsync();

        var response = await SubmitAsync(contract.Id);

        // 1. HTTP status và nội dung trả về
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await ReadAsync<SubmitContractResponse>(response);
        Assert.Equal(contract.Id, result!.ContractId);
        Assert.Equal(ContractStatus.PendingApproval, result.Status);
        Assert.True(result.ApprovalId > 0);

        // 2. Dữ liệu thật trong database (không chỉ tin vào mã 200)
        Assert.Equal(ContractStatus.PendingApproval, await GetStatusFromDbAsync(contract.Id));

        var approvals = await WithDbAsync(db =>
            db.ApprovalRequests.AsNoTracking().Where(a => a.ContractId == contract.Id).ToListAsync());
        var approval = Assert.Single(approvals);
        Assert.Equal(result.ApprovalId, approval.Id);
        Assert.Equal(ApprovalStatus.Pending, approval.Status);

        var histories = await WithDbAsync(db =>
            db.ContractHistories.AsNoTracking().Where(h => h.ContractId == contract.Id).ToListAsync());
        var history = Assert.Single(histories);
        Assert.Equal(ContractStatus.Draft, history.FromStatus);
        Assert.Equal(ContractStatus.PendingApproval, history.ToStatus);

        // 3. Đọc lại bằng API
        var read = await Client.GetFromJsonAsync<ContractDto>($"/api/contracts/{contract.Id}", TestJson.Options);
        Assert.Equal(ContractStatus.PendingApproval, read!.Status);
    }

    // ---------------------------------------------------------------- TC08
    [Fact]
    public async Task TC08_SubmitTwice_DoesNotCreateDuplicateApproval()
    {
        var contract = await CreateReadyContractAsync();

        var first = await SubmitAsync(contract.Id);
        var second = await SubmitAsync(contract.Id);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("ALREADY_SUBMITTED", (await ReadAsync<ErrorResponse>(second))!.Code);

        Assert.Equal(1, await CountApprovalsAsync(contract.Id));
        Assert.Equal(1, await CountHistoriesAsync(contract.Id));
        Assert.Equal(ContractStatus.PendingApproval, await GetStatusFromDbAsync(contract.Id));
    }

    // Bổ sung cho TC08 (AC05): hai yêu cầu gửi đồng thời
    [Fact]
    public async Task TC08b_ConcurrentSubmit_OnlyOneSucceeds()
    {
        var contract = await CreateReadyContractAsync();

        var responses = await Task.WhenAll(
            SubmitAsync(contract.Id),
            SubmitAsync(contract.Id));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(1, await CountApprovalsAsync(contract.Id));
        Assert.Equal(1, await CountHistoriesAsync(contract.Id));
    }

    // ---------------------------------------------------------------- Bổ sung
    [Fact]
    public async Task Submit_NonExistingContract_Returns404()
    {
        var response = await SubmitAsync(999999);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("NOT_FOUND", (await ReadAsync<ErrorResponse>(response))!.Code);
    }

    // Lỗi đã tìm thấy ở tuần 06: cùng một tên tệp có thể thêm nhiều lần
    [Fact]
    public async Task AddDuplicateAttachment_IsRejected()
    {
        var customer = await CreateCustomerAsync();
        var contract = await CreateContractAsync(customer.Id);
        var url = $"/api/contracts/{contract.Id}/attachments";

        var first = await PostAsync(url, new AddAttachmentRequest("trung-ten.pdf"));
        var second = await PostAsync(url, new AddAttachmentRequest("TRUNG-TEN.pdf"));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("ATTACHMENT_EXISTS", (await ReadAsync<ErrorResponse>(second))!.Code);

        var count = await WithDbAsync(db => db.ContractAttachments.CountAsync(a => a.ContractId == contract.Id));
        Assert.Equal(1, count);
    }
}
