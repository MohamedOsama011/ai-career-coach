import { Component, input, output, signal, computed, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SubscriptionPlan, PlanLimits } from '../../../../core/models/payment.model';

@Component({
  selector: 'app-plan-form-modal',
  imports: [FormsModule],
  templateUrl: './plan-form-modal.html',
  styleUrl: './plan-form-modal.css',
})
export class PlanFormModal implements OnInit {
  plan = input<SubscriptionPlan | null>(null);

  save = output<{ name: string; price: number; durationMonths: number; limitsJson: string | null }>();
  cancel = output<void>();

  name = signal('');
  price = signal(0);
  durationMonths = signal(1);
  maxInterviews = signal(-1);
  maxRoadmaps = signal(-1);
  maxJobs = signal(-1);
  allowRescan = signal(true);

  isEdit = computed(() => this.plan() !== null);

  private parseLimits(json: string | null | undefined): PlanLimits {
    if (!json) return { interviewSessions: -1, roadmapGenerations: -1, jobRecommendations: -1, roadmapRescan: true };
    try {
      const parsed = JSON.parse(json);
      return {
        interviewSessions: parsed.interviewSessions ?? -1,
        roadmapGenerations: parsed.roadmapGenerations ?? -1,
        jobRecommendations: parsed.jobRecommendations ?? -1,
        roadmapRescan: parsed.roadmapRescan ?? true,
      };
    } catch {
      return { interviewSessions: -1, roadmapGenerations: -1, jobRecommendations: -1, roadmapRescan: true };
    }
  }

  ngOnInit(): void {
    const p = this.plan();
    if (p) {
      this.name.set(p.name);
      this.price.set(p.price);
      this.durationMonths.set(p.durationMonths);
      const limits = this.parseLimits(p.limitsJson);
      this.maxInterviews.set(limits.interviewSessions);
      this.maxRoadmaps.set(limits.roadmapGenerations);
      this.maxJobs.set(limits.jobRecommendations);
      this.allowRescan.set(limits.roadmapRescan);
    }
  }

  onSave(): void {
    if (!this.name().trim() || this.price() <= 0 || this.durationMonths() < 1) return;

    const limits: PlanLimits = {
      interviewSessions: this.maxInterviews(),
      roadmapGenerations: this.maxRoadmaps(),
      jobRecommendations: this.maxJobs(),
      roadmapRescan: this.allowRescan(),
    };

    this.save.emit({
      name: this.name().trim(),
      price: this.price(),
      durationMonths: this.durationMonths(),
      limitsJson: JSON.stringify(limits),
    });
  }
}
