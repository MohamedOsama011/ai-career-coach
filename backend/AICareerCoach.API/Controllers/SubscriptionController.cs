using AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _subscriptionService.GetAllSubscriptionsAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var result = await _subscriptionService.GetSubscriptionByIdAsync(id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SubscriptionDto dto)
        {
            await _subscriptionService.CreateSubscriptionAsync(dto);
            return Ok();
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromBody] Subscription subscription)
        {
            await _subscriptionService.DeleteSubscriptionAsync(subscription);
            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] SubscriptionDto dto)
        {
            var result = await _subscriptionService.UpdateSubscriptionAsync(dto, id);
            return Ok(result);
        }
    }
}
