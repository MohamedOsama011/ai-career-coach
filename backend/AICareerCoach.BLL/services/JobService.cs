using AICareerCoach.BLL.DTOs.Common;
using AICareerCoach.BLL.DTOs.Job;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.services
{
    public class JobService : IJobService
    {
        private readonly AICareerCoachDbContext _context;
        public JobService(AICareerCoachDbContext context) => _context = context;
        public async Task<PagedResult<JobDto>> GetJobsAsync(JobFilterDto filter)
        {
            var query = _context.Jobs.AsQueryable();

            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(j =>
                    j.Title.Contains(filter.Search) ||
                    j.Company.Contains(filter.Search) ||
                    j.Description.Contains(filter.Search));

            if (!string.IsNullOrEmpty(filter.Location))
                query = query.Where(j => j.Location.Contains(filter.Location));

            if (filter.MinSalary.HasValue)
                query = query.Where(j => j.Salary >= filter.MinSalary.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(j => j.PostedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

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
            var job = await _context.Jobs.FindAsync(id);
            if (job is null) throw new Exception("Job not found.");

            return MapToDto(job); 
        }

        public async Task<JobDto> CreateAsync(CreateJobDto dto)
        {
            var job = new Job
            {
                Title = dto.Title,
                Company = dto.Company,
                Description = dto.Description,
                RequiredSkills = string.Join(',', dto.RequiredSkills),
                Location = dto.Location,
                Salary = dto.Salary,
                PostedAt = DateTime.UtcNow
            };

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

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
