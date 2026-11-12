using QTKD.Api.Entities;
using QTKD.Api.Exceptions;
using QTKD.Api.Services;

namespace QTKD.Tests.Unit;

// Unit test cho CTR-01, CTR-02, CTR-03: chỉ kiểm tra logic nghiệp vụ, không dùng database.
[Trait("Category", "Unit")]
public class ContractRulesTests
{
    // Hợp đồng hợp lệ để gửi: mỗi test chỉ làm hỏng đúng một điều kiện
    private static Contract ValidContract() => new()
    {
        Id = 1,
        ContractNo = "HD-TEST-001",
        CustomerId = 1,
        Customer = new Customer { Id = 1, Name = "Công ty Test", Status = CustomerStatus.Active },
        StartDate = new DateOnly(2026, 10, 1),
        EndDate = new DateOnly(2027, 9, 30),
        Status = ContractStatus.Draft,
        ServiceGroup = "Dịch vụ cảng",
        Content = "Nội dung hợp đồng",
        Attachments = new List<ContractAttachment>
        {
            new() { Id = 1, ContractId = 1, FileName = "hop-dong.pdf" }
        }
    };

    // =====================================================================
    // CTR-01: chỉ được sửa khi Draft hoặc RevisionRequested
    // =====================================================================

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.RevisionRequested)]
    public void Ctr01_EditableStatus_IsAllowed(ContractStatus status)
    {
        var contract = ValidContract();
        contract.Status = status;

        var exception = Record.Exception(() => ContractRules.EnsureEditable(contract));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(ContractStatus.PendingApproval)]
    [InlineData(ContractStatus.Approved)]
    [InlineData(ContractStatus.Rejected)]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Expired)]
    [InlineData(ContractStatus.Cancelled)]
    public void Ctr01_NonEditableStatus_IsRejected(ContractStatus status)
    {
        var contract = ValidContract();
        contract.Status = status;

        var exception = Assert.Throws<ConflictException>(() => ContractRules.EnsureEditable(contract));

        Assert.Equal("CTR-01", exception.Code);
        Assert.Equal(409, exception.StatusCode);
    }

    // =====================================================================
    // CTR-02: điều kiện để gửi trình duyệt
    // =====================================================================

    [Fact]
    public void Ctr02_ValidContract_HasNoErrors()
    {
        var errors = ContractRules.GetSubmitErrors(ValidContract());

        Assert.Empty(errors);
    }

    [Fact]
    public void Ctr02_InactiveCustomer_IsReported()
    {
        var contract = ValidContract();
        contract.Customer!.Status = CustomerStatus.Inactive;

        var errors = ContractRules.GetSubmitErrors(contract);

        Assert.Single(errors);
        Assert.Contains("Khách hàng", errors[0]);
    }

    [Fact]
    public void Ctr02_MissingCustomer_IsReported()
    {
        var contract = ValidContract();
        contract.Customer = null;

        var errors = ContractRules.GetSubmitErrors(contract);

        Assert.Single(errors);
        Assert.Contains("Khách hàng", errors[0]);
    }

    [Theory]
    [InlineData(2026, 10, 1, 2026, 10, 1)]   // ngày bắt đầu bằng ngày kết thúc
    [InlineData(2027, 10, 1, 2026, 10, 1)]   // ngày bắt đầu sau ngày kết thúc
    public void Ctr02_InvalidDateRange_IsReported(int sy, int sm, int sd, int ey, int em, int ed)
    {
        var contract = ValidContract();
        contract.StartDate = new DateOnly(sy, sm, sd);
        contract.EndDate = new DateOnly(ey, em, ed);

        var errors = ContractRules.GetSubmitErrors(contract);

        Assert.Single(errors);
        Assert.Contains("hiệu lực", errors[0]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctr02_MissingContent_IsReported(string? content)
    {
        var contract = ValidContract();
        contract.Content = content;

        var errors = ContractRules.GetSubmitErrors(contract);

        Assert.Single(errors);
        Assert.Contains("nội dung", errors[0]);
    }

    [Fact]
    public void Ctr02_MissingServiceGroup_IsReported()
    {
        var contract = ValidContract();
        contract.ServiceGroup = null;

        var errors = ContractRules.GetSubmitErrors(contract);

        Assert.Single(errors);
        Assert.Contains("nội dung", errors[0]);
    }

    [Fact]
    public void Ctr02_NoAttachments_IsReported()
    {
        var contract = ValidContract();
        contract.Attachments.Clear();

        var errors = ContractRules.GetSubmitErrors(contract);

        Assert.Single(errors);
        Assert.Contains("tệp", errors[0]);
    }

    [Fact]
    public void TC05_Ctr02_SeveralProblems_AreAllReported()
    {
        var contract = ValidContract();
        contract.Customer!.Status = CustomerStatus.Inactive;
        contract.Content = null;
        contract.Attachments.Clear();

        var errors = ContractRules.GetSubmitErrors(contract);

        // Trả về đủ cả 3 điều kiện chưa đạt, không dừng ở lỗi đầu tiên
        Assert.Equal(3, errors.Count);
    }

    // =====================================================================
    // CTR-03: không chuyển trực tiếp Draft sang Approved
    // =====================================================================

    [Fact]
    public void TC07_Ctr03_DraftToApproved_IsRejected()
    {
        var exception = Assert.Throws<ConflictException>(
            () => ContractRules.EnsureCanTransition(ContractStatus.Draft, ContractStatus.Approved));

        Assert.Equal("CTR-03", exception.Code);
        Assert.False(ContractRules.CanTransition(ContractStatus.Draft, ContractStatus.Approved));
    }

    [Fact]
    public void Ctr03_DraftToPendingApproval_IsAllowed()
    {
        var exception = Record.Exception(
            () => ContractRules.EnsureCanTransition(ContractStatus.Draft, ContractStatus.PendingApproval));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(ContractStatus.PendingApproval, ContractStatus.Approved)]
    [InlineData(ContractStatus.PendingApproval, ContractStatus.Rejected)]
    [InlineData(ContractStatus.PendingApproval, ContractStatus.RevisionRequested)]
    [InlineData(ContractStatus.RevisionRequested, ContractStatus.Draft)]
    [InlineData(ContractStatus.Approved, ContractStatus.Active)]
    [InlineData(ContractStatus.Active, ContractStatus.Expired)]
    public void Ctr03_TransitionsInLifecycle_AreAllowed(ContractStatus from, ContractStatus to)
    {
        Assert.True(ContractRules.CanTransition(from, to));
    }

    [Theory]
    [InlineData(ContractStatus.Draft, ContractStatus.Active)]
    [InlineData(ContractStatus.Draft, ContractStatus.Rejected)]
    [InlineData(ContractStatus.Rejected, ContractStatus.Active)]
    [InlineData(ContractStatus.Approved, ContractStatus.Draft)]
    [InlineData(ContractStatus.Cancelled, ContractStatus.Draft)]
    public void Ctr03_TransitionsOutsideLifecycle_AreRejected(ContractStatus from, ContractStatus to)
    {
        var exception = Assert.Throws<ConflictException>(() => ContractRules.EnsureCanTransition(from, to));

        Assert.Equal("INVALID_TRANSITION", exception.Code);
    }
}
