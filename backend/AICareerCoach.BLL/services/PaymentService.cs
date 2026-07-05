using AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.DTOs.Fawaterak;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;


namespace AICareerCoach.BLL.services
{
    public class PaymentService : Ipayment

    {
        private readonly AICareerCoachDbContext context;
        public PaymentService(AICareerCoachDbContext _context)
        {
            context = _context;


        }

        public async Task<List<PaymentDTO>> Getallpaymentsbyid(string userId)
        {
            return await context.UserSubscriptions
                .Where(us =>
                    us.Userid == userId &&
                    (us.Status == "pending" ||
                     us.Status == "active" ||
                     us.Status == "ended"))
                .Include(us => us.Subscription)
                .Include(us => us.Payments)
                .SelectMany(
                    us => us.Payments.DefaultIfEmpty(),
                    (us, payment) => new PaymentDTO
                    {
                        SubscriptionName = us.Subscription.Name,
                        SubscriptionStatus = us.Status,
                        PaymentStatus = payment.Status,
                        PaidAt = payment.paidat
                    })
                .ToListAsync();
        }

        


































        //public async Task<string> createpayment(string planid, string userid)
        //{
        //    var url = "";
        //    var response = new Generalresponse();
        //    //var fawa=new FawaterakDto();
        //    var user = await context.Users.FindAsync(userid);
        //    var plan = await context.Subscriptions.FirstOrDefaultAsync(x => x.Id == planid);
        //    if (plan == null)
        //    {
        //        response.Success = false;
        //        response.Data = "subscription not found";
        //    }
        //    else if (user == null)
        //    {
        //        response.Success = false;
        //        response.Data = "user not found not found";
        //    }
        //    else
        //    {
        //        var usersubscription = new UserSubscription
        //        {
        //            Userid = userid,
        //            Subscriptionid = planid,
        //            Isactive = false,

        //            //Status="notactivated"
        //        };
        //        await context.AddAsync(usersubscription);
        //        await context.SaveChangesAsync();
        //        var payment = new Payment
        //        {
        //            Usersubscriptionid = usersubscription.Id,
        //            Status = "pending",
        //            Amount = plan.Price,
        //            Invoiceid = null,
        //            InternalTransactionid = Guid.NewGuid().ToString(),
        //            GatewayTransactionid = null,
        //            Paymentprovider = "Fawaterac"
        //        };
        //        await context.AddAsync(payment);
        //        await context.SaveChangesAsync();

        //        var fawa = new FawaterakDto
        //        {
        //            Name = user.UserName,
        //            Email = user.Email,
        //            Amount = plan.Price,
        //            InternaltransactionId = payment.InternalTransactionid
        //        };

        //        url = await ifawaterak.createfawaterakpayment(fawa);
        //    }
        //    return url;

        //}

        //public  async Task<Generalresponse> Handlewebhook(webhookDto dto)
        // {
        //     var response=new Generalresponse();
        //     var payment = await context.Payments.FirstOrDefaultAsync(x =>x.InternalTransactionid.ToString()==dto.Referenceid);
        //     if(payment==null)
        //     {
        //         response.Success = false;
        //         response.Data = "payment is not exist";
        //     }
        //     if(dto.status=="Paid")
        //     {
        //         payment.Status=dto.status;
        //         payment.GatewayTransactionid = dto.Transactionid;
        //         payment.Invoiceid = dto.Invoiceid;
        //         var subscription = payment.UserSubscription;
        //         subscription.Isactive = true;
        //         //subscription.Status = "Active";
        //         subscription.StartDate = DateTime.UtcNow;
        //         subscription.Enddate = DateTime.UtcNow.AddMonths(1);
        //         response.Data = "sucess";
        //         response.Success = true;
        //     }
        //     if(dto.status=="Failed")
        //     {
        //         payment.Status = dto.status;
        //         payment.GatewayTransactionid = dto.Transactionid;
        //         payment.Invoiceid = dto.Invoiceid;
        //         var subscription = payment.UserSubscription;
        //         subscription.Isactive = false;
        //         //subscription.Status = "NotActive";
        //         response.Data = "failed";
        //         response.Success = false;
        //     }
        //     await context.SaveChangesAsync();
        //     return response;
        // }

