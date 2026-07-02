import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Usersubscription } from './usersubscription';

describe('Usersubscription', () => {
  let component: Usersubscription;
  let fixture: ComponentFixture<Usersubscription>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Usersubscription]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Usersubscription);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
