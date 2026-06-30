import { Component, input, output } from '@angular/core';
import { Job } from '../../../../core/models/job.model';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-jobs-admin-table',
  imports: [DatePipe],
  templateUrl: './jobs-admin-table.html',
  styleUrl: './jobs-admin-table.css',
})
export class JobsAdminTable {
  jobs = input.required<Job[]>();
  loading = input<boolean>(false);

  edit = output<Job>();
  delete = output<number>();
}
