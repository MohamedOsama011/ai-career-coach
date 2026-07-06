import { Component, input, output, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { CareerProfileStore } from '../../../core/store/career-profile-store';
import { GateFeature } from '../../../core/services/subscription-gate.service';

interface FeatureCopy {
  title: string;
  message: string;
  cta: string;
  icon: string;
}

const FEATURE_COPY: Record<GateFeature, FeatureCopy> = {
  interview: {
    title: 'You\'ve used your free mock interview',
    message: 'You\'ve completed your 1 free mock interview session. Upgrade to Pro for unlimited practice sessions with personalized feedback.',
    cta: 'Upgrade for unlimited interviews',
    icon: 'mic',
  },
  roadmap: {
    title: 'You\'ve used your free roadmap',
    message: 'You\'ve generated your 1 free career roadmap. Upgrade to Pro for unlimited regenerations and personalized gap analyses.',
    cta: 'Upgrade for unlimited roadmaps',
    icon: 'route',
  },
  rescan: {
    title: 'Rescanning is a Pro feature',
    message: 'Re-analyzing your skill gaps against the latest job market is available on Pro and above. Upgrade to keep your roadmap fresh.',
    cta: 'Upgrade to rescan gaps',
    icon: 'find_replace',
  },
  jobs: {
    title: 'Unlock all job matches',
    message: 'Free users see the top 3 personalized job matches. Upgrade to Pro to see all 5 matches and unlock detailed missing-skill insights.',
    cta: 'Upgrade to see all matches',
    icon: 'work',
  },
};

@Component({
  selector: 'app-upgrade-modal',
  imports: [CommonModule],
  templateUrl: './upgrade-modal.html',
  styleUrl: './upgrade-modal.css',
})
export class UpgradeModal {
  private store = inject(CareerProfileStore);
  private router = inject(Router);

  feature = input.required<GateFeature>();
  used = input<number | undefined>();
  limit = input<number | undefined>();

  close = output<void>();
  viewPlans = output<void>();

  readonly copy = computed<FeatureCopy>(() => FEATURE_COPY[this.feature()]);

  onClose(): void {
    this.store.dismissUpgradeModal();
    this.close.emit();
  }

  onViewPlans(): void {
    this.store.dismissUpgradeModal();
    this.viewPlans.emit();
    this.router.navigate(['/subscriptions']);
  }
}
