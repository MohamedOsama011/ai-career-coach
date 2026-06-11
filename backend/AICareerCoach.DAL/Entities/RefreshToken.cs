using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AICareerCoach.DAL.Models;

namespace AICareerCoach.DAL.Entities
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime Expirydate { get; set; }

        public string Token { get; set; }

        public int Userid { get; set; }

        public virtual User? User { get; set; }

    }
}
