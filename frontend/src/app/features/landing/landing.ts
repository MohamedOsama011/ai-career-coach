import { Component, ElementRef, ViewChild, AfterViewInit, OnDestroy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { gsap } from 'gsap';
import { LandingDemo } from './landing-demo/landing-demo';
import { LandingBento } from './landing-bento/landing-bento';
import { LandingWorkflow } from './landing-workflow/landing-workflow';
import { LandingFeatures } from './landing-features/landing-features';
import { LandingPricing } from './landing-pricing/landing-pricing';
import { LandingFinalCta } from './landing-final-cta/landing-final-cta';
import { LandingFooter } from './landing-footer/landing-footer';

/** Single metric tile in the mock dashboard (top-row KPIs). */
interface HeroMetric {
  readonly title: string;
  readonly value: string;
  readonly description: string;
  readonly trend: 'up' | 'down' | 'neutral';
}

/** Single skill row in the mock "Skill Gap Analysis" widget. */
interface HeroSkill {
  readonly name: string;
  readonly percent: number;
  /** True when the row represents a critical gap (amber treatment). */
  readonly highlight?: boolean;
}

/** A single star particle in the hero background. */
interface Star {
  x: number;
  y: number;
  radius: number;
  opacity: number;
  baseOpacity: number;
  speedX: number;
  speedY: number;
  twinkleSpeed: number;
  twinklePhase: number;
}

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, LandingDemo, LandingBento, LandingWorkflow, LandingFeatures, LandingPricing, LandingFinalCta, LandingFooter],
  templateUrl: './landing.html',
  styleUrl: './landing.css',
})
export class Landing implements AfterViewInit, OnDestroy {
  @ViewChild('heroEyebrow')       heroEyebrow!:       ElementRef<HTMLElement>;
  @ViewChild('heroTitle')         heroTitle!:         ElementRef<HTMLHeadingElement>;
  @ViewChild('heroSubtitle')      heroSubtitle!:      ElementRef<HTMLParagraphElement>;
  @ViewChild('heroCta')           heroCta!:           ElementRef<HTMLDivElement>;
  @ViewChild('heroVisualContainer') heroVisualContainer!: ElementRef<HTMLDivElement>;
  @ViewChild('heroVisualWrapper') heroVisualWrapper!: ElementRef<HTMLDivElement>;
  @ViewChild('dashboardGlowBack') dashboardGlowBack!: ElementRef<HTMLDivElement>;
  @ViewChild('particleCanvas')    particleCanvas!:    ElementRef<HTMLCanvasElement>;

  /** Heights used for the waveform bar @for loop in the template. */
  readonly waveformHeights = [0.5, 0.9, 0.3, 0.7, 1.0, 0.4, 0.8, 0.2, 0.6, 0.35, 0.85, 0.45];

  /** Mocked dashboard tiles — replaced the previous generic SaaS numbers
   * ($45,231 / +573 / +12,234) with realistic AI Career Coach KPIs. */
  readonly heroMetrics: readonly HeroMetric[] = [
    { title: 'CV Score', value: '87/100', description: '+12 this week', trend: 'up' },
    { title: 'Skills Gap', value: '3 High', description: '2 closed since last scan', trend: 'down' },
    { title: 'Interview Grade', value: 'A-', description: 'Last session: 9.4 / 10', trend: 'up' },
    { title: 'Jobs Matched', value: '24', description: '6 above 80% match', trend: 'up' },
  ];

  /** Mocked skill-gap rows. Percentages are the locked
   * `None=0, Beginner=25, Intermediate=50, Advanced=75, Expert=100` mapping. */
  readonly heroSkills: readonly HeroSkill[] = [
    { name: 'TypeScript Core', percent: 100 },
    { name: 'Angular Architecture', percent: 75 },
    { name: 'Angular Signals', percent: 25, highlight: true },
    { name: 'System Design', percent: 50 },
  ];

  private mm!: gsap.MatchMedia;

