using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.Auth
{
    public class ResetPassword
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        public string  token { get; set; }
        [Required]
        public string Password { get; set; }
        [Compare("Password")]
		public string ConfirmPassword { get; set; }

	}
}
