namespace QTKD.Api.Entities;

// Mô phỏng Approval (giả định G6): mỗi lần Submit thành công tạo 1 bản ghi Pending.
public class ApprovalRequest
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public Contract? Contract { get; set; }

    // Luôn gán DateTime.UtcNow (Npgsql yêu cầu Kind = Utc với timestamptz)
    public DateTime SubmittedAt { get; set; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
}
