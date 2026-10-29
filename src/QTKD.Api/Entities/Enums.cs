namespace QTKD.Api.Entities;

public enum CustomerStatus
{
    Active,
    Inactive
}

// Ánh xạ từ sơ đồ trạng thái Phase 1 (xem docs/design-notes.md, mục 2)
public enum ContractStatus
{
    Draft,              // Draft (Soạn thảo / Bản nháp)
    PendingApproval,    // Submitted + Under Review (gộp, giả định G8)
    RevisionRequested,  // Revision Requested (Yêu cầu chỉnh sửa)
    Approved,           // Approved (Đã phê duyệt)
    Rejected,           // Rejected (Từ chối)
    Active,             // Active (Có hiệu lực)
    Expired,            // Expired (Hết hiệu lực)
    Cancelled           // Cancelled (Hủy)
}

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected
}
