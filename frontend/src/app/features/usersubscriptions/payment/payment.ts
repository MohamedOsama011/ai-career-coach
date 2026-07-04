import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';

import { PaymentServices } from '../../../core/services/payment.services';

import { Details ,getallallpaymentmethods} from '../../../core/models/payment.model';

@Component({
  selector: 'app-payment',
  imports: [CommonModule],
  templateUrl: './payment.html',
  styleUrl: './payment.css',
})
export class Payment implements OnInit{

  subscriptionId!:string;
  usersubid!:string;

  paymentMethods:Details[]=[];

  constructor(

    private paymentService:PaymentServices,

    private route:ActivatedRoute

  ){}

  ngOnInit(): void {

    const id=this.route.snapshot.paramMap.get('id');

    if(id){

      this.subscriptionId=id;

      this.loadPaymentMethods();

    }

  }

  loadPaymentMethods(){

    this.paymentService
    .getallpaymentmethods(this.subscriptionId)
    .subscribe({
      next:(res:getallallpaymentmethods)=>{

        this.paymentMethods=res.data.data;
      this.usersubid=res.usersubscriptionid;
      },

      error:(err:any)=>{

        console.log(err);

      }

    });

  }

pay(methodId: number): void {

  this.paymentService
    .excutepayment(
      methodId.toString(),
      this.usersubid
    )
    .subscribe({

      next: (res:any) => {

        window.location.href = res.data.payment_Data.redirectTo;

      },

      error: (err:any) => {

        console.log(err);

      }

    });

}

}
