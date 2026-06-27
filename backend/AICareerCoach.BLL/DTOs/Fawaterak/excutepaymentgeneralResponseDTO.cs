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

            public FawaterakDataDto Data { get; set; }
        }

        public class FawaterakDataDto
        {
            public int Invoice_Id { get; set; }

            public string Invoice_Key { get; set; }

            public PaymentDataDto Payment_Data { get; set; }
        }

        public class PaymentDataDto
        {
            public string RedirectTo { get; set; }
        }
    }

