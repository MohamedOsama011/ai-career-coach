import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { SubscriptionService } from '../../../core/services/subscription.service';
import { SubscriptionPlan } from '../../../core/models/payment.model';
import { PlanFormModal } from './plan-form-modal/plan-form-modal';
import { ConfirmModal } from '../../../shared/components/confirm-modal/confirm-modal';

@Component({
  selector: 'app-plans-management',
  imports: [PlanFormModal, ConfirmModal],
  templateUrl: './plans-management.html',
  styleUrl: './plans-management.css',
})
export class PlansManagement implements OnInit {
  private subscriptionService = inject(SubscriptionService);

  plans = signal<SubscriptionPlan[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  editingPlan = signal<SubscriptionPlan | null>(null);
  showForm = signal(false);

  pendingDeleteId = signal<number | null>(null);
  deleting = signal(false);

  totalPlans = computed(() => this.plans().length);

  pendingDeletePlan = computed<SubscriptionPlan | null>(() => {
    const id = this.pendingDeleteId();
    if (id === null) return null;
    return this.plans().find(p => p.id === id) ?? null;
  });

  ngOnInit(): void {
    this.loadPlans();
  }

  loadPlans(): void {
    this.loading.set(true);
    this.error.set(null);
    this.subscriptionService.getAll().subscribe({
      next: (res) => {
        if (res.success && Array.isArray(res.data)) {
          this.plans.set(res.data);
        } else {
          this.plans.set([]);
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load plans.');
        this.loading.set(false);
      },
    });
  }

  openAdd(): void {
    this.editingPlan.set(null);
    this.showForm.set(true);
  }

  openEdit(plan: SubscriptionPlan): void {
    this.editingPlan.set(plan);
    this.showForm.set(true);
  }

  closeForm(): void {
    this.showForm.set(false);
    this.editingPlan.set(null);
  }

  savePlan(data: { name: string; price: number; durationMonths: number }): void {
    const editing = this.editingPlan();
    if (editing) {
      this.subscriptionService.update(String(editing.id), data).subscribe({
        next: () => {
          this.closeForm();
          this.loadPlans();
        },
        error: (err) => this.error.set(err?.message ?? 'Update failed'),
      });
    } else {
      this.subscriptionService.create(data).subscribe({
        next: () => {
          this.closeForm();
          this.loadPlans();
        },
        error: (err) => this.error.set(err?.message ?? 'Create failed'),
      });
    }
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
    const snapshot = this.plans();
    this.plans.update(list => list.filter(p => p.id !== id));

    this.subscriptionService.delete(String(id)).subscribe({
      next: () => {
        this.deleting.set(false);
        this.pendingDeleteId.set(null);
      },
      error: (err) => {
        this.deleting.set(false);
        this.pendingDeleteId.set(null);
        this.plans.set(snapshot);
        this.error.set(err?.message ?? 'Delete failed');
      },
    });
  }
}
