using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.DAL.Entities
{
    public class JobEmbedding
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public float[] Embedding { get; set; } = Array.Empty<float>();

        public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
        public Job Job { get; set; } = null!;

    }
}
