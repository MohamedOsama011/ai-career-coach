import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { of } from 'rxjs';
import { Payment } from '../models/payment.model';

@Injectable({
  providedIn:'root'
})

export class AdminService {

  api="https://localhost:7222/api/Admin";

  constructor(
    private http:HttpClient
  ) {}

private mockPayments: Payment[] = [

{
    id:1,
    userName:'Ahmed Ali',
    email:'ahmed@test.com',
    plan:'Premium',
    amount:29,
    paymentMethod:'Visa',
    paymentDate:new Date(),
    status:'Paid',
    transactionId:'TRX-1001'
},

{
    id:2,
    userName:'Sara Mohamed',
    email:'sara@test.com',
    plan:'Pro',
    amount:49,
    paymentMethod:'MasterCard',
    paymentDate:new Date(),
    status:'Pending',
    transactionId:'TRX-1002'
},

{
    id:3,
    userName:'Omar Hassan',
    email:'omar@test.com',
    plan:'Basic',
    amount:19,
    paymentMethod:'PayPal',
    paymentDate:new Date(),
    status:'Failed',
    transactionId:'TRX-1003'
}

];

getPayments(){

    return of(this.mockPayments);

}

  getStatistics(){

    return this.http.get<any>(
      `${this.api}/statistics`
    );
  }

  getUsers(){

    return this.http.get<any[]>(
      `${this.api}/users`
    );
  }

  deleteUser(id:string){

    return this.http.delete(
      `${this.api}/users/${id}`
    );
  }

  changeRole(id:string,role:string){

    return this.http.put(
      `${this.api}/users/${id}/role`,
      JSON.stringify(role),
      {
        headers:{
          'Content-Type':'application/json'
        }
      }
    );
  }

getCVs() {

  return this.http.get<any[]>(
    `${this.api}/cvs`
  );

}

deleteCV(id:number){

  return this.http.delete(
    `${this.api}/cvs/${id}`
  );

}
downloadCV(id: number) {

  return this.http.get(
    `${this.api}/cvs/${id}/download`,
    {
      responseType: 'blob'
    }
  );

}

getUserManagement() {
  return this.http.get<any[]>(`${this.api}/user-management`);
}





}