import {
  Component,
  ElementRef,
  QueryList,
  ViewChild,
  ViewChildren,
  AfterViewInit,
  OnDestroy,
  signal,
  NgZone,
  inject,
} from '@angular/core';
import { gsap } from 'gsap';
import { ScrollTrigger } from 'gsap/ScrollTrigger';
import { MorphSVGPlugin } from 'gsap/MorphSVGPlugin';

gsap.registerPlugin(ScrollTrigger, MorphSVGPlugin);

type FeatureId = 'cv' | 'gap' | 'roadmap' | 'interview' | 'jobs';

interface BentoFeature {
  readonly id: FeatureId;
  readonly title: string;
  readonly description: string;
  readonly cellSize: 'wide' | 'normal';
  readonly microViz: 'cv-card' | 'bar-chart' | 'roadmap-line' | 'waveform' | 'job-card';
}

const FEATURES: readonly BentoFeature[] = [
  { id: 'cv',        title: 'CV Analysis',          description: 'Parse, score, and surface what your CV is missing.',         cellSize: 'normal', microViz: 'cv-card' },
  { id: 'gap',       title: 'Skills Gap',           description: 'See the exact skills separating you from your target.',      cellSize: 'normal', microViz: 'bar-chart' },
  { id: 'roadmap',   title: 'Personalized Roadmap', description: 'A gap-driven learning path, ordered by priority.',            cellSize: 'wide',   microViz: 'roadmap-line' },
  { id: 'interview', title: 'Mock Interview',       description: 'Practice with an AI coach that knows your weak spots.',       cellSize: 'normal', microViz: 'waveform' },
  { id: 'jobs',      title: 'Job Match',            description: 'See real openings ranked by fit + missing skills.',           cellSize: 'normal', microViz: 'job-card' },
];

/** 5 source paths for the hero cell's MorphSVG animation. Different point
 *  counts (5-10 each) produce organic, AI-like intermediate states when
 *  MorphSVG normalizes them. This is intentional and on-brand. */
const HERO_MORPH_PATHS: readonly string[] = [
  'M12 8 L30 8 L40 18 L40 42 L12 42 Z M30 8 L30 18 L40 18',
  'M10 40 L20 40 L20 26 L30 26 L25 18 L40 18 L35 26 L45 26 L40 18 M25 10 L25 18',
  'M8 25 L18 25 A4 4 0 1 0 26 25 A4 4 0 1 0 18 25 M26 25 L36 25 A4 4 0 1 0 44 25 A4 4 0 1 0 36 25',
  'M5 25 L11 12 L17 38 L23 8 L29 42 L35 14 L41 36 L47 25',
  'M18 20 L18 38 L42 38 L42 20 L34 20 Q34 12, 30 12 L26 12 Q22 12, 22 20 Z',
];

const HERO_MORPH_LABELS: readonly string[] = [
  'CV Analysis',
  'Skills Gap',
  'Personalized Roadmap',
  'Mock Interview',
  'Job Match',
];

@Component({
  selector: 'app-landing-bento',
  standalone: true,
  imports: [],
  templateUrl: './landing-bento.html',
  styleUrl: './landing-bento.css',
})
export class LandingBento implements AfterViewInit, OnDestroy {
  @ViewChild('sectionRoot') sectionRoot!: ElementRef<HTMLElement>;
  @ViewChild('heroLabel')   heroLabel!:   ElementRef<HTMLSpanElement>;
  @ViewChild('heroMorph')   heroMorph!:   ElementRef<SVGPathElement>;
  @ViewChildren('bentoCell') bentoCells!: QueryList<ElementRef<HTMLElement>>;

  private readonly zone = inject(NgZone);

  /** Source-of-truth data for the 5 feature cells. */
  readonly features: readonly BentoFeature[] = FEATURES;
  readonly heroLabels: readonly string[] = HERO_MORPH_LABELS;

  /** Live state — signals drive both the template and the GSAP timeline. */
  readonly activeMorphIndex = signal<number>(0);
  readonly bentoInView     = signal<boolean>(false);

  /** Convenience for the template — maps cell index to the active feature. */
  readonly activeFeatureLabel = (): string => HERO_MORPH_LABELS[this.activeMorphIndex()];

  private mm!: gsap.MatchMedia;
  private morphInterval?: ReturnType<typeof setInterval>;
  private intersectionObserver?: IntersectionObserver;
  private readonly spotlightCleanups: Array<() => void> = [];

