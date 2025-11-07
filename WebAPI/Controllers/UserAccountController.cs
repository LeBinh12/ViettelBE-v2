using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Share;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserAccountController
{
    private readonly IUserService _userService;

    public UserAccountController(IUserService userService)
    {
        _userService = userService;
    }
    
    [HttpPost("register")]
    public async Task<ActionResult<Result<string>>> Register([FromBody] RegisterRequest request)
    {
        return await _userService.RegisterAsync(request.Username, request.Email, request.Password);
    }
    
    [HttpPost("login")]
    public async Task<ActionResult<Result<string>>> Login([FromBody] LoginRequest request)
    {
        return await _userService.LoginAsync( request.Username, request.Password);
    }

    [HttpGet("validate")]
    public async Task<ActionResult<Result<ValidateTokenResponse>>> ValidateToken([FromQuery] string token)
    {
        return await _userService.ValidateTokenAsync(token);
    }
    
}