import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ViewSubscription } from './view-subscription';

describe('ViewSubscription', () => {
  let component: ViewSubscription;
  let fixture: ComponentFixture<ViewSubscription>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ViewSubscription]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ViewSubscription);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
