import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PercentPipe } from '@angular/common';
import { AdminRoadmapService } from '../../../core/services/admin-roadmap.service';
import { RoadmapTemplateDto, AdminCreateRoadmapStepDto } from '../../../core/models/admin.model';
import { ConfirmModal } from '../../../shared/components/confirm-modal/confirm-modal';

@Component({
  selector: 'app-roadmap-templates',
  imports: [FormsModule, PercentPipe, ConfirmModal],
  templateUrl: './roadmap-templates.html',
  styleUrl: './roadmap-templates.css',
})
export class RoadmapTemplates implements OnInit {
  private adminRoadmapService = inject(AdminRoadmapService);

  templates = signal<RoadmapTemplateDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  showForm = signal(false);
  editingTemplate = signal<RoadmapTemplateDto | null>(null);
  formData = signal({
    track: '',
    title: '',
    description: '',
    orderIndex: 0,
    steps: [] as AdminCreateRoadmapStepDto[],
  });
  saving = signal(false);

  pendingDeleteId = signal<number | null>(null);
  deleting = signal(false);

  testTemplateId = signal<number | null>(null);
  testCvText = signal('');
  testScore = signal<number | null>(null);
  testProcessing = signal(false);
  testTemplateName = signal('');

  totalCount = computed(() => this.templates().length);
  embeddedCount = computed(() => this.templates().filter(t => t.hasEmbedding).length);

  pendingDeleteTemplate = computed<RoadmapTemplateDto | null>(() => {
    const id = this.pendingDeleteId();
    if (id === null) return null;
    return this.templates().find(t => t.id === id) ?? null;
  });

  ngOnInit(): void {
    this.loadTemplates();
  }

  loadTemplates(): void {
    this.loading.set(true);
    this.error.set(null);
    this.adminRoadmapService.getAll().subscribe({
      next: (data) => {
        this.templates.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load roadmap templates.');
        this.loading.set(false);
      },
    });
  }

  openAdd(): void {
    this.editingTemplate.set(null);
    this.formData.set({ track: '', title: '', description: '', orderIndex: 0, steps: [] });
    this.showForm.set(true);
  }

  openEdit(t: RoadmapTemplateDto): void {
    this.editingTemplate.set(t);
    this.formData.set({
      track: t.track,
      title: t.title,
      description: t.description,
      orderIndex: t.orderIndex,
      steps: t.steps.map(s => ({
        title: s.title,
        description: s.description,
        level: s.level,
        resources: [...s.resources],
        orderIndex: s.orderIndex,
      })),
    });
    this.showForm.set(true);
  }

  closeForm(): void {
    this.showForm.set(false);
    this.editingTemplate.set(null);
  }

  updateFormField(field: string, value: string | number): void {
    this.formData.update(f => ({ ...f, [field]: value }));
  }

  addStep(): void {
    this.formData.update(f => ({
      ...f,
      steps: [...f.steps, { title: '', description: '', level: 'Beginner', resources: [], orderIndex: f.steps.length + 1 }],
    }));
  }

  removeStep(index: number): void {
    this.formData.update(f => {
      const steps = f.steps.filter((_, i) => i !== index).map((s, i) => ({ ...s, orderIndex: i + 1 }));
      return { ...f, steps };
    });
  }

  updateStepField(index: number, field: string, value: string | number): void {
    this.formData.update(f => {
      const steps = [...f.steps];
      steps[index] = { ...steps[index], [field]: value };
      return { ...f, steps };
    });
  }

  addResource(stepIndex: number): void {
    this.formData.update(f => {
      const steps = [...f.steps];
      steps[stepIndex] = { ...steps[stepIndex], resources: [...steps[stepIndex].resources, ''] };
      return { ...f, steps };
    });
  }

  updateResourceField(stepIndex: number, resIndex: number, value: string): void {
    this.formData.update(f => {
      const steps = [...f.steps];
      const resources = [...steps[stepIndex].resources];
      resources[resIndex] = value;
      steps[stepIndex] = { ...steps[stepIndex], resources };
      return { ...f, steps };
    });
  }

  removeResource(stepIndex: number, resIndex: number): void {
    this.formData.update(f => {
      const steps = [...f.steps];
      steps[stepIndex] = {
        ...steps[stepIndex],
        resources: steps[stepIndex].resources.filter((_, i) => i !== resIndex),
      };
      return { ...f, steps };
    });
  }

  save(): void {
    const data = this.formData();
    if (!data.track || !data.title || !data.description || data.steps.length === 0) {
      this.error.set('Please fill all required fields and add at least one step.');
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const editing = this.editingTemplate();

    if (editing) {
      this.adminRoadmapService.update(editing.id, data).subscribe({
        next: () => {
          this.saving.set(false);
          this.closeForm();
          this.loadTemplates();
        },
        error: (err) => {
          this.saving.set(false);
          this.error.set(err?.message ?? 'Update failed');
        },
      });
    } else {
      this.adminRoadmapService.create(data).subscribe({
        next: () => {
          this.saving.set(false);
          this.closeForm();
          this.loadTemplates();
        },
        error: (err) => {
          this.saving.set(false);
          this.error.set(err?.message ?? 'Create failed');
        },
      });
    }
  }

  deleteMessage(title: string): string {
    return `Are you sure you want to delete "${title}"? This will also remove all steps and embeddings.`;
  }

  requestDelete(id: number): void {
    this.pendingDeleteId.set(id);
  }

  cancelDelete(): void {
    this.pendingDeleteId.set(null);
  }

  confirmDelete(): void {
    const id = this.pendingDeleteId();
    if (id === null) return;

    this.deleting.set(true);
    const snapshot = this.templates();
    this.templates.update(list => list.filter(t => t.id !== id));

    this.adminRoadmapService.delete(id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.pendingDeleteId.set(null);
      },
      error: (err) => {
        this.deleting.set(false);
        this.pendingDeleteId.set(null);
        this.templates.set(snapshot);
        this.error.set(err?.message ?? 'Delete failed');
      },
    });
  }

  openTest(t: RoadmapTemplateDto): void {
    this.testTemplateId.set(t.id);
    this.testTemplateName.set(t.title);
    this.testCvText.set('');
    this.testScore.set(null);
  }

  closeTest(): void {
    this.testTemplateId.set(null);
    this.testCvText.set('');
    this.testScore.set(null);
    this.testTemplateName.set('');
  }

  runTest(): void {
    const id = this.testTemplateId();
    if (id === null) return;

    this.testProcessing.set(true);
    this.testScore.set(null);
    this.error.set(null);

    this.adminRoadmapService.testMatch(id, this.testCvText() || undefined).subscribe({
      next: (result) => {
        this.testScore.set(result.score);
        this.testProcessing.set(false);
      },
      error: (err) => {
        this.testProcessing.set(false);
        this.error.set(err?.message ?? 'Test match failed');
      },
    });
  }
}
