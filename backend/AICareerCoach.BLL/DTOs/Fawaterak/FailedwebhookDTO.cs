using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.Fawaterak
{
    public class FailedwebhookDTO
    {
           public string invoice_key { get; set; }
           public int invoice_id { get; set; }
           public string payment_method { get; set; }
           public object? pay_load;
           public decimal amount { get; set; }
           public string paidCurrency { get; set; }
           public string errorMessage { get; set; }
           public string referenceNumber { get; set; }
           public Response response { get; set; }
   
}

public class Response
{
    public string gatewayCode { get; set; }
    public string gatewayRecommendation { get; set; }
}
}
