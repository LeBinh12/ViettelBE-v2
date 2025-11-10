using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Share.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet("all")]
        public async Task<ActionResult<IResult<IEnumerable<CategoryResponse>>>> GetAll()
        {
            var result = await _categoryService.GetAllAsync();
            return StatusCode(result.Code, result);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<IResult<CategoryResponse>>> GetById(Guid id)
        {
            var result = await _categoryService.GetByIdAsync(id);
            return StatusCode(result.Code, result);
        }

        [HttpPost]
        public async Task<ActionResult<IResult<CategoryResponse>>> Create([FromBody] CategoryRequest request)
        {
            var result = await _categoryService.CreateAsync(request);
            return StatusCode(result.Code, result);
        }

        [HttpPut]
        public async Task<ActionResult<IResult<CategoryResponse>>> Update([FromBody] CategoryUpdateRequest request)
        {
            var result = await _categoryService.UpdateAsync(request);
            return StatusCode(result.Code, result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<IResult<bool>>> Delete(Guid id)
        {
            var result = await _categoryService.DeleteAsync(id);
            return StatusCode(result.Code, result);
        }
    }
}
