import { Component, ChangeDetectorRef, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../../core/services/admin.service';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { Chart } from 'chart.js/auto';
import Swal from 'sweetalert2';

import { Card } from '../../../shared/components/card/card';
import { StatCard } from '../../../shared/components/stat-card/stat-card';
import { Button } from '../../../shared/components/button/button';
import { Badge } from '../../../shared/components/badge/badge';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule , FormsModule
    ,     Card,
    StatCard,
    Button,
    Badge
  ],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.css'
})
export class AdminDashboard implements OnInit {

  statistics: any = {};
  users: any[] = [];
  userManagement: any[] = [];
  cvs: any[] = [];

lastSync = new Date();

rolesChart: any;

statsChart: any;

currentPage = 1;

pageSize = 5;

toastMessage = '';
toastClass = 'bg-success';
showToast = false;

constructor(
    private adminService: AdminService,
    private cdr: ChangeDetectorRef
) {}

  ngOnInit(): void {
  this.loadDashboard();
}

loadDashboard() {

  forkJoin({

    stats: this.adminService.getStatistics(),
    users: this.adminService.getUsers(),
    userManagement: this.adminService.getUserManagement(),
    cvs: this.adminService.getCVs()

  }).subscribe({

    next: ({ stats, users, userManagement, cvs }) => {

      this.statistics = stats;
      this.users = users;
      this.userManagement = userManagement;
      this.cvs = cvs;

      this.cdr.detectChanges();

      setTimeout(() => {

        this.drawCharts();

      });

    }

  });

}
deleteCV(id: number) {

  Swal.fire({
    title: 'Delete CV?',
    text: 'This CV will be permanently deleted.',
    icon: 'warning',
    showCancelButton: true,
    confirmButtonText: 'Delete',
    cancelButtonText: 'Cancel',
    confirmButtonColor: '#dc3545'
  }).then((result) => {

    if (!result.isConfirmed)
      return;

    this.adminService.deleteCV(id).subscribe({

      next: () => {

        this.loadDashboard();

        Swal.fire({
          icon: 'success',
          title: 'Deleted!',
          timer: 1500,
          showConfirmButton: false
        });

      }

    });

  });

}
pagedUsers() {

  const start =
    (this.currentPage - 1) * this.pageSize;

  return this.filteredUsers()
    .slice(start, start + this.pageSize);
}

  loadStatistics() {

    this.adminService.getStatistics()
      .subscribe(res => {

        this.statistics = res;
      });
  }

  loadUsers() {

    this.adminService.getUsers()
      .subscribe(res => {

        this.users = res;
      });
  }

deleteUser(id: string) {

  Swal.fire({
    title: 'Delete User?',
    text: 'You will not be able to recover this user!',
    icon: 'warning',
    showCancelButton: true,
    confirmButtonColor: '#dc3545',
    cancelButtonColor: '#6c757d',
    confirmButtonText: 'Yes, Delete',
    cancelButtonText: 'Cancel'
  }).then((result) => {

    if (!result.isConfirmed)
      return;

    this.adminService.deleteUser(id).subscribe({

      next: () => {

        this.loadDashboard();

        Swal.fire({
          icon: 'success',
          title: 'Deleted!',
          text: 'User deleted successfully.',
          timer: 1500,
          showConfirmButton: false
        });

      }

    });

  });

}

changeRole(id: string, role: string) {

  this.adminService.changeRole(id, role).subscribe({

    next: () => {

this.showNotification("Role updated successfully");

this.loadDashboard();
    }

  });

}

searchText = '';

filteredUsers() {

  if (!this.searchText.trim())
    return this.users;

return this.users.filter(u =>

    u.fullName
      .toLowerCase()
      .includes(this.searchText.toLowerCase())

    ||

    u.email
      .toLowerCase()
      .includes(this.searchText.toLowerCase())

);
}
downloadCV(id: number) {

  this.adminService.downloadCV(id).subscribe(blob => {

    const url = window.URL.createObjectURL(blob);

    const a = document.createElement('a');

    a.href = url;

    a.download = 'CV.pdf';

    a.click();

    window.URL.revokeObjectURL(url);

  });

}

showNotification(message: string, success = true) {

  this.toastMessage = message;

  this.toastClass = success
    ? 'bg-success'
    : 'bg-danger';

  this.showToast = true;

  setTimeout(() => {

    this.showToast = false;

  }, 2500);

}

syncJobs() {

  Swal.fire({

    title: 'Sync Jobs?',

    text: 'Fetch latest jobs from external providers?',

    icon: 'question',

    showCancelButton: true,

    confirmButtonText: 'Sync Now',

    confirmButtonColor: '#2563EB'

  }).then(result => {

    if (!result.isConfirmed)
      return;

    // هيتغير بعد ما الـ API تخلص
    setTimeout(() => {

      this.lastSync = new Date();

      Swal.fire({

        icon: 'success',

        title: 'Jobs synchronized successfully',

        text: '148 new jobs imported.',

        timer: 1800,

        showConfirmButton: false

      });

    }, 1200);

  });

}

drawCharts() {

  if (this.rolesChart)
    this.rolesChart.destroy();

  if (this.statsChart)
    this.statsChart.destroy();

  this.rolesChart = new Chart("rolesChart", {
  type: 'pie',

  data: {
    labels: ["Users", "Admins"],

    datasets: [{
      data: [
        this.statistics.users - this.statistics.admins,
        this.statistics.admins
      ],
backgroundColor: [
  "#2563EB",
  "#93C5FD"
],
borderColor: "#FFFFFF",
borderWidth: 2
    }]
  },

  options: {
    responsive: true,
    plugins: {
      legend: {
        position: 'bottom'
      }
    }
  }

});

  this.statsChart = new Chart("statsChart", {

  type: 'bar',

  data: {

    labels: [

      "Users",
      "Admins",
      "CVs",
      "interviews"

    ],

    datasets: [{

      label: "System Statistics",

      data: [

        this.statistics.users,
        this.statistics.admins,
        this.statistics.cVs || this.statistics.cvs,
        this.statistics.interviews 

      ],

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
    labels: {
      color: "#374151"
    }
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
      color: "#6B7280",
      precision: 0
    },
    grid: {
      color: "#E5E7EB"
    }
  }


    }

  }

});
}
}