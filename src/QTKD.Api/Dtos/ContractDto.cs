using QTKD.Api.Entities;

namespace QTKD.Api.Dtos;

public record ContractDto(
    int Id,
    string ContractNo,
    int CustomerId,
    string CustomerName,
    DateOnly StartDate,
    DateOnly EndDate,
    ContractStatus Status,
    string? ServiceGroup,
    string? Content,
    List<string> Attachments);
