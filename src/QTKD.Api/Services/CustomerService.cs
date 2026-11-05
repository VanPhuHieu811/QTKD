using Microsoft.EntityFrameworkCore;
using QTKD.Api.Data;
using QTKD.Api.Dtos;
using QTKD.Api.Entities;
using QTKD.Api.Exceptions;

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

    public async Task<CustomerDto> GetByIdAsync(int id)
    {
        return await _db.Customers
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CustomerDto(c.Id, c.Name, c.Status))
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"Không tìm thấy khách hàng có Id = {id}.");
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request)
    {
        var name = ValidateName(request.Name);

        var customer = new Customer
        {
            Name = name,
            Status = request.Status ?? CustomerStatus.Active
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return ToDto(customer);
    }

    public async Task<CustomerDto> UpdateAsync(int id, UpdateCustomerRequest request)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException($"Không tìm thấy khách hàng có Id = {id}.");

        customer.Name = ValidateName(request.Name);
        if (request.Status.HasValue)
            customer.Status = request.Status.Value;

        await _db.SaveChangesAsync();
        return ToDto(customer);
    }

    private static string ValidateName(string? name)
    {
        var errors = new List<string>();
        var trimmed = name?.Trim();

        if (string.IsNullOrEmpty(trimmed))
            errors.Add("Name là bắt buộc.");
        else if (trimmed.Length > 200)
            errors.Add("Name tối đa 200 ký tự.");

        if (errors.Count > 0)
            throw new ValidationFailedException("VALIDATION_FAILED", "Dữ liệu khách hàng không hợp lệ.", errors);

        return trimmed!;
    }

    private static CustomerDto ToDto(Customer c) => new(c.Id, c.Name, c.Status);
}
