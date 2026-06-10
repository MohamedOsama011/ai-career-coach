using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.Auth
{
    public class CangePassword
    {
        [Required]
        public string OldPassword { get; set; }
		[Required]

		public string NewPassword { get; set; }
		[Required]

		public string ConfirmNewPassword { get; set; }


	}
}
