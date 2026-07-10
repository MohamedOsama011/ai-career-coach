using AICareerCoach.DAL.Models;
using System;
using System.Collections.Generic;

namespace AICareerCoach.DAL.Entities;

public class CV
{
    public int CVId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string FileHash { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string? ExtractedData { get; set; }

    public User? User { get; set; } = null!;


    public virtual ICollection<Roadmap>? Roadmaps { get; set; } = new List<Roadmap>();


}
