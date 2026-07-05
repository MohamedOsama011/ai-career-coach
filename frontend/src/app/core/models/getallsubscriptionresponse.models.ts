export interface getallsubscriptionresponse {
  data: SubscriptionPlan[];
}

export interface SubscriptionPlan {
  id: number;
  name: string;
  price: number;
  Description:string;
  Createdatat:Date;
  
  subscriptions: UserSubscription[];
}

export interface UserSubscription {
}

export interface createSubscriptionResponse
{
   name:string;
    price:number;
    Description:string;
}


export interface getonesubscriptionresponse {
  
  data: SubscriptionPlan;
}

export interface updateSubscriptionResponse
{
success:string;
data:object;
}