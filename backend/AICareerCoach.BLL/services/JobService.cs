using AICareerCoach.BLL.DTOs.Common;
using AICareerCoach.BLL.DTOs.Job;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.repository;
using System.Text.Json;

namespace AICareerCoach.BLL.Services
{
    public class JobService : IJobService
    {
        private readonly IJobRepository _jobRepo;

        public JobService(IJobRepository jobRepo) => _jobRepo = jobRepo;

        public async Task<PagedResult<JobDto>> GetJobsAsync(JobFilterDto filter)
        {
            var (items, total) = await _jobRepo.GetPagedAsync(filter.Search, filter.Location, filter.MinSalary, filter.Page, filter.PageSize);

            return new PagedResult<JobDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = total,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<JobDto> GetByIdAsync(int id)
        {
            var job = await _jobRepo.GetByIdAsync(id);
            if (job is null) throw new KeyNotFoundException($"Job with id {id} not found.");
            return MapToDto(job);
        }

        public async Task<JobDto> CreateAsync(CreateJobDto dto)
        {
            var job = new Job
            {
                Title = dto.Title,
                Company = dto.Company,
                Description = dto.Description,
                RequiredSkills = JsonSerializer.Serialize(dto.RequiredSkills),
                Location = dto.Location,
                Salary = dto.Salary,
                PostedAt = DateTime.UtcNow
            };

            await _jobRepo.AddAsync(job);
            return MapToDto(job);
        }

        private static JobDto MapToDto(Job job) => new()
        {
            Id = job.Id,
            Title = job.Title,
            Company = job.Company,
            Description = job.Description,
            RequiredSkills = JsonSerializer.Deserialize<List<string>>(job.RequiredSkills) ?? new(),
            Location = job.Location,
            Salary = job.Salary,
            PostedAt = job.PostedAt
        };
    }
}
