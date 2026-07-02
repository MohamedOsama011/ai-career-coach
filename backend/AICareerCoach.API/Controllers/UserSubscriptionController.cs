using AICareerCoach.BLL.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserSubscriptionController : ControllerBase
    {
       private readonly Iusersubscription Iusersubscription;

        public UserSubscriptionController(Iusersubscription _Iusersubscription)
        {
            Iusersubscription = _Iusersubscription;
        }


        [HttpGet("Getusersubscriptionbyuser/{userid}")]
        public async Task<IActionResult> Get(string userid)
        {
            var result =await  Iusersubscription.getallbyuserid(userid);
            return Ok(result);
        }

    }
}
