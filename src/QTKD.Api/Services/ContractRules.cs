using QTKD.Api.Entities;
using QTKD.Api.Exceptions;

namespace QTKD.Api.Services;

// Các quy tắc nghiệp vụ của hợp đồng
public static class ContractRules
{
    // ---------- CTR-01: chỉ được sửa khi Draft hoặc RevisionRequested ----------
    public static void EnsureEditable(Contract contract)
    {
        if (contract.Status is not (ContractStatus.Draft or ContractStatus.RevisionRequested))
        {
            throw new ConflictException(
                "CTR-01",
                $"Không thể chỉnh sửa hợp đồng ở trạng thái {contract.Status}. " +
                "Chỉ được sửa khi hợp đồng ở trạng thái Draft hoặc RevisionRequested.");
        }
    }

    // ---------- CTR-02: điều kiện để gửi trình duyệt ----------
    // Contract phải được nạp kèm Customer và Attachments. Trả về danh sách điều kiện chưa đạt (rỗng = hợp lệ)
    public static List<string> GetSubmitErrors(Contract contract)
    {
        var errors = new List<string>();

        if (contract.Customer is null || contract.Customer.Status != CustomerStatus.Active)
            errors.Add("Khách hàng không hợp lệ (không tồn tại hoặc không ở trạng thái Active).");

        if (contract.StartDate >= contract.EndDate)
            errors.Add("Thời gian hiệu lực không hợp lệ (ngày bắt đầu phải trước ngày kết thúc).");

        if (string.IsNullOrWhiteSpace(contract.ServiceGroup) || string.IsNullOrWhiteSpace(contract.Content))
            errors.Add("Thiếu nội dung bắt buộc (ServiceGroup và Content).");

        if (contract.Attachments.Count == 0)
            errors.Add("Thiếu tệp đính kèm bắt buộc (cần ít nhất 1 tệp).");

        return errors;
    }

    // ---------- CTR-03 và vòng đời trạng thái ----------
    private static readonly Dictionary<ContractStatus, ContractStatus[]> AllowedTransitions = new()
    {
        [ContractStatus.Draft] = new[] { ContractStatus.PendingApproval, ContractStatus.Cancelled },
        [ContractStatus.PendingApproval] = new[]
        {
            ContractStatus.Approved, ContractStatus.Rejected,
            ContractStatus.RevisionRequested, ContractStatus.Cancelled
        },
        [ContractStatus.RevisionRequested] = new[] { ContractStatus.Draft, ContractStatus.Cancelled },
        [ContractStatus.Approved] = new[] { ContractStatus.Active, ContractStatus.Cancelled },
        [ContractStatus.Rejected] = Array.Empty<ContractStatus>(),
        [ContractStatus.Active] = new[] { ContractStatus.Expired, ContractStatus.Cancelled },
        [ContractStatus.Expired] = Array.Empty<ContractStatus>(),
        [ContractStatus.Cancelled] = Array.Empty<ContractStatus>()
    };

    public static bool CanTransition(ContractStatus from, ContractStatus to) =>
        AllowedTransitions.TryGetValue(from, out var targets) && targets.Contains(to);

    public static void EnsureCanTransition(ContractStatus from, ContractStatus to)
    {
        if (from == ContractStatus.Draft && to == ContractStatus.Approved)
        {
            throw new ConflictException(
                "CTR-03",
                "Không được chuyển trực tiếp từ Draft sang Approved. Hợp đồng phải đi qua quy trình phê duyệt.");
        }

        if (!CanTransition(from, to))
        {
            throw new ConflictException(
                "INVALID_TRANSITION",
                $"Không thể chuyển trạng thái hợp đồng từ {from} sang {to}.");
        }
    }
}
