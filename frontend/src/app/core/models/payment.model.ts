export interface GeneralResponse {
  success: boolean;
  data: any;
}

export interface SubscriptionPlan {
  id: number;
  name: string;
  price: number;
}

export interface CreateSubscriptionRequest {
  name: string;
  price: number;
}

export interface CreatePaymentRequest {
  planId: string;
  userId: string;
}
