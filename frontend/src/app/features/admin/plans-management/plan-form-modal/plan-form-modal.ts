import { Component, input, output, signal, computed, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SubscriptionPlan } from '../../../../core/models/payment.model';

@Component({
  selector: 'app-plan-form-modal',
  imports: [FormsModule],
  templateUrl: './plan-form-modal.html',
  styleUrl: './plan-form-modal.css',
})
export class PlanFormModal implements OnInit {
  plan = input<SubscriptionPlan | null>(null);

  save = output<{ name: string; price: number; durationMonths: number }>();
  cancel = output<void>();

  name = signal('');
  price = signal(0);
  durationMonths = signal(1);

  isEdit = computed(() => this.plan() !== null);

  ngOnInit(): void {
    const p = this.plan();
    if (p) {
      this.name.set(p.name);
      this.price.set(p.price);
      this.durationMonths.set(p.durationMonths);
    }
  }

  onSave(): void {
    if (!this.name().trim() || this.price() <= 0 || this.durationMonths() < 1) return;
    this.save.emit({
      name: this.name().trim(),
      price: this.price(),
      durationMonths: this.durationMonths(),
    });
  }
}
