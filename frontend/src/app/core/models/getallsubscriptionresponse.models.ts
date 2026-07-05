export interface getallsubscriptionresponse {
  data: SubscriptionPlan[];
}

export interface SubscriptionPlan {
  id: number;
  name: string;
  price: number;
  description:string;
  createdatat:Date;
  updatedat:Date|null
  subscriptions: UserSubscription[];
}

export interface UserSubscription {
}

export interface createSubscriptionResponse
{
   name:string;
    price:number;
    description:string;
}


export interface getonesubscriptionresponse {
  
  data: SubscriptionPlan;
}

export interface updateSubscriptionResponse
{
success:string;
data:object;
}













export interface Usersubscripions{
   id: number;
  subscriptionName: string;
  status: string;
  startDate: Date | null;
  endDate: Date | null; Id:string

}