  // ── Star particles ────────────────────────────────────────────────
  private stars: Star[] = [];
  private animFrameId = 0;
  private mouseX = -1000;
  private mouseY = -1000;
  private mouseDown = false;
  private readonly STAR_COUNT = 40;
  private readonly MOUSE_REPEL_RADIUS = 100;
  private readonly MOUSE_REPEL_FORCE = 0.3;
  private particleResize!: () => void;
  private particleMousemove!: (e: MouseEvent) => void;
  private particleMouseleave!: () => void;
  private particleSection!: HTMLElement;
  private particleCtx!: CanvasRenderingContext2D | null;

  ngAfterViewInit(): void {
    this.initParticles();

    // ── 1. Hero entrance + ambient loops ──────────────────────────────
    this.mm = gsap.matchMedia();

    this.mm.add('(prefers-reduced-motion: no-preference)', () => {
      const tl = gsap.timeline({ defaults: { ease: 'power4.out' } });

      tl.from(this.heroEyebrow.nativeElement, {
          opacity: 0, y: 20, duration: 0.7,
        })
        .from(
          this.heroTitle.nativeElement.querySelectorAll('span'),
          { opacity: 0, y: 30, duration: 1.0, stagger: 0.12 },
          '-=0.5',
        )
        .from(this.heroSubtitle.nativeElement, {
            opacity: 0, y: 20, duration: 0.8,
          }, '-=0.55')
        .from(this.heroCta.nativeElement, {
            opacity: 0, y: 20, duration: 0.8,
          }, '-=0.55')
        .from(
          this.heroVisualWrapper.nativeElement,
          {
            opacity: 0,
            y: 60,
            rotationX: 15,
            rotationY: -10,
            scale: 0.95,
            duration: 1.2,
            ease: 'power3.out',
            onComplete: () => {
              this.startFloatingLoop();
              this.attachSpotlightListener();
            },
          },
          '-=0.7',
        )
        .to(
          this.dashboardGlowBack.nativeElement,
          {
            opacity: 1,
            duration: 1.8,
            ease: 'power2.out',
          },
          '-=0.2',
        );

      gsap.from('.skill-bar-inner', {
        width: 0,
        duration: 1.6,
        delay: 1.8,
        stagger: 0.18,
        ease: 'power2.out',
      });

      gsap.to('.waveform-bar', {
        scaleY: 'random(0.15, 1.0)',
        duration: 0.15,
        repeat: -1,
        yoyo: true,
        stagger: { each: 0.04, from: 'random' },
        ease: 'none',
      });

      return () => {
        gsap.killTweensOf(this.heroVisualWrapper.nativeElement);
        gsap.killTweensOf(this.dashboardGlowBack.nativeElement);
        this.dashboardGlowBack.nativeElement.style.opacity = '0';
        const el = this.heroVisualContainer.nativeElement;
        el.removeEventListener('mousemove', this.spotlightHandler);
      };
    });
  }

  // ── Star particles ────────────────────────────────────────────────
  private initParticles(): void {
    const canvas = this.particleCanvas.nativeElement;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const resize = () => {
      const section = canvas.parentElement!;
      canvas.width = section.clientWidth;
      canvas.height = section.clientHeight;
    };
    resize();
    window.addEventListener('resize', resize);

    // Create stars
    this.stars = Array.from({ length: this.STAR_COUNT }, () => {
      const radius = Math.random() * 2 + 0.5;
      const depth = radius / 2.5;
      return {
        x: Math.random() * canvas.width,
        y: Math.random() * canvas.height,
        radius,
        baseOpacity: 0.15 + depth * 0.35,
        opacity: 0,
        speedX: (Math.random() - 0.5) * 0.15,
        speedY: (Math.random() - 0.5) * 0.1,
        twinkleSpeed: 0.005 + Math.random() * 0.015,
        twinklePhase: Math.random() * Math.PI * 2,
      };
    });

    // Mouse tracking on hero section
    const section = canvas.parentElement!;
    const onMouseMove = (e: MouseEvent) => {
      const r = section.getBoundingClientRect();
      this.mouseX = e.clientX - r.left;
      this.mouseY = e.clientY - r.top;
    };
    const onMouseLeave = () => {
      this.mouseX = -1000;
      this.mouseY = -1000;
    };
    section.addEventListener('mousemove', onMouseMove);
    section.addEventListener('mouseleave', onMouseLeave);

    // Store for cleanup
    this.particleResize = resize;
    this.particleMousemove = onMouseMove;
    this.particleMouseleave = onMouseLeave;
    this.particleSection = section;
    this.particleCtx = ctx;

    // Reduced motion check
    const prefersReduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (!prefersReduced) {
      this.animateParticles();
    } else {
      this.drawParticles();
    }
  }

