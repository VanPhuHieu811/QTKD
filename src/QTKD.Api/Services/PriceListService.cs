using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using QTKD.Api.Data;
using QTKD.Api.Dtos;
using QTKD.Api.Entities;
using QTKD.Api.Exceptions;

namespace QTKD.Api.Services;

public class PriceListService
{
    private readonly QtkdDbContext _db;

    public PriceListService(QtkdDbContext db)
    {
        _db = db;
    }

    private static readonly Expression<Func<PriceList, PriceListDto>> ToDto = p => new PriceListDto(
        p.Id,
        p.Name,
        p.EffectiveDate,
        p.Items.OrderBy(i => i.ServiceCode)
               .Select(i => new PriceItemDto(i.ServiceCode, i.UnitPrice))
               .ToList());

    public async Task<List<PriceListDto>> GetAllAsync()
    {
        return await _db.PriceLists.AsNoTracking().OrderBy(p => p.Id).Select(ToDto).ToListAsync();
    }

    public async Task<PriceListDto> GetByIdAsync(int id)
    {
        return await _db.PriceLists.AsNoTracking().Where(p => p.Id == id).Select(ToDto).FirstOrDefaultAsync()
            ?? throw new NotFoundException($"Không tìm thấy bảng giá có Id = {id}.");
    }

    public async Task<List<PriceItemDto>> GetItemsAsync(int priceListId)
    {
        var list = await GetByIdAsync(priceListId); // 404 nếu bảng giá không tồn tại
        return list.Items;
    }

    public async Task<PriceListDto> CreateAsync(CreatePriceListRequest request)
    {
        var errors = new List<string>();
        var name = request.Name?.Trim();

        if (string.IsNullOrEmpty(name)) errors.Add("Name là bắt buộc.");
        else if (name.Length > 200) errors.Add("Name tối đa 200 ký tự.");
        if (request.EffectiveDate is null) errors.Add("EffectiveDate là bắt buộc.");

        if (errors.Count > 0)
            throw new ValidationFailedException("VALIDATION_FAILED", "Dữ liệu bảng giá không hợp lệ.", errors);

        var priceList = new PriceList { Name = name!, EffectiveDate = request.EffectiveDate!.Value };
        _db.PriceLists.Add(priceList);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(priceList.Id);
    }

    public async Task<PriceItemDto> AddItemAsync(int priceListId, AddPriceItemRequest request)
    {
        if (!await _db.PriceLists.AnyAsync(p => p.Id == priceListId))
            throw new NotFoundException($"Không tìm thấy bảng giá có Id = {priceListId}.");

        var errors = new List<string>();
        var code = request.ServiceCode?.Trim();

        if (string.IsNullOrEmpty(code)) errors.Add("ServiceCode là bắt buộc.");
        else if (code.Length > 50) errors.Add("ServiceCode tối đa 50 ký tự.");

        if (request.UnitPrice is null) errors.Add("UnitPrice là bắt buộc.");
        else if (request.UnitPrice <= 0) errors.Add("UnitPrice phải lớn hơn 0.");

        if (errors.Count > 0)
            throw new ValidationFailedException("VALIDATION_FAILED", "Dữ liệu mục giá không hợp lệ.", errors);

        if (await _db.PriceItems.AnyAsync(i => i.PriceListId == priceListId && i.ServiceCode == code))
            throw new ConflictException("SERVICE_CODE_EXISTS", $"Mã dịch vụ {code} đã có trong bảng giá này.");

        var item = new PriceItem
        {
            PriceListId = priceListId,
            ServiceCode = code!,
            UnitPrice = request.UnitPrice!.Value
        };
        _db.PriceItems.Add(item);
        await _db.SaveChangesAsync();
        return new PriceItemDto(item.ServiceCode, item.UnitPrice);
    }
}
