import { Component, AfterViewInit, OnDestroy, ElementRef, ViewChild } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';
import gsap from 'gsap';

@Component({
  selector: 'app-auth-layout',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './auth-layout.html',
  styleUrl: './auth-layout.css',
})
export class AuthLayout implements AfterViewInit, OnDestroy {
  @ViewChild('authShapes') shapesRef!: ElementRef<HTMLElement>;
  @ViewChild('authBrand') brandRef!: ElementRef<HTMLElement>;
  @ViewChild('authForm') formRef!: ElementRef<HTMLElement>;
  private mm?: gsap.MatchMedia;

  ngAfterViewInit() {
    this.mm = gsap.matchMedia().add('(prefers-reduced-motion: no-preference)', () => {
      const shapes = this.shapesRef?.nativeElement.querySelectorAll('.auth-shape');
      const brandEls = this.brandRef?.nativeElement.querySelectorAll('.auth-brand-line');
      const formEls = this.formRef?.nativeElement.querySelectorAll('.auth-form-animate');

      if (shapes?.length) {
        gsap.from(shapes, {
          scale: 0.8,
          opacity: 0,
          duration: 1.2,
          stagger: 0.2,
          ease: 'power2.out',
        });
        shapes.forEach((shape, i) => {
          gsap.to(shape, {
            y: '+=15',
            rotation: i % 2 === 0 ? '+=5' : '-=5',
            duration: 5 + i * 1.5,
            repeat: -1,
            yoyo: true,
            ease: 'sine.inOut',
          });
        });
      }

      if (brandEls?.length) {
        gsap.from(brandEls, {
          y: 20,
          opacity: 0,
          duration: 0.8,
          stagger: 0.15,
          delay: 0.3,
          ease: 'power2.out',
        });
      }

      if (formEls?.length) {
        gsap.from(formEls, {
          x: 20,
          opacity: 0,
          duration: 0.5,
          stagger: 0.08,
          delay: 0.4,
          ease: 'power2.out',
        });
      }
    });
  }

  ngOnDestroy() {
    this.mm?.revert();
  }
}
