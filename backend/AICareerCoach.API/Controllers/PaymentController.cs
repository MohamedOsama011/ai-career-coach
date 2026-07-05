using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly Ipayment ipayment;
        private readonly ILogger<FawaterakController> logger;
        private readonly UserManager<User> userManager;

        public PaymentController(Ipayment _ipayment, ILogger<FawaterakController> _logger, UserManager<User> _userManager)
        {
            ipayment = _ipayment;
            logger = _logger;
            userManager = _userManager;
        }


        [HttpGet("getalluserpayments")]
        public async Task<IActionResult> getalluserpayments()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User is not authenticated.");
            }
            var result = await ipayment.Getallpaymentsbyid(user.Id);
            return Ok(result);
        }
    }
}
