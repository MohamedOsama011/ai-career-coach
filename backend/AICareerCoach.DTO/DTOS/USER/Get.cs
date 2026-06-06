using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AICareerCoach.DAL.Entities;

namespace AICareerCoach.DTO1.DTOS.USER
{
    public class Get
    {
		public string name { get; set; }
		public string email { get; set; }
		public string title{ get; set; }

		public List<Roadmap>? Roadmaps;//want to be updated
		public List<mockInterview>? MockInterviews;
		public List<CV>? CVs;
	}
}
