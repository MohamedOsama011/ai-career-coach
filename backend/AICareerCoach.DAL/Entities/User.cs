using AICareerCoach.DAL.Entities;

namespace AICareerCoach.DAL.Entities
{
    public class User
    {

        //properties
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; } 
        public string CareerGoal { get; set; } 


        //relations  
        //use lazy loading  by using hash set
        public ICollection<Roadmap>? Roadmaps { get; set; }= new HashSet<Roadmap>();  //want to be updated
		public ICollection<mockInterview>? MockInterviews { get; set; }= new HashSet<mockInterview>();
        public ICollection<CV>? CVs { get; set; } = new HashSet<CV>();
    }
}