  ngAfterViewInit(): void {
    // Initial hero shape = path 0 (CV) — the morph cycle below will rotate it.
    this.heroMorph.nativeElement.setAttribute('d', HERO_MORPH_PATHS[0]);

    this.mm = gsap.matchMedia();

    // ── Reduced motion: show cells in final state, no morph, no scroll reveal. ──
    this.mm.add('(prefers-reduced-motion: reduce)', () => {
      // CSS already sets cells to opacity 1 / transform none. Nothing to do.
    });

    // ── Normal motion: full reveal + morph cycle + spotlight listeners. ──
    this.mm.add('(prefers-reduced-motion: no-preference)', () => {
      this.setupIntersectionObserver();
      this.setupSpotlightListeners();
      this.setupScrollReveal();
      this.startMorphCycle();
    });
  }

  ngOnDestroy(): void {
    this.stopMorphCycle();
    this.intersectionObserver?.disconnect();
    this.spotlightCleanups.forEach((fn) => fn());
    this.spotlightCleanups.length = 0;
    if (this.mm) this.mm.revert();
    // Safety: any stragglers (ScrollTrigger + MorphSVG are both reverted by mm,
    // but explicit kill is a cheap belt-and-suspenders for HMR).
    ScrollTrigger.getAll().forEach((t) => t.kill());
  }

  // ── IntersectionObserver — pause morph cycle when off-screen. ──────────
  private setupIntersectionObserver(): void {
    this.zone.runOutsideAngular(() => {
      this.intersectionObserver = new IntersectionObserver(
        (entries) => {
          for (const entry of entries) {
            this.bentoInView.set(entry.isIntersecting);
            if (!entry.isIntersecting) {
              this.stopMorphCycle();
            } else if (this.morphInterval === undefined) {
              this.startMorphCycle();
            }
          }
        },
        { threshold: 0.2 },
      );
      this.intersectionObserver.observe(this.sectionRoot.nativeElement);
    });
  }

  // ── Spotlight mouse tracker — reused from landing.ts:143-157, per-cell. ─
  // Each cell listens to mousemove, updates --mouse-x / --mouse-y CSS vars
  // (400ms GSAP tween), CSS paints a radial-gradient border under the cursor.
  private setupSpotlightListeners(): void {
    this.bentoCells.forEach((cellRef) => {
      const el = cellRef.nativeElement;
      const handler = (e: MouseEvent): void => {
        const r = el.getBoundingClientRect();
        gsap.to(el, {
          '--mouse-x': `${e.clientX - r.left}px`,
          '--mouse-y': `${e.clientY - r.top}px`,
          duration: 0.25,
          ease: 'power2.out',
        });
      };
      el.addEventListener('mousemove', handler);
      this.spotlightCleanups.push(() => el.removeEventListener('mousemove', handler));
    });
  }

  // ── ScrollTrigger reveal — staggered fade-in for the 6 cells. ──────────
  private setupScrollReveal(): void {
    const cells = this.bentoCells.map((c) => c.nativeElement);
    // Initial state set in matchMedia scope so revert() restores the cells.
    gsap.set(cells, { opacity: 0, y: 40 });
    gsap.to(cells, {
      opacity: 1,
      y: 0,
      duration: 0.7,
      stagger: 0.12,
      ease: 'power2.out',
      scrollTrigger: {
        trigger: this.sectionRoot.nativeElement,
        start: 'top 92%',
        toggleActions: 'play none none none',
      },
    });
  }

  // ── Morph cycle — cycles 0..4 every 2.5s; pauses when off-screen. ──────
  private startMorphCycle(): void {
    if (this.morphInterval || !this.bentoInView()) return;
    this.morphInterval = setInterval(() => {
      if (!this.bentoInView()) {
        this.stopMorphCycle();
        return;
      }
      const next = (this.activeMorphIndex() + 1) % HERO_MORPH_PATHS.length;
      this.setHeroPath(next);
    }, 2500);
  }

  private stopMorphCycle(): void {
    if (this.morphInterval) {
      clearInterval(this.morphInterval);
      this.morphInterval = undefined;
    }
  }

  private setHeroPath(index: number): void {
    const targetPath = HERO_MORPH_PATHS[index];
    gsap.to(this.heroMorph.nativeElement, {
      morphSVG: targetPath,
      duration: 1.2,
      ease: 'power2.inOut',
    });
    this.zone.run(() => {
      this.activeMorphIndex.set(index);
      // Cross-fade the label in sync with the morph.
      if (this.heroLabel) {
        gsap.fromTo(
          this.heroLabel.nativeElement,
          { opacity: 0, y: 4 },
          { opacity: 1, y: 0, duration: 0.4, ease: 'power2.out', overwrite: true },
        );
      }
    });
  }

  // ── Template helpers ──────────────────────────────────────────────────
  trackFeature(_: number, f: BentoFeature): string { return f.id; }
}
