import { Injectable,inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import {Observable} from 'rxjs'

import{getallallpaymentmethods,Excutepaymentresponse} from'../models/payment.model'
import {Usersubscripions} from '../models/getallsubscriptionresponse.models'
import{UserPayment} from '../models/user-payment.model'
@Injectable({
  providedIn: 'root',
})
export class PaymentServices {
  private http = inject(HttpClient);

private apiUrl = 'https://localhost:7222/api/Fawaterak';

getallpaymentmethods(id:string):Observable<getallallpaymentmethods>{
return this.http.get<getallallpaymentmethods>(`${this.apiUrl}/getallpaymentmethods/${id}`);
}
excutepayment(methodid:string,usersubid:string):Observable<Excutepaymentresponse>{
  return this.http.post<Excutepaymentresponse>(`${this.apiUrl}/envoicepaymet?methodid=${methodid}&usersubscriptionid=${usersubid}`,{})
}


getallusersubscriptions():Observable<Usersubscripions[]>{
return this.http.get<Usersubscripions[]>(`https://localhost:7222/api/UserSubscription/Getusersubscriptionbyuser`);
}


getalluserpayments():Observable<UserPayment[]>{
  return this.http.get<UserPayment[]>(`https://localhost:7222/api/Payment/getalluserpayments`)
}

}