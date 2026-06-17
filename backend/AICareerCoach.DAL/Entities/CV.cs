using AICareerCoach.DAL.Models;
using System;
using System.Collections.Generic;

namespace AICareerCoach.DAL.Entities;

public class CV
{
    public string CVId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string filehashing { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string Extracteddata { get; set; }

    public User? User { get; set; } = null!;


    public virtual ICollection<Roadmap>? Roadmaps { get; set; } = new List<Roadmap>();

    public virtual ICollection<mockInterview>? Interviews { get; set; } = new List<mockInterview>();


}
