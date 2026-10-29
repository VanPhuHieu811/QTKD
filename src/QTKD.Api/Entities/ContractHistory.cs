namespace QTKD.Api.Entities;

public class ContractHistory
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public Contract? Contract { get; set; }

    public ContractStatus FromStatus { get; set; }
    public ContractStatus ToStatus { get; set; }
    public DateTime ChangedAt { get; set; }
}
