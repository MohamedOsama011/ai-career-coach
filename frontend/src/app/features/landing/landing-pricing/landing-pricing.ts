import { Component, AfterViewInit, OnDestroy, ElementRef, ViewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import gsap from 'gsap';
import { ScrollTrigger } from 'gsap/ScrollTrigger';

interface PricingFeature {
  label: string;
  included: boolean;
  value?: string;
}

interface PricingPlan {
  name: string;
  price: string;
  period: string;
  description: string;
  features: PricingFeature[];
  highlighted: boolean;
  cta: string;
  ctaRoute: string;
}

const PLANS: PricingPlan[] = [
  {
    name: 'Basic',
    price: 'Free',
    period: 'forever',
    description: 'Try the basics \u2014 see if AI career coaching works for you.',
    highlighted: false,
    cta: 'Get Started',
    ctaRoute: '/register',
    features: [
      { label: 'Mock interview sessions', value: '1', included: true },
      { label: 'Career roadmap generations', value: '1', included: true },
      { label: 'Job recommendations', value: '3', included: true },
      { label: 'CV analysis & feedback', included: true },
      { label: 'Skills gap analysis', included: true },
      { label: 'Skills gap rescan', included: false },
      { label: 'Priority AI responses', included: false },
    ],
  },
  {
    name: 'Pro',
    price: 'EGP 399',
    period: '/month',
    description: 'For active job seekers who want a real edge.',
    highlighted: true,
    cta: 'Subscribe Now',
    ctaRoute: '/subscriptions',
    features: [
      { label: 'Mock interview sessions', value: '10', included: true },
      { label: 'Career roadmap generations', value: '5', included: true },
      { label: 'Job recommendations', value: '10', included: true },
      { label: 'CV analysis & feedback', included: true },
      { label: 'Skills gap analysis', included: true },
      { label: 'Skills gap rescan', included: true },
      { label: 'Priority AI responses', included: false },
    ],
  },
  {
    name: 'Premium',
    price: 'EGP 999',
    period: '/month',
    description: 'Unlimited access \u2014 your full career transformation toolkit.',
    highlighted: false,
    cta: 'Subscribe Now',
    ctaRoute: '/subscriptions',
    features: [
      { label: 'Mock interview sessions', value: 'Unlimited', included: true },
      { label: 'Career roadmap generations', value: 'Unlimited', included: true },
      { label: 'Job recommendations', value: 'Unlimited', included: true },
      { label: 'CV analysis & feedback', included: true },
      { label: 'Skills gap analysis', included: true },
      { label: 'Skills gap rescan', included: true },
      { label: 'Priority AI responses', included: true },
    ],
  },
];

@Component({
  selector: 'app-landing-pricing',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './landing-pricing.html',
  styleUrl: './landing-pricing.css',
})
export class LandingPricing implements AfterViewInit, OnDestroy {
  readonly plans = PLANS;

  @ViewChild('pricingSection', { static: true }) sectionRef!: ElementRef<HTMLElement>;
  private mm?: gsap.MatchMedia;

  ngAfterViewInit() {
    gsap.registerPlugin(ScrollTrigger);

    this.mm = gsap.matchMedia().add('(prefers-reduced-motion: no-preference)', () => {
      const section = this.sectionRef.nativeElement;
      const cards = gsap.utils.toArray<HTMLElement>('.pricing-card', section);
      const header = section.querySelector('.pricing-header') as HTMLElement;

      if (header) {
        gsap.set(header, { opacity: 0, y: 30 });
        gsap.to(header, {
          opacity: 1,
          y: 0,
          duration: 0.7,
          ease: 'power2.out',
          scrollTrigger: { trigger: header, start: 'top 96%' },
        });
      }

      if (cards.length) {
        gsap.set(cards, { opacity: 0, y: 50, scale: 0.95 });
        gsap.to(cards, {
          opacity: 1,
          y: 0,
          scale: 1,
          duration: 0.6,
          ease: 'power2.out',
          stagger: 0.15,
          scrollTrigger: { trigger: section, start: 'top 95%' },
        });
      }
    });
  }

  ngOnDestroy() {
    this.mm?.revert();
  }
}
