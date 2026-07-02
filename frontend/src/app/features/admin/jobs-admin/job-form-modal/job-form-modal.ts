import { Component, input, output, signal, computed, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Job, UpdateJobDto } from '../../../../core/models/job.model';

@Component({
  selector: 'app-job-form-modal',
  imports: [FormsModule],
  templateUrl: './job-form-modal.html',
  styleUrl: './job-form-modal.css',
})
export class JobFormModal implements OnInit {
  job = input<Job | null>(null);

  save = output<UpdateJobDto>();
  cancel = output<void>();

  title = signal<string>('');
  company = signal<string>('');
  description = signal<string>('');
  skillsText = signal<string>('');
  location = signal<string>('');
  salary = signal<number>(0);
  isRemote = signal<boolean>(false);
  externalUrl = signal<string>('');
  companyLogoUrl = signal<string>('');

  isEdit = computed(() => this.job() !== null);

  ngOnInit(): void {
    const j = this.job();
    if (j) {
      this.title.set(j.title);
      this.company.set(j.company);
      this.description.set(j.description ?? '');
      this.skillsText.set(j.requiredSkills.join(', '));
      this.location.set(j.location);
      this.salary.set(parseInt(j.salary?.replace(/[^0-9]/g, '') || '0', 10) || j.salary as any || 0);
      this.isRemote.set(j.isRemote ?? false);
      this.externalUrl.set(j.externalUrl ?? '');
    }
  }

  onSave(): void {
    const skills = this.skillsText()
      .split(',')
      .map(s => s.trim())
      .filter(s => s.length > 0);

    const dto: UpdateJobDto = {
      title: this.title(),
      company: this.company(),
      description: this.description(),
      requiredSkills: skills,
      location: this.location(),
      salary: this.salary(),
      isRemote: this.isRemote(),
      externalUrl: this.externalUrl() || undefined,
      companyLogoUrl: this.companyLogoUrl() || undefined
    };
    this.save.emit(dto);
  }
}
