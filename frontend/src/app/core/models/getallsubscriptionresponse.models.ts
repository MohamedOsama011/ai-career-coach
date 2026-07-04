export interface getallsubscriptionresponse {
  success: boolean;
  data: SubscriptionPlan[];
}

export interface SubscriptionPlan {
  id: number;
  name: string;
  price: number;
  subscriptions: UserSubscription[];
}

export interface UserSubscription {
}

export interface createSubscriptionResponse
{
   name:string;
    price:number;
}


export interface getonesubscriptionresponse {
  success: boolean;
  data: SubscriptionPlan;
}

export interface updateSubscriptionResponse
{
success:string;
data:object;
}