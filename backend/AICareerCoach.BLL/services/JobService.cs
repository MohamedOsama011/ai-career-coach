using AICareerCoach.BLL.DTOs.Common;
using AICareerCoach.BLL.DTOs.Job;
using AICareerCoach.BLL.Helpers;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Services
{
    public class JobService : IJobService
    {
        private readonly IJobRepository _jobRepo;
        private readonly IEmbeddingService _embeddingService;
        private readonly AICareerCoachDbContext _context;

        public JobService(IJobRepository jobRepo, IEmbeddingService embeddingService, AICareerCoachDbContext context)
        {
            _jobRepo = jobRepo;
            _embeddingService = embeddingService;
            _context = context;
        }

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
                CompanyLogoUrl = dto.CompanyLogoUrl,
                PostedAt = DateTime.UtcNow,
                Source = string.IsNullOrEmpty(dto.Source) ? "Manual" : dto.Source,
                IsRemote = dto.IsRemote,
                ExternalUrl = dto.ExternalUrl
            };

            await _jobRepo.AddAsync(job);

            await EmbedJobAsync(job);

            return MapToDto(job);
        }

        public async Task<JobDto> UpdateAsync(int id, UpdateJobDto dto)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job is null) throw new KeyNotFoundException($"Job with id {id} not found.");

            job.Title = dto.Title;
            job.Company = dto.Company;
            job.Description = dto.Description;
            job.RequiredSkills = JsonSerializer.Serialize(dto.RequiredSkills);
            job.Location = dto.Location;
            job.Salary = dto.Salary;
            job.CompanyLogoUrl = dto.CompanyLogoUrl;
            if (dto.IsRemote.HasValue) job.IsRemote = dto.IsRemote.Value;
            if (dto.ExternalUrl != null) job.ExternalUrl = dto.ExternalUrl;

            await _context.SaveChangesAsync();

            await _context.JobEmbeddings
                .Where(je => je.JobId == id)
                .ExecuteDeleteAsync();

            await EmbedJobAsync(job);

            return MapToDto(job);
        }

        public async Task DeleteAsync(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job is null) throw new KeyNotFoundException($"Job with id {id} not found.");

            await _context.JobEmbeddings
                .Where(je => je.JobId == id)
                .ExecuteDeleteAsync();

            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();
        }

        private async Task EmbedJobAsync(Job job)
        {
            var cleanedSkills = job.RequiredSkills;
            var combinedText = $"Title: {job.Title}\nCompany: {job.Company}\nDescription: {job.Description}\nSkills: {cleanedSkills}";

            var embedding = await _embeddingService.GenerateEmbeddingAsync(combinedText);

            var jobEmbedding = new JobEmbedding
            {
                JobId = job.Id,
                Embedding = embedding,
                ComputedAt = DateTime.UtcNow
            };
            await _context.JobEmbeddings.AddAsync(jobEmbedding);
            await _context.SaveChangesAsync();
        }

        private static JobDto MapToDto(Job job) => new()
        {
            Id = job.Id,
            Title = job.Title,
            Company = job.Company,
            Description = HtmlHelper.StripHtml(job.Description),
            RequiredSkills = JsonSerializer.Deserialize<List<string>>(job.RequiredSkills) ?? new(),
            Location = job.Location,
            Salary = job.Salary,
            PostedAt = job.PostedAt,
            CompanyLogoUrl = job.CompanyLogoUrl,
            ExternalUrl = job.ExternalUrl,
            ContractType = job.ContractType,
            IsRemote = job.IsRemote,
            Category = job.Category,
            Source = job.Source
        };
    }
}
