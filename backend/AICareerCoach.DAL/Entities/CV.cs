using AICareerCoach.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace AICareerCoach.DAL.Entities;

public class CV
{
    public int CVId { get; set; }

    public int UserId { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; }

    // Navigation

    public User User { get; set; } = null!;

    public ICollection<Roadmap> Roadmaps { get; set; } = new List<Roadmap>();

    public ICollection<Interview> Interviews { get; set; } = new List<Interview>();
}
