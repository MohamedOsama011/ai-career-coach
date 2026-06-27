using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.Fawaterak
{
    public class FawaterakcreatelinkpaymentResponseDTO
    {
        public string status { get; set; }
        public Data1 data { get; set; }



    }
    public class Data1
        {
            public string url { get; set; }
            public string invoiceKey { get; set; }
            public int invoiceId { get; set; }
        }
    }
