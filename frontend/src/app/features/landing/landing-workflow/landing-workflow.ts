import {
  Component,
  ElementRef,
  QueryList,
  ViewChild,
  ViewChildren,
  AfterViewInit,
  OnDestroy,
  signal,
} from '@angular/core';
import { gsap } from 'gsap';
import { ScrollTrigger } from 'gsap/ScrollTrigger';

gsap.registerPlugin(ScrollTrigger);

interface WorkflowNode {
  readonly num: '01' | '02' | '03' | '04' | '05' | '06' | '07';
  readonly title: string;
  readonly description: string;
  readonly icon: string;
  readonly metric: string;
  readonly side: 'left' | 'right';
}

/** 7 nodes = the actual AI engine pipeline, grounded in the codebase:
 *  EmbeddingService (text-embedding-3-small), JobRecommendationService
 *  (cosine similarity, top 5), SkillExtractionService (batched 5/call),
 *  RoadmapLlmService (two-pass), InterviewLlmService (SSE, t=0.5, 500 tokens). */
const NODES: readonly WorkflowNode[] = [
  {
    num: '01',
    title: 'CV Ingestion',
    description: 'PDF, DOCX, or plain text. Extracted with line-level precision; trimmed to 12 000 chars before any AI call.',
    icon: 'file_upload',
    metric: '12000 chars',
    side: 'left',
  },
  {
    num: '02',
    title: 'Embedding Generation',
    description: 'text-embedding-3-small maps your CV into a vector in the same space as every job in our index.',
    icon: 'data_object',
    metric: '1536-dim',
    side: 'right',
  },
  {
    num: '03',
    title: 'Skill Extraction',
    description: 'gpt-4o-mini normalizes skills from your CV, batched 5 jobs per call to stay under rate limits.',
    icon: 'auto_awesome',
    metric: 'gpt-4o-mini',
    side: 'left',
  },
  {
    num: '04',
    title: 'Role Matching',
    description: 'Cosine similarity ranks your vector against the job index. Top 5 surface in under a second.',
    icon: 'hub',
    metric: 'Top 5',
    side: 'right',
  },
  {
    num: '05',
    title: 'Gap Analysis',
    description: 'Priority-ranked gaps. Current level vs. required — not a template, but the exact skills you don\'t have yet.',
    icon: 'compare_arrows',
    metric: 'Priority',
    side: 'left',
  },
  {
    num: '06',
    title: 'Roadmap Generation',
    description: 'Two-pass LLM. Pass 1: assess seniority + identify gaps. Pass 2: build steps ordered by priority.',
    icon: 'route',
    metric: '2-pass',
    side: 'right',
  },
  {
    num: '07',
    title: 'Interview Simulation',
    description: 'Streaming SSE at temp 0.5, max 500 tokens. The coach knows your CV, gaps, and target role.',
    icon: 'graphic_eq',
    metric: 'SSE · t=0.5',
    side: 'left',
  },
];

@Component({
  selector: 'app-landing-workflow',
  standalone: true,
  imports: [],
  templateUrl: './landing-workflow.html',
  styleUrl: './landing-workflow.css',
})
export class LandingWorkflow implements AfterViewInit, OnDestroy {
  @ViewChild('sectionRoot')    sectionRoot!:   ElementRef<HTMLElement>;
  @ViewChild('lineFill')       lineFill!:      ElementRef<SVGPathElement>;
  @ViewChildren('workflowNode') workflowNodes!: QueryList<ElementRef<HTMLElement>>;

  /** Source-of-truth data for the 7 timeline nodes. */
  readonly nodes: readonly WorkflowNode[] = NODES;

  /** Index of the topmost node whose row is at or above the line's current
   *  draw position. Used to flip the matching numbered circle to its
   *  "reached" (violet-fill) state. Computed via DOM lookup, not a signal. */
  readonly reachedCount = signal<number>(0);

  private mm!: gsap.MatchMedia;
  private lineLength = 0;

