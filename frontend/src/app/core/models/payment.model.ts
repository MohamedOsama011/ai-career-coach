export  interface getallallpaymentmethods
{
    data: SubscriptionPlan;
    usersubscriptionid:string;
}

export interface SubscriptionPlan {
status: string;
vendorSettingsData:VendorSettingsData;
data:Details[];

}

export interface VendorSettingsData {
  custome_iframe_title:object;
}
export interface Details{
    payment_method_id:number;
    name_en:string;
    name_ar:string;
    redirect:string;
    logo:string;
}



export interface Excutepaymentresponse{
status:string;
message:any;
data:response;

}
export interface response{
intent_key:string;
expires_in:number;
payment_Data:Paymetlink;
}

export interface Paymetlink{
   redirectTo:string; 
}

