using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.Fawaterak
{
    public class GetPaymentMethodsResponseDTO
    {
        public string status { get; set; }

        public Data data { get; set; }
    }
        public class Data
        {
            public int paymentId { get; set; }
            public string name_en { get; set; }
            public string name_ar { get; set; }
            public bool redirect { get; set; }
            public string logo { get; set; }
            
        }
    }

