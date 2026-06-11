using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.BLL.DTOs.Auth
{
    public class ForgotPassword
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        //NavigationManager navigationManager { get; set; }
    }
}
