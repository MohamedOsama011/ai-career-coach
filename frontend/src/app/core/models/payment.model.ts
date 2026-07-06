export interface Payment {

  id:number;

  userName:string;

  email:string;

  plan:string;

  amount:number;

  paymentMethod:string;

  paymentDate:Date;

  status:'Paid' | 'Pending' | 'Failed';

  transactionId:string;

}