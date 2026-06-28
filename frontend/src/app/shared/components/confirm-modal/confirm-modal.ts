import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-confirm-modal',
  imports: [CommonModule],
  templateUrl: './confirm-modal.html',
  styleUrl: './confirm-modal.css',
})
export class ConfirmModal {
  @Input() title = 'Are you sure?';
  @Input() message = '';
  @Input() confirmText = 'Confirm';
  @Input() cancelText = 'Cancel';
  @Input() variant: 'default' | 'danger' = 'default';
  @Input() open = false;

  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();
}
