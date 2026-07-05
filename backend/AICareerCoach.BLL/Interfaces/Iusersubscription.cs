using AICareerCoach.BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Interfaces
{
    public interface Iusersubscription
    {
        Task<List<UsersubresponseDTO>> getallbyuserid(string userid);
    }
}
