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

    // GET /api/contracts
    [HttpGet]
    public async Task<ActionResult<List<ContractDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    // GET /api/contracts/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContractDto>> GetById(int id)
        => Ok(await _service.GetByIdAsync(id));

    // POST /api/contracts
    [HttpPost]
    public async Task<ActionResult<ContractDto>> Create(CreateContractRequest request)
    {
        var created = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT /api/contracts/{id}   (CTR-01)
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ContractDto>> Update(int id, UpdateContractRequest request)
        => Ok(await _service.UpdateAsync(id, request));

    // POST /api/contracts/{id}/attachments   (CTR-01)
    [HttpPost("{id:int}/attachments")]
    public async Task<ActionResult<ContractDto>> AddAttachment(int id, AddAttachmentRequest request)
        => Ok(await _service.AddAttachmentAsync(id, request));

    // POST /api/contracts/{id}/submit   (US03: CTR-02, CTR-03)
    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<SubmitContractResponse>> Submit(int id)
        => Ok(await _service.SubmitAsync(id));
}
