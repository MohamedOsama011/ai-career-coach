using AICareerCoach.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.DAL.Entities;

public class Job
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string RequiredSkills { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public decimal Salary { get; set; }

    public DateTime PostedAt { get; set; }

    public string? CompanyLogoUrl { get; set; }

    public string? ExternalId { get; set; }

    public string? Source { get; set; } = "Adzuna";

    public string? ExternalUrl { get; set; }

    public string? ContractType { get; set; }

    public string? Category { get; set; }

    public bool IsRemote { get; set; }
}
