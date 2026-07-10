import {
  Component,
  AfterViewInit,
  OnDestroy,
  signal,
  computed,
} from '@angular/core';
import { gsap } from 'gsap';
import { ScrollTrigger } from 'gsap/ScrollTrigger';

gsap.registerPlugin(ScrollTrigger);

// ── Types ──────────────────────────────────────────────────────────────
interface InterviewStep {
  readonly num: '01' | '02';
  readonly label: string;
  readonly title: string;
  readonly description: string;
}

interface JobCard {
  readonly company: string;
  readonly role: string;
  readonly location: string;
  readonly match: number;
  readonly skills: readonly string[];
}

interface RoadmapStep {
  readonly num: number;
  readonly title: string;
  readonly description: string;
  readonly priority: 'High' | 'Medium' | 'Low';
}

// ── Data ───────────────────────────────────────────────────────────────
const INTERVIEW_STEPS: readonly InterviewStep[] = [
  {
    num: '01',
    label: 'Ask anything',
    title: 'A coach that knows your CV',
    description:
      'Every question is generated from your actual experience, gaps, and target role. No generic scripts.',
  },
  {
    num: '02',
    label: 'Get your scorecard',
    title: 'Honest, detailed feedback',
    description:
      'A per-question breakdown with strengths, improvements, and an overall grade — then steps to close the gaps.',
  },
];

const JOB_CARDS: readonly JobCard[] = [
  {
    company: 'Stripe',
    role: 'Senior Backend Engineer',
    location: 'Remote · UK',
    match: 92,
    skills: ['System Design', 'API Design'],
  },
  {
    company: 'Vercel',
    role: 'Full Stack Developer',
    location: 'Remote · Global',
    match: 87,
    skills: ['React', 'Edge Functions'],
  },
  {
    company: 'Linear',
    role: 'Angular Frontend Engineer',
    location: 'Remote · US/EU',
    match: 83,
    skills: ['TypeScript', 'Signals'],
  },
];

const ROADMAP_STEPS: readonly RoadmapStep[] = [
  {
    num: 1,
    title: 'System Design Fundamentals',
    description: 'CAP theorem, horizontal scaling, event-driven architecture',
    priority: 'High',
  },
  {
    num: 2,
    title: 'Advanced TypeScript',
    description: 'Conditional types, template literals, type-safe APIs',
    priority: 'High',
  },
  {
    num: 3,
    title: 'Cloud & DevOps',
    description: 'Docker, CI/CD pipelines, Azure basics',
    priority: 'Medium',
  },
  {
    num: 4,
    title: 'Leadership & Mentoring',
    description: 'Code reviews, architectural decisions, team velocity',
    priority: 'Medium',
  },
];

@Component({
  selector: 'app-landing-features',
  standalone: true,
  imports: [],
  templateUrl: './landing-features.html',
  styleUrl: './landing-features.css',
})
export class LandingFeatures implements AfterViewInit, OnDestroy {
  readonly interviewStepsData = INTERVIEW_STEPS;
  readonly jobCards = JOB_CARDS;
  readonly roadmapSteps = ROADMAP_STEPS;

  /** Slider value — drives the top job card's match ring. */
  readonly sliderValue = signal(87);

  /** Top card match % derived from slider. */
  readonly topCardMatch = computed(() => this.sliderValue());

  /** SVG stroke-dashoffset for the match ring (circumference = 2πr, r=42). */
  readonly ringOffset = computed(() => {
    const circumference = 2 * Math.PI * 42;
    return circumference * (1 - this.sliderValue() / 100);
  });

  /** SVG circumference constant. */
  readonly circumference = 2 * Math.PI * 42;

  onSliderInput(event: Event): void {
    const val = parseInt((event.target as HTMLInputElement).value, 10);
    this.sliderValue.set(val);
  }

  private mm!: gsap.MatchMedia;

  ngAfterViewInit(): void {
    this.mm = gsap.matchMedia();

    // ── Reduced motion: all states visible, no animations ──
    this.mm.add('(prefers-reduced-motion: reduce)', () => {
      // No interview state transitions to bypass (states removed).
    });

    // ── Normal motion ──
    this.mm.add('(prefers-reduced-motion: no-preference)', () => {
      this.setupSectionReveals();
    });
  }

  ngOnDestroy(): void {
    if (this.mm) this.mm.revert();
    ScrollTrigger.getAll().forEach((t) => t.kill());
  }

