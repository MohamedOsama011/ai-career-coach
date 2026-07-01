using AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.DTOs.Fawaterak;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Interfaces
{
    public interface Ifawaterak
    {

        Task<fawaterakresponsepaymentmethodsDTO> createpayment(datasendedwhenclickonsubscriptionDTO dto);
        Task<object> Envoicecalling(string methodid, string usersubscriptionid);
        Task<GetPaymentMethodsResponseDTO> getallpaymentmethods();
        Task<excutepaymentgeneralResponseDTO> Excutepayment(FawaterakDto fawaterakDto);
        Task<Gettransactionresponse> GettransactionData(gettransactionDTO fawaterakDto); 

        //Task<Generalresponse> Successwebhook(webhookSuccessDto dto);    
        //Task<Generalresponse> failedwebhook(FailedwebhookDTO dto);
        //Task<Generalresponse> Cancelwebhook(CancelwebhookDTO dto);



    }
}
