import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

interface FooterLink {
  label: string;
  href: string;
  external?: boolean;
}

interface FooterColumn {
  title: string;
  links: FooterLink[];
}

const COLUMNS: FooterColumn[] = [
  {
    title: 'Product',
    links: [
      { label: 'Features', href: '/' },
      { label: 'Pricing', href: '/#pricing' },
      { label: 'Roadmap', href: '/roadmap' },
    ],
  },
  {
    title: 'Company',
    links: [
      { label: 'About', href: '/', external: true },
      { label: 'Careers', href: '/', external: true },
      { label: 'Contact', href: '/', external: true },
    ],
  },
  {
    title: 'Resources',
    links: [
      { label: 'Blog', href: '/', external: true },
      { label: 'Help Center', href: '/', external: true },
      { label: 'API Docs', href: '/', external: true },
    ],
  },
  {
    title: 'Legal',
    links: [
      { label: 'Privacy Policy', href: '/', external: true },
      { label: 'Terms of Service', href: '/', external: true },
      { label: 'Cookie Policy', href: '/', external: true },
    ],
  },
];

@Component({
  selector: 'app-landing-footer',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './landing-footer.html',
  styleUrl: './landing-footer.css',
})
export class LandingFooter {
  readonly columns = COLUMNS;
}
