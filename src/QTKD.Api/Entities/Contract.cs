namespace QTKD.Api.Entities;

public class Contract
{
    public int Id { get; set; }
    public string ContractNo { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Draft;

    // "Nội dung bắt buộc" cho CTR-02 (giả định G1)
    public string? ServiceGroup { get; set; }
    public string? Content { get; set; }

    public ICollection<ContractAttachment> Attachments { get; set; } = new List<ContractAttachment>();
    public ICollection<ApprovalRequest> ApprovalRequests { get; set; } = new List<ApprovalRequest>();
    public ICollection<ContractHistory> Histories { get; set; } = new List<ContractHistory>();
}
