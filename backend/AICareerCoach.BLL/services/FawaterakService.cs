using AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.DTOs.Fawaterak;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Tsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.services
{
    public class FawaterakService : Ifawaterak
    {
        private readonly HttpClient httpClient;
        private readonly IConfiguration configuration;
        private readonly AICareerCoachDbContext context;
        public FawaterakService(HttpClient _httpClient, IConfiguration _configuration, AICareerCoachDbContext _context)
        {
            httpClient = _httpClient;
            configuration = _configuration;
            context = _context; 
        }
        public async Task<Generalresponse> createpayment(datasendedwhenclickonsubscriptionDTO dto)
        {
            var url = "";
            var response = new Generalresponse();
            var user = await context.Users.FirstOrDefaultAsync(x => x.Id == dto.userid);
            var plan = await context.Subscriptions.FirstOrDefaultAsync(x => x.Id.ToString() == dto.planid);
            if (plan == null)
            {
                response.Success = false;
                response.Data = "subscription not found";
            }
            else if (user == null)
            {
                response.Success = false;
                response.Data = "user not found not found";
            }
            else
            {
                var usersubscription = new UserSubscription
                {
                    Userid = dto.userid,
                    Subscriptionid = int.Parse(dto.planid),
                    Isactive = false,
                    Status = "pending",
                    

                };
                await context.AddAsync(usersubscription);
                await context.SaveChangesAsync();
                var payment = new Payment
                {
                    Usersubscriptionid = usersubscription.Id,
                    Status = "pending",
                    Amount = plan.Price,
                    invoicenumber = usersubscription.Id.ToString(),
                };
                await context.AddAsync(payment);
                await context.SaveChangesAsync();


                var res = await getallpaymentmethods();

                if (res != null)
                {
                    response.Success = true;
                    response.Data = res + usersubscription.Id.ToString();
                }
            }
                return response;

                //    var fawa = new FawaterakDto
                //    {

                //        first_name = user.UserName,
                //        last_name = user.FullName,
                //        email = user.Email,
                //        phone = user.PhoneNumber,
                //        cartTotal = plan.Price,
                //        currency= "EGP",
                //        cartitems_name = plan.Name,
                //        cartitems_price = plan.Price,
                //        cartitems_quantity = 1,
                //        invoice = payment.InternalTransactionid

                //    };

                //    url = await createfawaterakpayment(fawa);
                //}
                //return url;
            }

       public async Task<GetPaymentMethodsResponseDTO> getallpaymentmethods()
        {

            var request = new HttpRequestMessage(HttpMethod.Post, $"{configuration["Fawaterak:BaseUrl"]}/api/v2/getPaymentmethods");
            request.Headers.Add("Authorization", configuration["Fawaterak:ApiKey"]);
            request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("fawaterak error" + result);
            }

            var responseobject = JsonSerializer.Deserialize<GetPaymentMethodsResponseDTO>(result);

            return responseobject;

           
        }
        
        public async Task<object> Envoicecalling(string methodid, string usersubscriptionid)
        {
            var data = await context.UserSubscriptions.Include(u => u.Payments).Include(u => u.Subscription)
    .Include(u => u.User).FirstOrDefaultAsync(x => x.Id.ToString() == usersubscriptionid);

            var dto = new FawaterakDto();
                dto.first_name = data.User.UserName;
                dto.last_name = data.User.FullName;
                dto.email = data.User.Email;
                 dto.phone = data.User.PhoneNumber;
                 dto.cartTotal = data.Subscription.Price;
            dto.currency = "EGP";
                dto.cartitems_name = data.Subscription.Name;
                dto.cartitems_price = data.Subscription.Price;
                dto.cartitems_quantity = 1;
                 dto.invoice = data.Payments.FirstOrDefault(p => p.Usersubscriptionid.ToString() == usersubscriptionid).invoicenumber;
                 dto.payment_method_id = methodid;
 
            var pay = await Excutepayment(dto);
            return pay;
        }
        public  async Task<excutepaymentgeneralResponseDTO>  Excutepayment(FawaterakDto Dto)
        {
            var requestbody = new
            {
                payment_method_id=Dto.payment_method_id,
                cartTotal = Dto.cartTotal,
                currency = Dto.currency,
                invoice_number=Dto.invoice,
                customer = new
                {
                    first_name = Dto.first_name,
                    last_name = Dto.last_name,
                    email = Dto.email,
                    phone = Dto.phone,
                    address = Dto.address
                },
                cartItems = new[]
                {
                    new{
                    name = Dto.cartitems_name,
                    price = Dto.cartitems_price,
                    quantity = Dto.cartitems_quantity,
                    }
                    
                },
                redirectionUrls=new {
                successUrl = "https://dev.fawaterk.com/success",
                failUrl= "https://dev.fawaterk.com/fail",
                pendingUrl= "https://dev.fawaterk.com/pending"
     },
            };
            var  request = new HttpRequestMessage(HttpMethod.Post, $"{configuration["Fawaterak:BaseUrl"]}/api/v2/invoiceInitPay");
            request.Headers.Add("Authorization", configuration["Fawaterak:ApiKey"]);
            request.Content = new StringContent(JsonSerializer.Serialize(requestbody), Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("fawaterak error" + result);
            }

            var responseobject =
    JsonSerializer.Deserialize<excutepaymentgeneralResponseDTO>(
        result,
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
            if (responseobject == null)
            {
                throw new Exception("Failed to deserialize Fawaterk response.");
            }

            //        var firstProperty = responseobject.Data.Payment_Data
            //.EnumerateObject()
            //.FirstOrDefault();

            //        string methodName = "";

            //        if (!firstProperty.Equals(default(JsonProperty)))
            //        {
            //            if (firstProperty.Name.Contains("fawry", StringComparison.OrdinalIgnoreCase))
            //                methodName = "Fawry";
            //            else if (firstProperty.Name.Contains("wallet", StringComparison.OrdinalIgnoreCase))
            //                methodName = "Wallet";
            //            else if (firstProperty.Name.Contains("redirect", StringComparison.OrdinalIgnoreCase))
            //                methodName = "Visa/MasterCard";
            //}
            return responseobject;
        }
        public async Task<FawaterakcreatelinkpaymentResponseDTO> createfawaterakpayment(FawaterakDto Dto)
        {
            var requestbody = new
            {
                cartTotal=Dto.cartTotal,
                currency= Dto.currency,
                customer = new
                {
                   first_name=Dto.first_name,
                   last_name=Dto.last_name,
                    email=Dto.email,
                    phone=Dto.phone,
                    address=Dto.address
                },
                cartItems=new
                {
                    name = Dto.cartitems_name,
                    price = Dto.cartitems_price,
                    quantity = Dto.cartitems_price
                },
  
               
                //referenceid = Dto.InternalTransactionID,
                SucessUrl=$"{configuration["Appsettings:Baseurl"]}/payments/success",
                failedUrl = $"{configuration["Appsettings:Baseurl"]}/payments/fail",
                webhookUrl = $"{configuration["Appsettings:Baseurl"]}/payments/webhook"
            };
            var request=new HttpRequestMessage(HttpMethod.Post, $"{configuration["Fawaterak:BaseUrl"]}/api/v2/createInvoiceLink");
            request.Headers.Add("Authorization", configuration["Fawaterak:ApiKey"]);
            request.Content=new StringContent(JsonSerializer.Serialize(requestbody),Encoding.UTF8,"application/json");

            var response=await httpClient.SendAsync(request);
            var result=await response.Content.ReadAsStringAsync();
            if(!response.IsSuccessStatusCode)
            {
            throw new Exception("fawaterak error"+result);
            }

            var responseobject = JsonSerializer.Deserialize<FawaterakcreatelinkpaymentResponseDTO>(result);

            
            return responseobject;

        }












        public async Task<Generalresponse> Successwebhook(webhookSuccessDto dto)
        {

            
            if (!VerifyWebhookHash(dto))
            {
                return new Generalresponse
                {
                    Success = false,
                    Data = "Invalid webhook hash."
                };
            }

            var payment = await context.Payments
                .Include(x => x.UserSubscription)
                .FirstOrDefaultAsync(x => x.Invoiceid == dto.invoice_id.ToString());

            if (payment == null)
            {
                return new Generalresponse
                {
                    Success = false,
                    Data = "Payment not found."
                };
            }

            if (payment.Status == "paid")
            {
                return new Generalresponse
                {
                    Success = true,
                    Data = "Payment already processed."
                };
            }

            payment.Status = "paid";
            payment.PaymentMethod = dto.payment_method;
            payment.referenceNumber = dto.referenceNumber;

            payment.UserSubscription.Isactive = true;
            payment.UserSubscription.Status = "active";
            payment.UserSubscription.StartDate = DateTime.UtcNow;
            payment.UserSubscription.Enddate = DateTime.UtcNow.AddMonths(1);

            await context.SaveChangesAsync();

            return new Generalresponse
            {
                Success = true,
                Data = dto
            };
        }

        public async Task<Generalresponse> failedwebhook(FailedwebhookDTO dto)
        {
            
            //if(!failedwebhookHash(dto))
            //{
            //    return new Generalresponse
            //    {
            //        Success = false,
            //        Data = "3D Secure authentication failed"
            //    };
            //}
            var payment = await context.Payments.Where(p=>p.Invoiceid == dto.invoice_id.ToString()&&p.InvoiceKey == dto.invoice_key).FirstOrDefaultAsync();

            if (payment==null)
            {
                return new Generalresponse
                {
                    Success = false,
                    Data = "Invoice not found."
                };
            }
            
            

            payment.Status = "failed";
            payment.UserSubscription.Status = "Notactive";
            payment.UserSubscription.Isactive = false;
            await context.SaveChangesAsync();
            return new Generalresponse
            {
                Success = true,
                Data = dto
            };
           
        }

        public async Task<Generalresponse> Cancelwebhook(CancelwebhookDTO dto)
        {
            
            

            var response = new Generalresponse();
            if (!cancelWebhookHash(dto))
            {
                response.Success = false;
                response.Data = "expired link transaction ";

            }

            string invoiceNumber = dto.pay_load?.merchant_reference;


            var payment = await context.Payments.Include(x => x.UserSubscription).FirstOrDefaultAsync(x => x.invoicenumber.ToString() == invoiceNumber);
            if (payment == null)
            {
                response.Success = false;
                response.Data = "payment is not exist";
            }

            payment.Status = "Expired";

            payment.UserSubscription.Isactive = false;
            payment.PaymentMethod = dto.paymentMethod;
            payment.referenceNumber = dto.referenceId;
            payment.transactionid = dto.transactionId.ToString();
            payment.transactionkey = dto.transactionKey;
            await context.SaveChangesAsync();
            response.Data = dto;
            response.Success = true;
            return response;
        }

        private bool VerifyWebhookHash(webhookSuccessDto dto)
        {
            var secretKey = configuration["Fawaterak:SecreteKey"];

            var query=
                $"InvoiceId={dto.invoice_id}&InvoiceKey={dto.invoice_key}&PaymentMethod={dto.payment_method}";

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));

            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(query));

            var generatedHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            return generatedHash.Equals(dto.hashKey, StringComparison.OrdinalIgnoreCase);
        }
        private bool cancelWebhookHash(CancelwebhookDTO dto)
        {
            var secretKey = configuration["Fawaterak:SecreteKey"];

            var query =
                $"referenceId={dto.referenceId}&PaymentMethod={dto.paymentMethod}";

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));

            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(query));

            var generatedHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            return generatedHash.Equals(dto.hashKey, StringComparison.OrdinalIgnoreCase);
        }


        private bool failedwebhookHash(FailedwebhookDTO dto)
        {
            var secretKey = configuration["Fawaterak:SecreteKey"];
            var query =
                $"InvoiceId={dto.invoice_id}&InvoiceKey={dto.invoice_key}&PaymentMethod={dto.payment_method}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(query));
            var generatedHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
            return generatedHash.Equals(dto.response.gatewayCode, StringComparison.OrdinalIgnoreCase);
        }

        //public async Task<Generalresponse> Refundedwebhook(ApprovedWebhookDto dto)
        //{
        //    var response = new Generalresponse();
        //    var payment = await context.Payments.FirstOrDefaultAsync(x => x.referenceNumber.ToString() == dto.re);
        //    if (payment == null)
        //    {
        //        response.Success = false;
        //        response.Data = "payment is not exist";
        //    }

        //    payment.Status = "approved";
        //    payment.GatewayTransactionid = dto.transactionId;
        //    var subscription = payment.UserSubscription;
        //    subscription.Isactive = false;
        //    subscription.StartDate = DateTime.UtcNow;
        //    response.Data = dto;
        //    response.Success = true;


        //    await context.SaveChangesAsync();
        //    return response;
        //}
    }
}
