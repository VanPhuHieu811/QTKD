namespace QTKD.Api.Entities;

// Khóa chính ghép (PriceListId, ServiceCode) được khai báo trong QtkdDbContext
public class PriceItem
{
    public int PriceListId { get; set; }
    public PriceList? PriceList { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
}
