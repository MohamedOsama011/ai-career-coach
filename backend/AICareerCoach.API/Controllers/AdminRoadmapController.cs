using AICareerCoach.BLL.DTOs.Admin;
using AICareerCoach.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.API.Controllers
{
    [Route("api/admin/roadmap-templates")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminRoadmapController : ControllerBase
    {
        private readonly IAdminRoadmapService _adminRoadmapService;

        public AdminRoadmapController(IAdminRoadmapService adminRoadmapService)
        {
            _adminRoadmapService = adminRoadmapService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var templates = await _adminRoadmapService.GetAllTemplatesAsync();
            return Ok(templates);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var template = await _adminRoadmapService.GetTemplateAsync(id);
                return Ok(template);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Roadmap template with id {id} not found." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRoadmapTemplateDto dto)
        {
            try
            {
                var template = await _adminRoadmapService.CreateTemplateAsync(dto);
                return CreatedAtAction(nameof(Get), new { id = template.Id }, template);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to create roadmap template.", detail = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRoadmapTemplateDto dto)
        {
            try
            {
                var template = await _adminRoadmapService.UpdateTemplateAsync(id, dto);
                return Ok(template);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Roadmap template with id {id} not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to update roadmap template.", detail = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _adminRoadmapService.DeleteTemplateAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Roadmap template with id {id} not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete roadmap template.", detail = ex.Message });
            }
        }

        [HttpPost("{id:int}/test-match")]
        public async Task<IActionResult> TestMatch(int id, [FromBody] TestMatchRequestDto? request)
        {
            try
            {
                var result = await _adminRoadmapService.TestMatchAsync(id, request?.SampleCvText);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Roadmap template with id {id} not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to run test match.", detail = ex.Message });
            }
        }
    }
}
