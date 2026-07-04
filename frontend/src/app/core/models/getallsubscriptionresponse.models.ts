export interface SubscriptionItem {
  id: number;
  name: string;
  price: number;
  userSubscriptions?: any[];
}

export interface CreateSubscriptionResponse {
  name: string;
  price: number;
}
