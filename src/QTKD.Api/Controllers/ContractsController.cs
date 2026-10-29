using Microsoft.AspNetCore.Mvc;
using QTKD.Api.Dtos;
using QTKD.Api.Services;

namespace QTKD.Api.Controllers;

[ApiController]
[Route("api/contracts")]
public class ContractsController : ControllerBase
{
    private readonly ContractService _service;

    public ContractsController(ContractService service)
    {
        _service = service;
    }

    // GET /api/contracts/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContractDto>> GetById(int id)
    {
        var contract = await _service.GetByIdAsync(id);
        if (contract is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy hợp đồng",
                Detail = $"Không có hợp đồng với Id = {id}."
            });
        }

        return Ok(contract);
    }
}
