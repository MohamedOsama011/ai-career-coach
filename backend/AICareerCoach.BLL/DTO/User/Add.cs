using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTO.User
{
    public class Add
    {
		[MaxLength(70)]
		public string Name { get; set; }
		public string email { get; set; }
	}
}
