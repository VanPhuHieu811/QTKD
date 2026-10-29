using Microsoft.EntityFrameworkCore;
using QTKD.Api.Data;
using QTKD.Api.Dtos;

namespace QTKD.Api.Services;

public class ContractService
{
    private readonly QtkdDbContext _db;

    public ContractService(QtkdDbContext db)
    {
        _db = db;
    }

    // Trả null nếu không tồn tại. Dùng Select (projection) nên không bị vòng lặp JSON Contract <-> Customer.
    public async Task<ContractDto?> GetByIdAsync(int id)
    {
        return await _db.Contracts
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ContractDto(
                c.Id,
                c.ContractNo,
                c.CustomerId,
                c.Customer!.Name,
                c.StartDate,
                c.EndDate,
                c.Status,
                c.ServiceGroup,
                c.Content,
                c.Attachments.Select(a => a.FileName).ToList()))
            .FirstOrDefaultAsync();
    }
}
