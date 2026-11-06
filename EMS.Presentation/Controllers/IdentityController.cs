using EMS.Application.Common.Models;
using EMS.Application.Dtos.RefreshToken;
using EMS.Application.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class IdentityController : BaseApiController
    {
        private readonly IAuthService _authService;
        public IdentityController(IAuthService authService) 
        { 
            _authService = authService;
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken) 
        { 
            var result = await _authService.LoginAsync(request, cancellationToken);
            return Success(result);
        }

        [AllowAnonymous]
        [HttpPost("RefreshToken")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
        { 
            var result = await _authService.RefreshTokenAsync(request, cancellationToken);
            return Success(result);
        }

        [Authorize]
        [HttpPost("RevokeRefreshToken")]
        public async Task<ActionResult<ApiResponse<object>>> RevokeRefreshToken([FromBody] RefreshTokenRequestDto request, CancellationToken cancellationToken)
        { 
            await _authService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
            return Done("ابطال رفرش توکن با موفقیت انجام شد.");
        }

        //[Authorize(Roles = "Admin")]
        //[HttpPost("register")]
        //public async Task<IActionResult> Register([FromBody] RegisterUserRequestDto request, CancellationToken cancellationToken)
        //{ 
        //    var userId = await _authService.RegisterAsync(request, cancellationToken);
        //    return Ok(new { userId });
        //}
    }
}
