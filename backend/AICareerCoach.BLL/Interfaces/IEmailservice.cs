using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Interfaces
{
    public interface IEmailservice
    {
        Task Sendemail(string receiver,string subject, string body);
    }
}
