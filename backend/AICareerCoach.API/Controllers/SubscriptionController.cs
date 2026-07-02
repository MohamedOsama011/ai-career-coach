using AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubsription subsription;
        private readonly AICareerCoachDbContext context;
        public SubscriptionController(ISubsription _sub,AICareerCoachDbContext _context)
        {
            subsription = _sub;
            context = _context;
        }


        [HttpGet("Getall")]
        public async Task<IActionResult> Getall()
        {
            var result=await subsription.Getall();
            return Ok(result);
        }


        [HttpGet("getsubscription/{id}")]
        public async Task  <IActionResult> Getbyid(string id)
        {
            var result=await subsription.Get(id);
            return Ok(result);
        }


        [HttpDelete("Delete/{id}")]
        
        public async Task Delete(string id)
        {

            var sub= context.Subscriptions.FirstOrDefault(x=>x.Id.ToString()==id);
             subsription.DeleteSubscription(sub);
        }


        [HttpPost("Create")]
        
        public async Task Create(SubscriptionDTO sub)
        {
            subsription.CreateSubscription(sub);
        }


        [HttpPut("Update/{id}")]

        public async Task<IActionResult> Update(string id, SubscriptionDTO subscription)
        {
            var sub=await subsription.UpdateSubscription(subscription, id);
            return Ok( sub);
        }

    }
}