  ngAfterViewInit(): void {
    this.mm = gsap.matchMedia();

    // Measure line length once layout has settled.
    this.lineLength = this.lineFill.nativeElement.getTotalLength();

    // ── Reduced motion: line fully drawn, all nodes visible (final state). ──
    this.mm.add('(prefers-reduced-motion: reduce)', () => {
      gsap.set(this.lineFill.nativeElement, { strokeDashoffset: 0 });
      this.reachedCount.set(NODES.length);
    });

    // ── Normal motion: scroll-tied line draw + per-node reveal. ──
    this.mm.add('(prefers-reduced-motion: no-preference)', () => {
      this.setupScrollTiedLine();
      this.setupNodeReveals();
    });
  }

  ngOnDestroy(): void {
    if (this.mm) this.mm.revert();
    // Safety net for HMR.
    ScrollTrigger.getAll().forEach((t) => t.kill());
  }

  // ── Scroll-tied line draw ────────────────────────────────────────────
  // `scrub: 1` ties the strokeDashoffset to scroll position with 1s of
  // smoothing. The line draws top-to-bottom as the user scrolls through
  // the section. Bidirectional — scrolling up un-draws the line.
  private setupScrollTiedLine(): void {
    const line = this.lineFill.nativeElement;
    gsap.set(line, { strokeDasharray: this.lineLength, strokeDashoffset: this.lineLength });

    gsap.to(line, {
      strokeDashoffset: 0,
      ease: 'none',  // scrub requires linear ease
      scrollTrigger: {
        trigger: this.sectionRoot.nativeElement,
        start: 'top 75%',
        end: 'bottom 20%',
        scrub: 1,
        onUpdate: (self) => {
          // Update reached count: how many nodes the line has passed.
          // self.progress is 0..1 across the section's scroll range.
          const reached = Math.min(NODES.length, Math.floor(self.progress * NODES.length * 1.1) + 1);
          this.reachedCount.set(reached);
        },
      },
    });
  }

  // ── Per-node directional reveal ─────────────────────────────────────
  // Left cards slide in from left, right cards from right, with scale.
  // Card internals (icon → title → desc → metric) stagger in after a
  // short delay. Number circles pop with overshoot.
  private setupNodeReveals(): void {
    this.workflowNodes.forEach((nodeRef, i) => {
      const node = NODES[i];
      const isLeft = node.side === 'left';
      const card = nodeRef.nativeElement.querySelector('.workflow-node-card');
      const circle = nodeRef.nativeElement.querySelector('.workflow-node-circle');

      // Card: slide in from its side with scale
      if (card) {
        gsap.from(card, {
          x: isLeft ? -60 : 60,
          opacity: 0,
          scale: 0.95,
          duration: 0.7,
          ease: 'power3.out',
          scrollTrigger: {
            trigger: nodeRef.nativeElement,
            start: 'top 90%',
            toggleActions: 'play none none reverse',
          },
        });

        // Stagger internal elements
        const children = card.querySelectorAll(
          '.workflow-card-icon, .workflow-card-title, .workflow-card-desc, .workflow-card-metric'
        );
        if (children.length) {
          gsap.from(children, {
            opacity: 0,
            y: 12,
            duration: 0.4,
            stagger: 0.08,
            ease: 'power2.out',
            delay: 0.2,
            scrollTrigger: {
              trigger: nodeRef.nativeElement,
              start: 'top 90%',
              toggleActions: 'play none none reverse',
            },
          });
        }
      }

      // Circle: pop in with overshoot
      if (circle) {
        gsap.from(circle, {
          scale: 0,
          opacity: 0,
          duration: 0.5,
          ease: 'back.out(1.7)',
          scrollTrigger: {
            trigger: nodeRef.nativeElement,
            start: 'top 90%',
            toggleActions: 'play none none reverse',
          },
        });
      }
    });
  }

  // ── Template helpers ─────────────────────────────────────────────────
  trackNode(_: number, n: WorkflowNode): string { return n.num; }

  /** True when the node's number circle should render in its "reached"
   *  state (violet fill). 1-based. */
  isReached(numStr: string): boolean {
    const n = parseInt(numStr, 10);
    return n <= this.reachedCount();
  }
}
