using QTKD.Api.Entities;

namespace QTKD.Api.Dtos;

// Các thuộc tính để nullable (string?) để thiếu dữ liệu thì service trả lỗi 400 theo định dạng chung.
public record CreateCustomerRequest(string? Name, CustomerStatus? Status);

public record UpdateCustomerRequest(string? Name, CustomerStatus? Status);
