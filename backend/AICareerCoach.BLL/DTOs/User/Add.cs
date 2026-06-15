using System.ComponentModel.DataAnnotations;

namespace AICareerCoach.BLL.DTOs.User
{
    public class Add
    {
        [MaxLength(70)]
        public string Name { get; set; }

        public string email { get; set; }
    }
}
