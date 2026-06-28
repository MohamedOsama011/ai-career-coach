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

        Task<Generalresponse> createpayment(datasendedwhenclickonsubscriptionDTO dto);
        Task<object> Envoicecalling(string methodid, string usersubscriptionid);
        Task<Generalresponse> Successwebhook(dynamic dto);    
        Task<Generalresponse> failedwebhook(dynamic dto);
        Task<Generalresponse> Cancelwebhook(dynamic dto);
        Task<GetPaymentMethodsResponseDTO> getallpaymentmethods();
        Task<excutepaymentgeneralResponseDTO> Excutepayment(FawaterakDto fawaterakDto);
        Task<FawaterakcreatelinkpaymentResponseDTO> createfawaterakpayment(FawaterakDto fawaterakDto);



    }
}
