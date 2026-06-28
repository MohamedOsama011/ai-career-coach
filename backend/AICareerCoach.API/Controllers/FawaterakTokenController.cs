using AICareerCoach.BLL.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FawaterakTokenController : ControllerBase
    {

        private readonly IFawaterakTokenService _fawaterakTokenService;
        public FawaterakTokenController(IFawaterakTokenService fawaterakTokenService)
        {
            _fawaterakTokenService = fawaterakTokenService;
        }
        [HttpGet("GetAccessToken")]
        public async Task<IActionResult> GetAccessToken()
        {
            try
            {
                var accessToken = await _fawaterakTokenService.GetAccessTokenAsync();
                return Ok(new { AccessToken = accessToken });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = ex.Message });
            }
        }
    }
}
