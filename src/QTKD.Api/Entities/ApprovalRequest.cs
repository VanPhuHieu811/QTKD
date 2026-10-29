namespace QTKD.Api.Entities;

public class ApprovalRequest
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public Contract? Contract { get; set; }

    public DateTime SubmittedAt { get; set; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
}
