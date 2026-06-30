using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Interfaces.AI
{
    public interface ISkillExtractionService
    {
        Task<Dictionary<string, List<string>>> ExtractSkillsBatchAsync(
            List<(string Id, string Title, string Description)> jobs,
            CancellationToken ct);
    }
}
