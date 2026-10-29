using QTKD.Api.Entities;

namespace QTKD.Api.Dtos;

public record CustomerDto(int Id, string Name, CustomerStatus Status);
