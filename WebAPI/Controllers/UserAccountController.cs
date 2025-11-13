using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Share;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserAccountController : ControllerBase
{
    private readonly IUserService _userService;

    public UserAccountController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<Result<string>>> Register([FromBody] RegisterRequest request)
    {
        var result = await _userService.RegisterAsync(request.Username, request.Email, request.Password);
        if (result.Succeeded)
            return Ok(result);
        return BadRequest(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<Result<string>>> Login([FromBody] LoginRequest request)
    {
        var result = await _userService.LoginAsync(request.Username, request.Password);
        if (result.Succeeded)
            return Ok(result);
        return BadRequest(result);
    }

    [HttpGet("validate")]
    public async Task<ActionResult<Result<ValidateTokenResponse>>> ValidateToken([FromQuery] string token)
    {
        var result = await _userService.ValidateTokenAsync(token);
        if (result.Succeeded)
            return Ok(result);
        return BadRequest(result);
    }

  
    [HttpGet("pagination")]
    public async Task<ActionResult<Result<PagedResult<UserDto>>>> GetPagedUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _userService.GetPagedUsersAsync(pageNumber, pageSize);
        if (result.Succeeded)
            return Ok(result);
        return BadRequest(result);
    }


}
