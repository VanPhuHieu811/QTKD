using QTKD.Api.Entities;

namespace QTKD.Api.Dtos;

public record CreateContractRequest(
    string? ContractNo,
    int? CustomerId,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? ServiceGroup,
    string? Content);

// ContractNo không đổi sau khi tạo
public record UpdateContractRequest(
    int? CustomerId,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? ServiceGroup,
    string? Content);

public record AddAttachmentRequest(string? FileName);

public record SubmitContractResponse(int ContractId, ContractStatus Status, int ApprovalId);
