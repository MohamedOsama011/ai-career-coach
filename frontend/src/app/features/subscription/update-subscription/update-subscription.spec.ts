import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UpdateSubscription } from './update-subscription';

describe('UpdateSubscription', () => {
  let component: UpdateSubscription;
  let fixture: ComponentFixture<UpdateSubscription>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UpdateSubscription]
    })
    .compileComponents();

    fixture = TestBed.createComponent(UpdateSubscription);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
