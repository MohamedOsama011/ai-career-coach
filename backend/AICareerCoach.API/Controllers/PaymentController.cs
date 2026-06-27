using AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Authorization;
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
        private readonly UserManager<User> _userManager;

        public PaymentController(Ipayment _ipayment, UserManager<User> userManager)
        {
            ipayment = _ipayment;
            _userManager = userManager;
        }

        [Authorize]
        [HttpPost("create/{planid}")]
        public async Task<IActionResult> Create(string planid)
        {
            var user = await _userManager.GetUserAsync(User);
          var resul=await  ipayment.createpayment(planid, user.Id);
            return Ok(resul);
            
        }

        [HttpPost("Handlewebhook")]
        public async Task<IActionResult> Handlewebhook(webhookDto webhook)
        {
            
            var result=await ipayment.Handlewebhook(webhook);
            return Ok(result);
        }
    }
}
