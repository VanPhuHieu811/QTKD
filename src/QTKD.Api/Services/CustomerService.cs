using Microsoft.EntityFrameworkCore;
using QTKD.Api.Data;
using QTKD.Api.Dtos;

namespace QTKD.Api.Services;

public class CustomerService
{
    private readonly QtkdDbContext _db;

    public CustomerService(QtkdDbContext db)
    {
        _db = db;
    }

    public async Task<List<CustomerDto>> GetAllAsync()
    {
        return await _db.Customers
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => new CustomerDto(c.Id, c.Name, c.Status))
            .ToListAsync();
    }
}
