import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreateSubscription } from './create-subscription';

describe('CreateSubscription', () => {
  let component: CreateSubscription;
  let fixture: ComponentFixture<CreateSubscription>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateSubscription]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CreateSubscription);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
