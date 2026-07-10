import {
  Component,
  ElementRef,
  ViewChild,
  AfterViewInit,
  OnDestroy,
  signal,
  computed,
  NgZone,
  inject,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { gsap } from 'gsap';

/** Hardcoded mock profile per user choice (no backend calls). */
export type ProfileId = 'dotnet' | 'angular' | 'fullstack' | 'data';

export interface ProfileMock {
  readonly id: ProfileId;
  readonly label: string;
  readonly icon: string;
  readonly name: string;
  readonly years: number;
  readonly target: string;
  readonly skills: readonly string[];
  readonly gaps: readonly string[];
  readonly jobTitle: string;
  readonly jobCompany: string;
  readonly jobMatch: number;
  readonly finalMatch: number;
  readonly finalGrade: string;
  readonly coachQuestion: string;
}

export type StepId = 1 | 2 | 3 | 4 | 5 | 6 | 7;

export interface StepSpec {
  readonly id: StepId;
  readonly title: string;
  readonly desc: string;
  readonly label: string;
}

const STEPS: readonly StepSpec[] = [
  { id: 1, title: 'Upload CV',       desc: 'Drop your resume',           label: 'Upload' },
  { id: 2, title: 'Parsing',         desc: 'Extracting text & skills',   label: 'Parse' },
  { id: 3, title: 'AI Analysis',     desc: 'Mapping to target role',     label: 'Analyze' },
  { id: 4, title: 'CV Score',        desc: 'See your ATS readiness',     label: 'Score' },
  { id: 5, title: 'Matched Jobs',    desc: 'Real opportunities',         label: 'Match' },
  { id: 6, title: 'Roadmap',         desc: 'Step-by-step learning path', label: 'Roadmap' },
  { id: 7, title: 'Interview Ready', desc: 'Practice with AI coach',     label: 'Ready' },
];

const PROFILES: Record<ProfileId, ProfileMock> = {
  dotnet: {
    id: 'dotnet',
    label: 'Backend .NET',
    icon: 'code',
    name: 'Sarah Chen',
    years: 4,
    target: 'Senior .NET Developer',
    skills: ['C#', 'ASP.NET Core', 'SQL Server', 'Entity Framework'],
    gaps: ['System Design', 'Kubernetes'],
    jobTitle: 'Senior .NET Engineer',
    jobCompany: 'Stripe',
    jobMatch: 78,
    finalMatch: 94,
    finalGrade: 'A-',
    coachQuestion: 'How would you design a distributed cache for a high-throughput payment API?',
  },
  angular: {
    id: 'angular',
    label: 'Frontend Angular',
    icon: 'web',
    name: 'Alex Carter',
    years: 3,
    target: 'Senior Angular Engineer',
    skills: ['TypeScript', 'Angular 21', 'RxJS', 'CSS Grid'],
    gaps: ['Signals', 'NgRx'],
    jobTitle: 'Senior Frontend Engineer',
    jobCompany: 'Vercel',
    jobMatch: 72,
    finalMatch: 94,
    finalGrade: 'A-',
    coachQuestion: 'Explain how Angular Signals differ from RxJS Observables in change detection.',
  },
  fullstack: {
    id: 'fullstack',
    label: 'Full Stack',
    icon: 'layers',
    name: 'Marcus Reed',
    years: 5,
    target: 'Full Stack Engineer',
    skills: ['C# .NET', 'Angular 21', 'Azure', 'SQL Server'],
    gaps: ['CI/CD', 'Docker'],
    jobTitle: 'Full Stack Engineer',
    jobCompany: 'Linear',
    jobMatch: 80,
    finalMatch: 95,
    finalGrade: 'A',
    coachQuestion: 'Walk me through how you would set up a CI/CD pipeline for an Angular + .NET monorepo.',
  },
  data: {
    id: 'data',
    label: 'Data Analyst',
    icon: 'analytics',
    name: 'Priya Sharma',
    years: 2,
    target: 'Senior Data Analyst',
    skills: ['Python', 'SQL Server', 'Pandas', 'Statistics'],
    gaps: ['Power BI', 'ML Basics'],
    jobTitle: 'Senior Data Analyst',
    jobCompany: 'Datadog',
    jobMatch: 74,
    finalMatch: 93,
    finalGrade: 'B+',
    coachQuestion: 'How would you detect and handle outliers in a skewed sales dataset?',
  },
};

@Component({
  selector: 'app-landing-demo',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './landing-demo.html',
  styleUrl: './landing-demo.css',
})
export class LandingDemo implements AfterViewInit, OnDestroy {
  @ViewChild('demoSection')     demoSection!:     ElementRef<HTMLElement>;
  @ViewChild('workspaceStage')  workspaceStage!:  ElementRef<HTMLElement>;
  @ViewChild('scoreNumber')     scoreNumber!:     ElementRef<HTMLSpanElement>;
  @ViewChild('finalMatchNumber') finalMatchNumber!: ElementRef<HTMLSpanElement>;
  @ViewChild('scoreCircleFill') scoreCircleFill!: ElementRef<SVGCircleElement>;
  @ViewChild('finalScoreCircleFill') finalScoreCircleFill!: ElementRef<SVGCircleElement>;
  @ViewChild('mobileStrip')     mobileStrip!:     ElementRef<HTMLElement>;

  private readonly zone = inject(NgZone);

  /** Source-of-truth data for the 4 preset profiles. */
  readonly profiles: readonly ProfileMock[] = Object.values(PROFILES);
  readonly steps: readonly StepSpec[] = STEPS;

  /** Live state — signals drive both the template and the GSAP timeline. */
  readonly activeStep      = signal<StepId>(1);
  readonly selectedProfile = signal<ProfileId | null>(null);
  readonly isAnimating     = signal<boolean>(false);
  readonly showFinalCta    = signal<boolean>(false);
  readonly isInView        = signal<boolean>(false);
  readonly isIdlePlaying   = signal<boolean>(false);

  /** Resolved profile derived from selectedProfile; null before user picks. */
  readonly currentProfile = computed<ProfileMock | null>(() => {
    const id = this.selectedProfile();
    return id ? PROFILES[id] : null;
  });

  /** Convenience for the template — circle stroke-dasharray uses 2πR. */
  readonly scoreRadius = 60;
  readonly scoreCircumference = 2 * Math.PI * this.scoreRadius;

  private mm!: gsap.MatchMedia;
  private timeline: gsap.core.Timeline | null = null;
  private idleIntervalId?: ReturnType<typeof setInterval>;
  private intersectionObserver?: IntersectionObserver;
  private mobileRevealObserver?: IntersectionObserver;

  ngAfterViewInit(): void {
    this.mm = gsap.matchMedia();

    // Initial state — every step pane is hidden, step 1 is shown.
    gsap.set(this.workspaceStage.nativeElement.querySelectorAll('.step-pane'), {
      opacity: 0,
      pointerEvents: 'none',
    });
    gsap.set(this.workspaceStage.nativeElement.querySelector('.step-1'), {
      opacity: 1,
      pointerEvents: 'auto',
    });

    // Set up the score circles to their "empty" state.
    this.initScoreCircle(this.scoreCircleFill.nativeElement, 0);
    this.initScoreCircle(this.finalScoreCircleFill.nativeElement, 0);

    // Pause idle cycle when the demo is mostly off-screen.
    this.zone.runOutsideAngular(() => {
      this.intersectionObserver = new IntersectionObserver(
        (entries) => {
          for (const entry of entries) {
            this.isInView.set(entry.isIntersecting);
            if (!entry.isIntersecting) {
              this.stopIdleCycle();
            } else if (this.selectedProfile() === null && !this.showFinalCta()) {
              this.startIdleCycle();
            }
          }
        },
        { threshold: 0.3 },
      );
      this.intersectionObserver.observe(this.demoSection.nativeElement);
    });

    // Mobile strip — IntersectionObserver reveals each card on scroll.
    if (this.mobileStrip) {
      this.zone.runOutsideAngular(() => {
        this.mobileRevealObserver = new IntersectionObserver(
          (entries) => {
            for (const entry of entries) {
              if (entry.isIntersecting) {
                entry.target.classList.add('mobile-step--visible');
                this.mobileRevealObserver?.unobserve(entry.target);
              }
            }
          },
          { threshold: 0.25 },
        );
        this.mobileStrip.nativeElement
          .querySelectorAll('.mobile-step')
          .forEach((el) => this.mobileRevealObserver?.observe(el));
      });
    }

    // Reduced-motion guard: if user prefers reduced motion, just show the
    // final state of every step (step 7 + final CTA) without animation.
    this.mm.add('(prefers-reduced-motion: reduce)', () => {
      this.showFinalStateForReducedMotion();
    });

    // Normal-motion path: idle cycle starts after a 1.5s grace period.
    this.mm.add('(prefers-reduced-motion: no-preference)', () => {
      setTimeout(() => {
        if (this.selectedProfile() === null && this.isInView()) {
          this.startIdleCycle();
        }
      }, 1500);
    });
  }

  ngOnDestroy(): void {
    this.stopIdleCycle();
    this.intersectionObserver?.disconnect();
    this.mobileRevealObserver?.disconnect();
    if (this.timeline) this.timeline.kill();
    if (this.mm) this.mm.revert();
  }

  // ── User interactions ───────────────────────────────────────────────
  selectProfile(id: ProfileId): void {
    if (this.isAnimating() && this.selectedProfile() === id) return;
    this.zone.run(() => {
      this.stopIdleCycle();
      this.selectedProfile.set(id);
      this.showFinalCta.set(false);
      this.runDemoTimeline(id);
    });
  }

  replay(): void {
    const id = this.selectedProfile();
    if (!id) {
      this.startIdleCycle();
      return;
    }
    this.zone.run(() => {
      this.showFinalCta.set(false);
      this.runDemoTimeline(id);
    });
  }

  // ── Idle cycle (12s loop using the Angular profile) ────────────────
  private startIdleCycle(): void {
    if (this.idleIntervalId || this.showFinalCta()) return;
    if (!this.isInView()) return;
    this.isIdlePlaying.set(true);
    this.zone.run(() => {
      this.selectedProfile.set('angular');
      this.runDemoTimeline('angular', { autoFinal: true });
    });
    this.idleIntervalId = setInterval(() => {
      if (!this.isInView() || this.showFinalCta()) {
        this.stopIdleCycle();
        return;
      }
      this.zone.run(() => {
        this.selectedProfile.set('angular');
        this.runDemoTimeline('angular', { autoFinal: true });
      });
    }, 18000);
  }

  private stopIdleCycle(): void {
    if (this.idleIntervalId) {
      clearInterval(this.idleIntervalId);
      this.idleIntervalId = undefined;
    }
    this.isIdlePlaying.set(false);
  }

  // ── The full 7-step timeline ───────────────────────────────────────
  private runDemoTimeline(profileId: ProfileId, opts: { autoFinal?: boolean } = {}): void {
    if (this.timeline) this.timeline.kill();
    this.isAnimating.set(true);
    this.showFinalCta.set(false);
    this.activeStep.set(1);

    const profile = PROFILES[profileId];
    const tl = gsap.timeline({
      onComplete: () => {
        this.isAnimating.set(false);
        if (opts.autoFinal) {
          this.showFinalCta.set(true);
        }
      },
    });

    // Per-step transition: hide all panes, show the new one, update the
    // activeStep signal. This is the single source of truth for which pane
    // is visible at any point in the timeline. Without this helper, every
    // step's contents would stack on top of each other inside the
    // absolutely-positioned .demo-stage (all .step-pane share the same
    // inset:0; box and rely on opacity for visibility).
    const showStep = (n: StepId, time: number): void => {
      tl.set('.step-pane', { opacity: 0, pointerEvents: 'none' }, time);
      tl.set(`.step-${n}`, { opacity: 1, pointerEvents: 'auto' }, time);
      tl.call(() => this.activeStep.set(n), [], time);
    };

    // Reset all step panes to invisible (showStep(1, 0) will reveal step 1).
    tl.set('.pdf-icon', { y: -100, opacity: 0 }, 0);
    tl.set('.laser-beam', { top: '0%' }, 0);
    tl.set('.cv-card', { scale: 1 }, 0);
    tl.set('.meta-chip', { y: 0, opacity: 0 }, 0);
    tl.set('.ai-core', { scale: 0, opacity: 0 }, 0);
    tl.set('.ai-module', { scale: 0, opacity: 0 }, 0);
    tl.set('.ai-link', { strokeDashoffset: 200 }, 0);
    tl.set('.job-card', { x: 100, opacity: 0 }, 0);
    tl.set('.roadmap-line-fill', { strokeDashoffset: 300 }, 0);
    tl.set('.roadmap-node', { scale: 0 }, 0);
    tl.set('.waveform-bar', { scaleY: 0.2 }, 0);
    tl.set('.coach-typed', { width: '0' }, 0);
    tl.set('.score-number-text', { textContent: 0 }, 0);
    this.initScoreCircle(this.scoreCircleFill.nativeElement, 0);
    this.initScoreCircle(this.finalScoreCircleFill.nativeElement, 0);

    // STEP 1 — Upload
    showStep(1, 0);
    tl.to('.pdf-icon', {
      y: 0,
      opacity: 1,
      duration: 0.9,
      ease: 'elastic.out(1, 0.75)',
    }, 0.3);

    // STEP 2 — Parse
    showStep(2, 2.0);
    tl.to('.laser-beam', { top: '100%', duration: 1.6, ease: 'none' }, 2.0)
      .to('.meta-chip', {
        y: -60,
        opacity: 1,
        duration: 0.9,
        stagger: 0.15,
        ease: 'power2.out',
      }, 2.2)
      .to('.meta-chip', {
        y: -100,
        opacity: 0,
        duration: 0.7,
        stagger: 0.06,
      }, 2.6);

    // STEP 3 — AI Thinking
    showStep(3, 4.4);
    tl.to('.step-2 .cv-card', { scale: 0, duration: 0.5, ease: 'power2.in' }, 4.5)
      .to('.ai-core', { scale: 1, opacity: 1, duration: 0.5, ease: 'back.out(2)' }, 4.7)
      .to('.ai-link', { strokeDashoffset: 0, duration: 0.9, ease: 'power2.out', stagger: 0.12 }, 4.9)
      .to('.ai-module', { scale: 1, opacity: 1, duration: 0.4, ease: 'back.out(1.7)', stagger: 0.12 }, 5.1);

    // STEP 4 — CV Score
    showStep(4, 6.5);
    tl.to(this.scoreCircleFill.nativeElement, {
      strokeDashoffset: this.scoreCircumference * (1 - profile.jobMatch / 100),
      duration: 1.4,
      ease: 'power2.out',
    }, 6.6)
      .to(this.scoreNumber.nativeElement, {
        duration: 1.4,
        ease: 'power2.out',
        onUpdate: () => {
          const v = Math.round(
            (1 - parseFloat(this.scoreCircleFill.nativeElement.style.strokeDashoffset || `${this.scoreCircumference}`)
              / this.scoreCircumference) * 100,
          );
          this.scoreNumber.nativeElement.textContent = String(v);
        },
      }, 6.6)
      .to('.score-warnings li', {
        opacity: 1,
        x: 0,
        duration: 0.5,
        stagger: 0.18,
        ease: 'power2.out',
      }, 7.0);

    // STEP 5 — Match Jobs
    showStep(5, 8.8);
    tl.to('.job-card', {
      x: 0,
      opacity: 1,
      duration: 0.7,
      stagger: 0.2,
      ease: 'power3.out',
    }, 8.9);

    // STEP 6 — Roadmap
    showStep(6, 10.8);
    tl.to('.roadmap-line-fill', {
      strokeDashoffset: 0,
      duration: 0.9,
      ease: 'power2.inOut',
    }, 10.9)
      .to('.roadmap-node', {
        scale: 1,
        duration: 0.5,
        stagger: 0.22,
        ease: 'elastic.out(1.2, 0.5)',
      }, 11.1);

    // STEP 7 — Interview Ready
    showStep(7, 13.0);
    tl.to('.waveform-bar', {
      scaleY: 'random(0.3, 1)',
      duration: 0.1,
      repeat: 12,
      yoyo: true,
      ease: 'none',
      stagger: { each: 0.04, from: 'random' },
    }, 13.1)
      .to('.coach-typed', {
        width: '100%',
        duration: 2.4,
        ease: 'none',
      }, 13.1)
      .to(this.scoreNumber.nativeElement, {
        duration: 1.2,
        ease: 'power2.out',
        onUpdate: () => {
          const v = Math.round(
            (1 - parseFloat(this.scoreCircleFill.nativeElement.style.strokeDashoffset || `${this.scoreCircumference}`)
              / this.scoreCircumference) * 100,
          );
          this.scoreNumber.nativeElement.textContent = String(v);
        },
      }, 14.7)
      .to(this.scoreCircleFill.nativeElement, {
        strokeDashoffset: this.scoreCircumference * (1 - profile.finalMatch / 100),
        duration: 1.2,
        ease: 'power2.out',
      }, 14.7)
      .to(this.finalScoreCircleFill.nativeElement, {
        strokeDashoffset: this.scoreCircumference * (1 - profile.finalMatch / 100),
        duration: 1.2,
        ease: 'power2.out',
      }, 14.7)
      .to(this.finalMatchNumber.nativeElement, {
        duration: 1.2,
        ease: 'power2.out',
        onUpdate: () => {
          const v = Math.round(
            (1 - parseFloat(this.finalScoreCircleFill.nativeElement.style.strokeDashoffset || `${this.scoreCircumference}`)
              / this.scoreCircumference) * 100,
          );
          this.finalMatchNumber.nativeElement.textContent = String(v);
        },
      }, 14.7)
      .to('.grade-chip', {
        scale: 1,
        opacity: 1,
        duration: 0.5,
        ease: 'back.out(1.7)',
      }, 15.2);

    this.timeline = tl;
  }

  // ── Reduced motion — show the final state immediately ─────────────
  private showFinalStateForReducedMotion(): void {
    this.selectedProfile.set('angular');
    const profile = PROFILES['angular'];
    this.activeStep.set(7);
    this.showFinalCta.set(true);
    gsap.set('.step-pane', { opacity: 0, pointerEvents: 'none' });
    gsap.set('.step-7', { opacity: 1, pointerEvents: 'auto' });
    this.initScoreCircle(this.scoreCircleFill.nativeElement, profile.finalMatch);
    this.initScoreCircle(this.finalScoreCircleFill.nativeElement, profile.finalMatch);
    if (this.scoreNumber) this.scoreNumber.nativeElement.textContent = String(profile.finalMatch);
    if (this.finalMatchNumber) this.finalMatchNumber.nativeElement.textContent = String(profile.finalMatch);
    gsap.set('.grade-chip', { scale: 1, opacity: 1 });
    gsap.set('.job-card', { x: 0, opacity: 1 });
    gsap.set('.job-card--primary', {
      borderColor: 'rgba(99, 102, 241, 0.5)',
      boxShadow: '0 8px 32px rgba(99, 102, 241, 0.25)',
    });
    gsap.set('.roadmap-node', { scale: 1 });
    gsap.set('.roadmap-line-fill', { strokeDashoffset: 0 });
  }

  private initScoreCircle(el: SVGCircleElement, percent: number): void {
    const dash = this.scoreCircumference * (1 - percent / 100);
    el.style.strokeDasharray = String(this.scoreCircumference);
    el.style.strokeDashoffset = String(dash);
  }

  // ── Template helpers ───────────────────────────────────────────────
  trackProfile(_: number, p: ProfileMock): string { return p.id; }
  trackStep(_: number, s: StepSpec): number       { return s.id; }
  trackByIndex(i: number): number                 { return i; }
}
