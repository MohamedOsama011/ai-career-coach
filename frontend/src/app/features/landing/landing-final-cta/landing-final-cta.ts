import { Component, AfterViewInit, OnDestroy, ElementRef, ViewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import gsap from 'gsap';
import { ScrollTrigger } from 'gsap/ScrollTrigger';

@Component({
  selector: 'app-landing-final-cta',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './landing-final-cta.html',
  styleUrl: './landing-final-cta.css',
})
export class LandingFinalCta implements AfterViewInit, OnDestroy {
  @ViewChild('ctaSection', { static: true }) sectionRef!: ElementRef<HTMLElement>;
  private mm?: gsap.MatchMedia;

  ngAfterViewInit() {
    gsap.registerPlugin(ScrollTrigger);

    this.mm = gsap.matchMedia().add('(prefers-reduced-motion: no-preference)', () => {
      const section = this.sectionRef.nativeElement;
      const glow = section.querySelector('.final-cta-glow') as HTMLElement;
      const card = section.querySelector('.final-cta-card') as HTMLElement;
      const heading = section.querySelector('.final-cta-title') as HTMLElement;
      const subtitle = section.querySelector('.final-cta-subtitle') as HTMLElement;
      const cta = section.querySelector('.final-cta-action') as HTMLElement;
      const note = section.querySelector('.final-cta-note') as HTMLElement;

      if (glow) {
        gsap.fromTo(glow, { y: 40, opacity: 0.3 }, {
          y: -30,
          opacity: 0.6,
          ease: 'none',
          scrollTrigger: { trigger: section, start: 'top bottom', end: 'bottom top', scrub: true },
        });
      }

      if (card) {
        const els = [heading, subtitle, cta, note].filter(Boolean);
        gsap.set(els, { opacity: 0, y: 24 });
        gsap.to(els, {
          opacity: 1,
          y: 0,
          duration: 0.6,
          ease: 'power2.out',
          stagger: 0.1,
          scrollTrigger: { trigger: card, start: 'top 90%' },
        });
      }
    });
  }

  ngOnDestroy() {
    this.mm?.revert();
  }
}
