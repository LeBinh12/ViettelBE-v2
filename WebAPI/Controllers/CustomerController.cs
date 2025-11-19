using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _service;

        public CustomerController(ICustomerService service)
        {
            _service = service;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] CustomerDTO.CustomerRequestDto dto)
        {
            var result = await _service.AddAsync(dto);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        [HttpPost("update/{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CustomerDTO.CustomerRequestDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        [HttpPost("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmail([FromQuery] string email)
        {
            var result = await _service.IsEmailValidAsync(email);
            return Ok(result);
        }
        
        // GET: api/Customer/paging?search=...&pageNumber=1&pageSize=10
                [HttpGet("paging")]
        public async Task<IActionResult> GetPaged([FromQuery] CustomerPagingRequestDto request)
        {
            var result = await _service.GetPagedAsync(request);
            return Ok(result);
        }
    }
}