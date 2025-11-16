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

        // GET: api/Customer
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        // POST: api/Customer

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CustomerDTO.CustomerRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _service.AddAsync(dto);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }



        // PUT: api/Customer/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CustomerDTO.CustomerRequestDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        // DELETE: api/Customer/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            return result.Succeeded ? Ok(result) : BadRequest(result);
        }

        // GET: api/Customer/check-email?email=...
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
