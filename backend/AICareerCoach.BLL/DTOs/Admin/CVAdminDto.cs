namespace AICareerCoach.BLL.DTOs.Admin
{
    public class CVAdminDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = "";
        public string UserEmail { get; set; } = "";
        public string FileName { get; set; } = "";
        public DateTime UploadDate { get; set; }
    }
}
