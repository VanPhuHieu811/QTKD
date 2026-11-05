namespace QTKD.Api.Dtos;

public record PriceItemDto(string ServiceCode, decimal UnitPrice);

public record PriceListDto(int Id, string Name, DateOnly EffectiveDate, List<PriceItemDto> Items);

public record CreatePriceListRequest(string? Name, DateOnly? EffectiveDate);

public record AddPriceItemRequest(string? ServiceCode, decimal? UnitPrice);
