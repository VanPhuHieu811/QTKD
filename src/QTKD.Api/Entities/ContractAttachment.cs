namespace QTKD.Api.Entities;

public class ContractAttachment
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public Contract? Contract { get; set; }
    public string FileName { get; set; } = string.Empty;
}