  // ── Section reveals ──────────────────────────────────────────────────
  private setupSectionReveals(): void {
    // Interview row 1: text from left, card from right + chat messages stagger
    const row1Text = document.querySelector('.interview-row:nth-child(1) .interview-row-text');
    const row1Card = document.querySelector('.interview-row:nth-child(1) .interview-row-visual');
    if (row1Text) {
      gsap.from(row1Text, {
        x: -40, opacity: 0, duration: 0.8, ease: 'power3.out',
        scrollTrigger: { trigger: '.interview-row:nth-child(1)', start: 'top 88%', toggleActions: 'play none none reverse' },
      });
    }
    if (row1Card) {
      const chatTl = gsap.timeline({
        scrollTrigger: { trigger: '.interview-row:nth-child(1)', start: 'top 88%', toggleActions: 'play none none reverse' },
      });
      // Card container slides in
      chatTl.from(row1Card, { x: 40, opacity: 0, scale: 0.95, duration: 0.6, ease: 'power3.out' });
      // Chat elements appear one by one
      const chatHeader = row1Card.querySelector('.chat-header');
      const chatMsgs = row1Card.querySelectorAll('.chat-msg');
      const chatInput = row1Card.querySelector('.chat-input-mock');
      if (chatHeader) {
        chatTl.from(chatHeader, { opacity: 0, y: -8, duration: 0.3, ease: 'power2.out' }, '-=0.1');
      }
      chatMsgs.forEach((msg, i) => {
        chatTl.from(msg, { opacity: 0, y: 16, duration: 0.4, ease: 'power2.out' }, `-=${0.25 - i * 0.05}`);
      });
      if (chatInput) {
        chatTl.from(chatInput, { opacity: 0, y: 12, duration: 0.3, ease: 'power2.out' }, '-=0.15');
      }
    }

    // Interview row 2 (reversed): card from left, text from right + scorecard animation
    const row2Card = document.querySelector('.interview-row:nth-child(2) .interview-row-visual');
    const row2Text = document.querySelector('.interview-row:nth-child(2) .interview-row-text');
    if (row2Card) {
      const scoreTl = gsap.timeline({
        scrollTrigger: { trigger: '.interview-row:nth-child(2)', start: 'top 88%', toggleActions: 'play none none reverse' },
      });
      // Card container slides in
      scoreTl.from(row2Card, { x: -40, opacity: 0, scale: 0.95, duration: 0.6, ease: 'power3.out' });
      // Header
      const scoreHeader = row2Card.querySelector('.scorecard-header');
      if (scoreHeader) {
        scoreTl.from(scoreHeader, { opacity: 0, y: -8, duration: 0.3, ease: 'power2.out' }, '-=0.1');
      }
      // Ring animation: stroke-dashoffset from full to target
      const ring = row2Card.querySelector('.score-ring circle:last-of-type') as SVGCircleElement | null;
      if (ring) {
        const targetOffset = 26;
        scoreTl.fromTo(ring,
          { attr: { 'stroke-dashoffset': this.circumference } },
          { attr: { 'stroke-dashoffset': targetOffset }, duration: 1.2, ease: 'power2.out' },
          '-=0.2'
        );
      }
      // Score counter: 0 → 87
      const scoreNum = row2Card.querySelector('.score-number');
      if (scoreNum) {
        const counter = { val: 0 };
        scoreTl.to(counter, {
          val: 87, duration: 1.2, ease: 'power2.out',
          onUpdate: () => { scoreNum.textContent = String(Math.round(counter.val)); },
        }, '<');
      }
      // /100 text
      const scoreMax = row2Card.querySelector('.score-max');
      if (scoreMax) {
        scoreTl.from(scoreMax, { opacity: 0, duration: 0.3, ease: 'power2.out' }, '-=0.6');
      }
      // Grade badge
      const grade = row2Card.querySelector('.scorecard-grade');
      if (grade) {
        scoreTl.from(grade, { opacity: 0, y: 8, duration: 0.4, ease: 'power2.out' }, '-=0.3');
      }
      // Stats
      const stats = row2Card.querySelectorAll('.scorecard-stats .stat');
      stats.forEach((stat, i) => {
        scoreTl.from(stat, { opacity: 0, y: 10, duration: 0.3, ease: 'power2.out' }, `-=${0.2 - i * 0.05}`);
      });
    }
    if (row2Text) {
      gsap.from(row2Text, {
        x: 40, opacity: 0, duration: 0.8, ease: 'power3.out',
        scrollTrigger: { trigger: '.interview-row:nth-child(2)', start: 'top 88%', toggleActions: 'play none none reverse' },
      });
    }

    // Job section
    const jobText = document.querySelector('.features-job-text');
    const jobVisual = document.querySelector('.features-job-visual');
    if (jobText) {
      gsap.from(jobText, {
        x: 40,
        opacity: 0,
        duration: 0.8,
        ease: 'power3.out',
        scrollTrigger: {
          trigger: '.features-job',
          start: 'top 88%',
          toggleActions: 'play none none reverse',
        },
      });
    }
    if (jobVisual) {
      gsap.from(jobVisual, {
        x: -40,
        opacity: 0,
        scale: 0.95,
        duration: 0.8,
        ease: 'power3.out',
        scrollTrigger: {
          trigger: '.features-job',
          start: 'top 88%',
          toggleActions: 'play none none reverse',
        },
      });
    }

    // Roadmap section: text slides from left, visual from right
    const roadmapText = document.querySelector('.features-roadmap-text');
    const roadmapVisual = document.querySelector('.features-roadmap-visual');
    if (roadmapText) {
      gsap.from(roadmapText, {
        x: -40,
        opacity: 0,
        duration: 0.8,
        ease: 'power3.out',
        scrollTrigger: {
          trigger: '.features-roadmap',
          start: 'top 88%',
          toggleActions: 'play none none reverse',
        },
      });
    }
    if (roadmapVisual) {
      gsap.from(roadmapVisual, {
        x: 40,
        opacity: 0,
        scale: 0.95,
        duration: 0.8,
        ease: 'power3.out',
        scrollTrigger: {
          trigger: '.features-roadmap',
          start: 'top 88%',
          toggleActions: 'play none none reverse',
        },
      });
    }

    // Stagger internal elements of job + roadmap text
    gsap.from('.features-job-text .feature-bullet', {
      opacity: 0,
      y: 12,
      duration: 0.4,
      stagger: 0.08,
      ease: 'power2.out',
      delay: 0.3,
      scrollTrigger: {
        trigger: '.features-job',
        start: 'top 88%',
        toggleActions: 'play none none reverse',
      },
    });

    gsap.from('.features-roadmap-text .feature-bullet', {
      opacity: 0,
      y: 12,
      duration: 0.4,
      stagger: 0.08,
      ease: 'power2.out',
      delay: 0.3,
      scrollTrigger: {
        trigger: '.features-roadmap',
        start: 'top 88%',
        toggleActions: 'play none none reverse',
      },
    });
  }
}
