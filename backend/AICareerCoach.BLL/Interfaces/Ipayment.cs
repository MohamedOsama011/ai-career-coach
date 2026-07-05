using AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.DTOs.Fawaterak;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Interfaces
{
    public interface Ipayment
    {
        Task<List<PaymentDTO>> Getallpaymentsbyid(string userid);

        //Task<Generalresponse> Handlewebhook(webhookDto webhookDto);
        //Task<Generalresponse> Successwebhook(webhookSuccessDto dto);
        //Task<Generalresponse> failedwebhook(FailedwebhookDTO dto);
        //Task<Generalresponse> Approvedwebhook(ApprovedWebhookDto dto);
        //Task<Generalresponse> Cancelwebhook(CancelwebhookDTO dto);


        //Task <Generalresponse>  Gerbyuserid(string userid);
    }
}
