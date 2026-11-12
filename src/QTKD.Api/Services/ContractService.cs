using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using QTKD.Api.Data;
using QTKD.Api.Dtos;
using QTKD.Api.Entities;
using QTKD.Api.Exceptions;

namespace QTKD.Api.Services;

public class ContractService
{
    private readonly QtkdDbContext _db;

    public ContractService(QtkdDbContext db)
    {
        _db = db;
    }

    private static readonly Expression<Func<Contract, ContractDto>> ToDto = c => new ContractDto(
        c.Id,
        c.ContractNo,
        c.CustomerId,
        c.Customer!.Name,
        c.StartDate,
        c.EndDate,
        c.Status,
        c.ServiceGroup,
        c.Content,
        c.Attachments.Select(a => a.FileName).ToList());

    // ---------- Xem ----------
    public async Task<List<ContractDto>> GetAllAsync()
    {
        return await _db.Contracts
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(ToDto)
            .ToListAsync();
    }

    public async Task<ContractDto> GetByIdAsync(int id)
    {
        return await _db.Contracts
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"Không tìm thấy hợp đồng có Id = {id}.");
    }

    // ---------- Tạo ----------
    public async Task<ContractDto> CreateAsync(CreateContractRequest request)
    {
        var errors = CheckRequiredFields(request.CustomerId, request.StartDate, request.EndDate);

        var contractNo = request.ContractNo?.Trim();
        if (string.IsNullOrEmpty(contractNo))
            errors.Add("ContractNo là bắt buộc.");
        else if (contractNo.Length > 50)
            errors.Add("ContractNo tối đa 50 ký tự.");

        ThrowIfAny(errors);
        await EnsureCustomerExistsAsync(request.CustomerId!.Value);

        if (await _db.Contracts.AnyAsync(c => c.ContractNo == contractNo))
            throw new ConflictException("CONTRACT_NO_EXISTS", $"Số hợp đồng {contractNo} đã tồn tại.");

        var contract = new Contract
        {
            ContractNo = contractNo!,
            CustomerId = request.CustomerId.Value,
            StartDate = request.StartDate!.Value,
            EndDate = request.EndDate!.Value,
            Status = ContractStatus.Draft,
            ServiceGroup = Normalize(request.ServiceGroup),
            Content = Normalize(request.Content)
        };

        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(contract.Id);
    }

    // ---------- Sửa (CTR-01) ----------
    public async Task<ContractDto> UpdateAsync(int id, UpdateContractRequest request)
    {
        var contract = await _db.Contracts.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException($"Không tìm thấy hợp đồng có Id = {id}.");

        ContractRules.EnsureEditable(contract); // CTR-01

        ThrowIfAny(CheckRequiredFields(request.CustomerId, request.StartDate, request.EndDate));
        await EnsureCustomerExistsAsync(request.CustomerId!.Value);

        contract.CustomerId = request.CustomerId.Value;
        contract.StartDate = request.StartDate!.Value;
        contract.EndDate = request.EndDate!.Value;
        contract.ServiceGroup = Normalize(request.ServiceGroup);
        contract.Content = Normalize(request.Content);

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    // ---------- Thêm tệp đính kèm (cũng là sửa hợp đồng nên áp dụng CTR-01) ----------
    public async Task<ContractDto> AddAttachmentAsync(int id, AddAttachmentRequest request)
    {
        var contract = await _db.Contracts.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException($"Không tìm thấy hợp đồng có Id = {id}.");

        ContractRules.EnsureEditable(contract); // CTR-01

        var fileName = request.FileName?.Trim();
        var errors = new List<string>();
        if (string.IsNullOrEmpty(fileName))
            errors.Add("FileName là bắt buộc.");
        else if (fileName.Length > 260)
            errors.Add("FileName tối đa 260 ký tự.");
        ThrowIfAny(errors);

        // Kiểm tra trùng tên tệp đính kèm trong cùng một hợp đồng (không phân biệt hoa thường)
        var lower = fileName!.ToLower();
        if (await _db.ContractAttachments.AnyAsync(a => a.ContractId == id && a.FileName.ToLower() == lower))
        {
            throw new ConflictException("ATTACHMENT_EXISTS", $"Tệp đính kèm '{fileName}' đã tồn tại trong hợp đồng.");
        }

        _db.ContractAttachments.Add(new ContractAttachment { ContractId = id, FileName = fileName! });
        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    // ---------- Gửi trình duyệt (US03) ----------
    public async Task<SubmitContractResponse> SubmitAsync(int id)
    {
        var contract = await _db.Contracts
            .Include(c => c.Customer)
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException($"Không tìm thấy hợp đồng có Id = {id}.");

        // AC02 / AC05: sai trạng thái hoặc gửi lặp -> từ chối, không tạo thêm gì
        if (contract.Status == ContractStatus.PendingApproval)
        {
            throw new ConflictException(
                "ALREADY_SUBMITTED",
                "Hợp đồng đã được gửi trình duyệt (PendingApproval), không tạo thêm yêu cầu mới.");
        }

        if (contract.Status != ContractStatus.Draft)
        {
            throw new ConflictException(
                "INVALID_STATE",
                $"Chỉ gửi trình duyệt được hợp đồng ở trạng thái Draft (hiện tại: {contract.Status}).");
        }

        // AC03: CTR-02
        var errors = ContractRules.GetSubmitErrors(contract);
        if (errors.Count > 0)
        {
            throw new ValidationFailedException(
                "CTR-02", "Hợp đồng chưa đủ điều kiện gửi trình duyệt.", errors);
        }

        // CTR-03: chỉ đi Draft -> PendingApproval
        ContractRules.EnsureCanTransition(contract.Status, ContractStatus.PendingApproval);

        // AC04: tạo ApprovalRequest, ghi lịch sử, đổi trạng thái. Một lần SaveChanges nên là một giao dịch:
        // lỗi ở bất kỳ phần nào thì không có thay đổi nào được lưu.
        var now = DateTime.UtcNow;
        var approval = new ApprovalRequest
        {
            ContractId = contract.Id,
            SubmittedAt = now,
            Status = ApprovalStatus.Pending
        };
        _db.ApprovalRequests.Add(approval);
        _db.ContractHistories.Add(new ContractHistory
        {
            ContractId = contract.Id,
            FromStatus = contract.Status,
            ToStatus = ContractStatus.PendingApproval,
            ChangedAt = now
        });
        contract.Status = ContractStatus.PendingApproval;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // AC05: hai yêu cầu đồng thời, unique index (ContractId, Pending) chặn yêu cầu đến sau
            throw new ConflictException(
                "ALREADY_SUBMITTED",
                "Hợp đồng đã được gửi trình duyệt bởi một yêu cầu khác.");
        }

        return new SubmitContractResponse(contract.Id, contract.Status, approval.Id);
    }

    // ---------- Hàm dùng chung ----------
    private static List<string> CheckRequiredFields(int? customerId, DateOnly? startDate, DateOnly? endDate)
    {
        var errors = new List<string>();
        if (customerId is null) errors.Add("CustomerId là bắt buộc.");
        if (startDate is null) errors.Add("StartDate là bắt buộc.");
        if (endDate is null) errors.Add("EndDate là bắt buộc.");
        return errors;
    }

    private static void ThrowIfAny(List<string> errors)
    {
        if (errors.Count > 0)
            throw new ValidationFailedException("VALIDATION_FAILED", "Dữ liệu hợp đồng không hợp lệ.", errors);
    }

    private async Task EnsureCustomerExistsAsync(int customerId)
    {
        if (!await _db.Customers.AnyAsync(c => c.Id == customerId))
        {
            throw new ValidationFailedException(
                "VALIDATION_FAILED",
                "Dữ liệu hợp đồng không hợp lệ.",
                new[] { $"CustomerId = {customerId} không tồn tại." });
        }
    }

    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
