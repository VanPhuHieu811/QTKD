using Microsoft.AspNetCore.Mvc;
using QTKD.Api.Dtos;
using QTKD.Api.Services;

namespace QTKD.Api.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly CustomerService _service;

    public CustomersController(CustomerService service)
    {
        _service = service;
    }

    // GET /api/customers
    [HttpGet]
    public async Task<ActionResult<List<CustomerDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    // GET /api/customers/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> GetById(int id)
        => Ok(await _service.GetByIdAsync(id));

    // POST /api/customers
    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request)
    {
        var created = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT /api/customers/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomerDto>> Update(int id, UpdateCustomerRequest request)
        => Ok(await _service.UpdateAsync(id, request));
}
