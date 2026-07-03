import { Component, OnInit, ChangeDetectorRef } from '@angular/core';import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { AdminService } from '../../../core/services/admin.service';
import { Payment } from '../../../core/models/payment.model';
import Swal from 'sweetalert2';

import { Card } from '../../../shared/components/card/card';
import { StatCard } from '../../../shared/components/stat-card/stat-card';
import { Button } from '../../../shared/components/button/button';
import { Badge } from '../../../shared/components/badge/badge';
import { Chart } from 'chart.js';

@Component({
  selector: 'app-payment-management',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    Card,
    StatCard,
    Button,
    Badge
  ],
  templateUrl: './payment-management.html',
  styleUrl: './payment-management.css'
})
export class PaymentManagement implements OnInit {

  payments: Payment[] = [];

  searchText = '';

  totalRevenue = 0;
  successfulPayments = 0;
  pendingPayments = 0;
  failedPayments = 0;
  monthlyRevenueChart: any;
  planRevenueChart: any;


  constructor(private adminService: AdminService,  private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.loadPayments();
  }

loadPayments() {

  this.adminService.getPayments().subscribe(res => {

    this.payments = res;

    this.calculateStatistics();

    this.cdr.detectChanges();

    setTimeout(() => {
      this.drawCharts();
    });

  });

}

  calculateStatistics() {

    this.totalRevenue = this.payments.reduce(
      (sum, payment) => sum + payment.amount,
      0
    );

    this.successfulPayments = this.payments.filter(
      payment => payment.status === 'Paid'
    ).length;

    this.pendingPayments = this.payments.filter(
      payment => payment.status === 'Pending'
    ).length;

    this.failedPayments = this.payments.filter(
      payment => payment.status === 'Failed'
    ).length;

  }

  filteredPayments() {

    if (!this.searchText.trim()) {
      return this.payments;
    }

    const text = this.searchText.toLowerCase();

    return this.payments.filter(payment =>

      payment.userName.toLowerCase().includes(text) ||

      payment.email.toLowerCase().includes(text) ||

      payment.transactionId.toLowerCase().includes(text)

    );

  }

viewPayment(payment: any) {

  Swal.fire({

    title: 'Payment Details',

    html: `
      <div style="text-align:left">

        <p><strong>Customer:</strong> ${payment.userName}</p>

        <p><strong>Email:</strong> ${payment.email}</p>

        <p><strong>Plan:</strong> ${payment.plan}</p>

        <p><strong>Amount:</strong> $${payment.amount}</p>

        <p><strong>Method:</strong> ${payment.paymentMethod}</p>

        <p><strong>Status:</strong> ${payment.status}</p>

        <p><strong>Date:</strong> ${payment.paymentDate}</p>

      </div>
    `,

    confirmButtonText: 'Close',
    confirmButtonColor: '#2563EB'

  });

}

refundPayment(payment: any) {

  Swal.fire({

    title: 'Refund Payment?',

    text: `Refund $${payment.amount} to ${payment.userName}?`,

    icon: 'warning',

    showCancelButton: true,

    confirmButtonText: 'Refund',

    cancelButtonText: 'Cancel',

    confirmButtonColor: '#2563EB'

  }).then(result => {

    if (!result.isConfirmed)
      return;

    payment.status = 'Refunded';

    Swal.fire({

      icon: 'success',

      title: 'Refund Completed',

      text: 'Payment has been refunded.',

      timer: 1800,

      showConfirmButton: false

    });

  });

}

deletePayment(id: number) {

  Swal.fire({

    title: 'Delete Payment?',

    text: 'This payment record will be removed.',

    icon: 'warning',

    showCancelButton: true,

    confirmButtonText: 'Delete',

    cancelButtonText: 'Cancel',

    confirmButtonColor: '#DC2626'

  }).then(result => {

    if (!result.isConfirmed)
      return;

    this.payments =
      this.payments.filter(x => x.id !== id);

    Swal.fire({

      icon: 'success',

      title: 'Deleted',

      timer: 1500,

      showConfirmButton: false

    });

  });

}

drawCharts() {

if (this.monthlyRevenueChart)
  this.monthlyRevenueChart.destroy();

if (this.planRevenueChart)
  this.planRevenueChart.destroy();


 const monthlyRevenue = new Map<string, number>();

this.payments
  .filter(p => p.status === 'Paid')
  .forEach(p => {

const months = [
  "Jan", "Feb", "Mar", "Apr", "May", "Jun",
  "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
];

const monthlyRevenue = new Map<string, number>();

// Initialize all months with 0
months.forEach(month => monthlyRevenue.set(month, 0));

// Fill actual revenue
this.payments
  .filter(p => p.status === 'Paid')
  .forEach(p => {

    const month = new Date(p.paymentDate)
      .toLocaleString('en-US', { month: 'short' });

    monthlyRevenue.set(
      month,
      (monthlyRevenue.get(month) || 0) + p.amount
    );

  });

this.monthlyRevenueChart = new Chart("monthlyRevenueChart", {

  type: 'line',

 data: {

  labels: months,

  datasets: [{

    label: "Revenue",

    data: months.map(month => monthlyRevenue.get(month) || 0),

    borderColor: "#2563EB",

    backgroundColor: "rgba(37,99,235,.15)",

    fill: true,

    tension: .4,

    pointRadius: 5,

    pointBackgroundColor: "#2563EB"

  }]

},
});

const plans = [...new Set(this.payments.map(p => p.plan))];

const revenueByPlan = plans.map(plan =>

  this.payments

    .filter(p => p.plan === plan && p.status === 'Paid')

    .reduce((sum, p) => sum + p.amount, 0)

);

this.planRevenueChart = new Chart("planRevenueChart", {

  type: 'bar',

  data: {

    labels: plans,

    datasets: [{

      label: "Revenue",

      data: revenueByPlan,

      backgroundColor: [

        "#2563EB",

        "#3B82F6",

        "#60A5FA",

        "#93C5FD"

      ],

      borderRadius: 8

    }]

  },

  options: {

    responsive: true,

    plugins: {

      legend: {

        display: false

      }

    },

    scales: {

      x: {

        ticks: {

          color: "#6B7280"

        },

        grid: {

          color: "#E5E7EB"

        }

      },

      y: {

        beginAtZero: true,

        ticks: {

          color: "#6B7280"

        },

        grid: {

          color: "#E5E7EB"

        }

      }

    }

  }

});

  })}}


  



