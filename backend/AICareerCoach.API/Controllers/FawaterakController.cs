using AICareerCoach.BLL.DTOs.Fawaterak;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FawaterakController : ControllerBase
    {
        private readonly Ifawaterak ifawaterak;
        private readonly ILogger<FawaterakController> logger;
        private readonly  UserManager<User> userManager;
        public FawaterakController(Ifawaterak _ifawaterak,ILogger<FawaterakController> _logger,UserManager<User> _userManager)
        {
            ifawaterak = _ifawaterak;
            logger = _logger;
            userManager = _userManager;
        }

        [HttpGet("getallpaymentmethods")]
        
        public async Task<IActionResult> createpayment(string planid)
        {
            var user=await userManager.GetUserAsync(User);
            var result= await ifawaterak.createpayment(planid,user.Id);
            return Ok(result);
        }

        [HttpPost("envoicepaymet")]
        
        public async Task<IActionResult>  excuteenvoice(string methodid,string usersubscriptionid)
        {
            var result=await ifawaterak.Envoicecalling(methodid,usersubscriptionid) ;
            return Ok(result);

        }

        [HttpPost("successwebhook")]

        public async Task<IActionResult> Successwebhook(dynamic dto)
        {
            var result = await ifawaterak.Successwebhook(dto);
            return Ok(result);
        }


        [HttpPost("failedwebhook")]
        public async Task<IActionResult> failedwebhook(dynamic dto)
        {
            var result=await ifawaterak.failedwebhook(dto);
            return BadRequest(result);
        }

        [HttpPost("Cancelwebhook")]
        public async Task<IActionResult> Cancelwebhook(dynamic dto)
        {
            var res = await ifawaterak.Cancelwebhook(dto);
            return Ok(res);
        }



    }
}
