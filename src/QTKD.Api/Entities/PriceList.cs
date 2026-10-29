namespace QTKD.Api.Entities;

public class PriceList
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly EffectiveDate { get; set; }

    public ICollection<PriceItem> Items { get; set; } = new List<PriceItem>();
}
