using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.Auth
{
    public class ChangePassword
    {
        [Required (ErrorMessage ="password is required")]
        public string OldPassword { get; set; }
        [Required (ErrorMessage ="password is required")]

		public string NewPassword { get; set; }
        [Required (ErrorMessage ="password is required")]

		public string ConfirmNewPassword { get; set; }


	}
}
