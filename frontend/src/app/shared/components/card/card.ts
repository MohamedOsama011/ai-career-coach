import { Component } from '@angular/core';

@Component({
  selector: 'app-card',
  imports: [],
  template: '<ng-content />',
  styles: [`
    :host {
      display: block;
      background: var(--brand-card);
      border: 1px solid var(--brand-border);
      border-radius: 16px;
      padding: 24px;
    }
  `],
})
export class Card {}
