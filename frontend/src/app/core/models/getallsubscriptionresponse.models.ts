export  interface getallsubscriptionresponse 
{
    status: string;
    data: SubscriptionPlan[];
    usersubscriptionid:string;
}

export interface SubscriptionPlan {
  id: number;
  name: string;
    price: number;
    usersubscriptions:UserSubscriptions[];
}
export interface UserSubscriptions {
  // Add properties when the API starts returning subscription data.
  anything:object;
}


export interface createSubscriptionResponse
{
   name:string;
    price:number;
}




export interface updateSubscriptionResponse
{
success:string;
data:object;
}