  private animateParticles(): void {
    const canvas = this.particleCanvas?.nativeElement;
    const ctx = this.particleCtx;
    if (!canvas || !ctx) return;

    const loop = () => {
      this.animFrameId = requestAnimationFrame(loop);
      this.updateParticles(canvas.width, canvas.height);
      this.drawParticles();
    };
    loop();
  }

  private updateParticles(w: number, h: number): void {
    for (const s of this.stars) {
      // Drift
      s.x += s.speedX;
      s.y += s.speedY;

      // Wrap around edges
      if (s.x < -5) s.x = w + 5;
      if (s.x > w + 5) s.x = -5;
      if (s.y < -5) s.y = h + 5;
      if (s.y > h + 5) s.y = -5;

      // Mouse repulsion
      const dx = s.x - this.mouseX;
      const dy = s.y - this.mouseY;
      const dist = Math.sqrt(dx * dx + dy * dy);
      if (dist < this.MOUSE_REPEL_RADIUS && dist > 0) {
        const force = (1 - dist / this.MOUSE_REPEL_RADIUS) * this.MOUSE_REPEL_FORCE;
        s.x += (dx / dist) * force;
        s.y += (dy / dist) * force;
      }

      // Twinkle (sinusoidal opacity)
      s.twinklePhase += s.twinkleSpeed;
      s.opacity = s.baseOpacity + Math.sin(s.twinklePhase) * 0.15;
    }
  }

  private drawParticles(): void {
    const canvas = this.particleCanvas?.nativeElement;
    const ctx = this.particleCtx;
    if (!canvas || !ctx) return;

    ctx.clearRect(0, 0, canvas.width, canvas.height);

    for (const s of this.stars) {
      ctx.beginPath();
      ctx.arc(s.x, s.y, s.radius, 0, Math.PI * 2);
      ctx.fillStyle = `rgba(255, 255, 255, ${Math.max(0, s.opacity)})`;
      ctx.fill();
    }
  }

  // ── Floating idle loop ──────────────────────────────────────────────
  private startFloatingLoop(): void {
    gsap.to(this.heroVisualWrapper.nativeElement, {
      y: '+=12',
      rotationX: '+=1.5',
      duration: 4,
      repeat: -1,
      yoyo: true,
      ease: 'power1.inOut',
    });
  }

  // ── Spotlight radial gradient border tracker ────────────────────────
  private readonly spotlightHandler = (e: MouseEvent): void => {
    const el = this.heroVisualContainer.nativeElement;
    const r = el.getBoundingClientRect();
    gsap.to(el, {
      '--mouse-x': `${e.clientX - r.left}px`,
      '--mouse-y': `${e.clientY - r.top}px`,
      duration: 0.25,
      ease: 'power2.out',
    });
  };

  private attachSpotlightListener(): void {
    const el = this.heroVisualContainer.nativeElement;
    el.addEventListener('mousemove', this.spotlightHandler);
  }

  ngOnDestroy(): void {
    cancelAnimationFrame(this.animFrameId);

    const section = this.particleSection;
    if (section) {
      section.removeEventListener('mousemove', this.particleMousemove);
      section.removeEventListener('mouseleave', this.particleMouseleave);
    }
    window.removeEventListener('resize', this.particleResize);

    if (this.mm) this.mm.revert();
  }
}
