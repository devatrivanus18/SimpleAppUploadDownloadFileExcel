using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PortalWebServer.Model;
using PortalWebServer.Service;

namespace PortalWebServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OtpController : ControllerBase
{
    private readonly OtpService _otpService;

    public OtpController(OtpService otpService)
    {
        _otpService = otpService;
    }

    [HttpPost("request-otp")]
    public IActionResult RequestOtp([FromBody] OtpRequest request)
    {
        var otp = _otpService.GenerateOtp(request.Email);
        _otpService.SendEmail(request.Email, otp);
        return Ok(new { message = "OTP sent successfully" });
    }

    [HttpPost("verify-otp")]
    public IActionResult VerifyOtp([FromBody] VerifyOtpRequest request, [FromServices] JwtService jwtService)
    {
        if (_otpService.VerifyOtp(request.Email, request.Otp))
        {
            var token = jwtService.GenerateJwtToken(request.Email);
            return Ok(new { token });
        }
        return Unauthorized(new { message = "Invalid OTP" });
    }
}
