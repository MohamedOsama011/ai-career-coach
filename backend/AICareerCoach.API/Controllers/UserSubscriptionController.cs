using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserSubscriptionController : ControllerBase
    {
       private readonly Iusersubscription Iusersubscription;
        private readonly UserManager<User> userManager;
        public UserSubscriptionController(Iusersubscription _Iusersubscription, UserManager<User> _userManager)
        {
            Iusersubscription = _Iusersubscription;
            userManager = _userManager;
        }


        [HttpGet("Getusersubscriptionbyuser")]
        public async Task<IActionResult> Get()
        {
            var user = await userManager.GetUserAsync(User);

            var result =await  Iusersubscription.getallbyuserid(user.Id);
            return Ok(result);
        }

    }
}
