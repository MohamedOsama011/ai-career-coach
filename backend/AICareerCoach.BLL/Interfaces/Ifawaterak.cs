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
        Task<FawaterakcreatelinkpaymentResponseDTO> createfawaterakpayment(FawaterakDto fawaterakDto);

        Task<GetPaymentMethodsResponseDTO> getallpaymentmethods();
        Task<excutepaymentgeneralResponseDTO> Excutepayment(FawaterakDto fawaterakDto,string methodid);


    }
}
