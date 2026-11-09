using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.Permission;
using EMS.Application.Dtos.RefreshToken;
using EMS.Application.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class IdentityController : BaseApiController
    {
        private readonly IAuthService _authService;
        private readonly ICurrentUserService _currentUser;
        public IdentityController(IAuthService authService, ICurrentUserService currentUser)
        {
            _authService = authService;
            _currentUser = currentUser;
        }

        /// <summary>
        /// The signed-in user with his roles and permissions as they are now in the database. The UI reads it to decide which pages and
        /// buttons to show (after a reload, or after a refresh following a 401), instead of trusting what it stored at login.
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<ApiResponse<CurrentUserDto>>> Me(CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId ?? throw new UnauthorizedAppException("احراز هویت انجام نشده یا توکن نامعتبر است.");
            return Success(await _authService.GetCurrentUserAsync(userId, cancellationToken));
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
