namespace QTKD.Api.Entities;

// "Tệp bắt buộc" cho CTR-02 (giả định G2). Chỉ lưu tên tệp, chưa upload thật.
public class ContractAttachment
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public Contract? Contract { get; set; }
    public string FileName { get; set; } = string.Empty;
}
