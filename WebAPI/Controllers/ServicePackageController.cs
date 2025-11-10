using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServicePackageController : ControllerBase
    {
        private readonly IServicePackageService _service;

        public ServicePackageController(IServicePackageService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return StatusCode(result.Code, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return StatusCode(result.Code, result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ServicePackageRequest request)
        {
            var result = await _service.CreateAsync(request);
            return StatusCode(result.Code, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, ServicePackageUpdateRequest request)
        {
            if (id != request.Id)
                return BadRequest(new { message = "Id không khớp" });

            var result = await _service.UpdateAsync(request);
            return StatusCode(result.Code, result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            return StatusCode(result.Code, result);
        }
    }
}
