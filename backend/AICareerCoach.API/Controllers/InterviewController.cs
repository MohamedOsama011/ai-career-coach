using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InterviewController : ControllerBase
    {
        [HttpGet("options")]
        public IActionResult GetOptions()
        {
            return StatusCode(501, "Not implemented until Phase 3");
        }

        [HttpPost("sessions")]
        public IActionResult StartSession()
        {
            return StatusCode(501, "Not implemented until Phase 3");
        }

        [HttpGet("sessions/active")]
        public IActionResult GetActiveSession()
        {
            return StatusCode(501, "Not implemented until Phase 3");
        }

        [HttpPost("sessions/{sessionId}/answers")]
        public IActionResult SubmitAnswer(int sessionId)
        {
            return StatusCode(501, "Not implemented until Phase 3");
        }

        [HttpGet("sessions/{sessionId}/scorecard")]
        public IActionResult GetScorecard(int sessionId)
        {
            return StatusCode(501, "Not implemented until Phase 3");
        }

        [HttpGet("sessions")]
        public IActionResult GetHistory()
        {
            return StatusCode(501, "Not implemented until Phase 3");
        }
    }
}
