using System;
using System.Collections.Generic;
using System.Text;

namespace AICareerCoach.DTO.DTOS.USER
{
    internal class Get
    {
		public string Name { get; set; }
		public string email { get; set; }
		public string CareerGoal { get; set; }


		//relations  
		//use lazy loading  by using hash set
		public ICollection<Roadmap>? Roadmaps { get; set; } = new HashSet<Roadmap>();  //want to be updated
		public ICollection<mockInterview>? MockInterviews { get; set; } = new HashSet<mockInterview>();
		public ICollection<CV>? CVs { get; set; } = new HashSet<CV>();
	}
}
