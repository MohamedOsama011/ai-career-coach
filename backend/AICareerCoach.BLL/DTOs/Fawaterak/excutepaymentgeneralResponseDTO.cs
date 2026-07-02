using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.Fawaterak
{
    public class excutepaymentgeneralResponseDTO
    {
        
            public string Status { get; set; }
        public string message { get; set; }


        public FawaterakDataDto Data { get; set; }
        }

        public class FawaterakDataDto
        {
            public string intent_key { get; set; }

            public int expires_in { get; set; }

            public PaymentDataDto Payment_Data { get; set; }
        }

        public class PaymentDataDto
        {
            public string RedirectTo { get; set; }
        }
    }