        //public async Task<Generalresponse> Successwebhook(webhookSuccessDto dto)
        //{
        //    if (!VerifyWebhookHash(dto))
        //    {
        //        return new Generalresponse
        //        {
        //            Success = false,
        //            Data = "Invalid webhook hash."
        //        };
        //    }

        //    var payment = await context.Payments
        //        .Include(x => x.UserSubscription)
        //        .FirstOrDefaultAsync(x => x.Invoiceid == dto.invoice_id.ToString());

        //    if (payment == null)
        //    {
        //        return new Generalresponse
        //        {
        //            Success = false,
        //            Data = "Payment not found."
        //        };
        //    }

        //    if (payment.Status == "paid")
        //    {
        //        return new Generalresponse
        //        {
        //            Success = true,
        //            Data = "Payment already processed."
        //        };
        //    }

        //    payment.Status = "paid";
        //    payment.Invoiceid = dto.invoice_id.ToString();
        //    payment.GatewayTransactionid = dto.referenceNumber;

        //    payment.UserSubscription.Isactive = true;
        //    payment.UserSubscription.StartDate = DateTime.UtcNow;
        //    payment.UserSubscription.Enddate = DateTime.UtcNow.AddMonths(1);

        //    await context.SaveChangesAsync();

        //    return new Generalresponse
        //    {
        //        Success = true,
        //        Data = dto
        //    };
        //}

        //public async Task<Generalresponse> failedwebhook(FailedwebhookDTO dto)
        //{
        //    var response = new Generalresponse();
        //    var payment = await context.Payments.FirstOrDefaultAsync(x => x.InternalTransactionid.ToString() == dto.referenceNumber);
        //    if (payment == null)
        //    {
        //        response.Success = false;
        //        response.Data = "payment is not exist";
        //    }

        //    payment.Status = "failed";
        //    payment.Invoiceid = dto.invoice_id.ToString();

        //    var subscription = payment.UserSubscription;
        //    subscription.Isactive = false;
        //    subscription.StartDate = DateTime.UtcNow;
        //    response.Data = dto;
        //    response.Success = true;


        //    await context.SaveChangesAsync();
        //    return response;
        //}
        //public async Task<Generalresponse> Approvedwebhook(ApprovedWebhookDto dto)
        //{
        //    var response = new Generalresponse();
        //    var payment = await context.Payments.FirstOrDefaultAsync(x => x.InternalTransactionid.ToString() == dto.referenceNumber);
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

        //public async Task<Generalresponse> Cancelwebhook(CancelwebhookDTO dto)
        //{
        //    var response = new Generalresponse();
        //    var payment = await context.Payments.FirstOrDefaultAsync(x => x.InternalTransactionid.ToString() == dto.referenceId);
        //    if (payment == null)
        //    {
        //        response.Success = false;
        //        response.Data = "payment is not exist";
        //    }

        //    payment.Status = "Expired";
        //    payment.GatewayTransactionid = dto.transactionId.ToString();
        //    var subscription = payment.UserSubscription;
        //    subscription.Isactive = false;
        //    subscription.StartDate = DateTime.UtcNow;
        //    response.Data = dto;
        //    response.Success = true;


        //    await context.SaveChangesAsync();
        //    return response;
        //}




        //private bool VerifyWebhookHash(webhookSuccessDto dto)
        //{
        //    var secretKey = configuration["Fawaterak:SecreteKey"];

        //    var query =
        //        $"InvoiceId={dto.invoice_id}&InvoiceKey={dto.invoice_key}&PaymentMethod={dto.payment_method}";

        //    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));

        //    var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(query));

        //    var generatedHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        //    return generatedHash.Equals(dto.hashKey, StringComparison.OrdinalIgnoreCase);
        //}


    }
}

