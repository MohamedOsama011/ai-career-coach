namespace AICareerCoach.BLL.DTOs.Job
{
    public class JobFilterDto
    {
        public string? Search { get; set; }
        public string? Location { get; set; }
        public decimal? MinSalary { get; set; }
        public bool? IsRemote { get; set; }
        public string? JobIds { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
