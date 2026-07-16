import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeng/themes/aura';
import { of } from 'rxjs';

import { RowAuditBadge } from './row-audit-badge';
import { RowAuditService } from '@core/services/row-audit.service';
import { RowAuditEntry } from '@core/models/row-audit.model';

describe('RowAuditBadge', () => {
  let fixture: ComponentFixture<RowAuditBadge>;
  let component: RowAuditBadge;
  let getForRecord: jasmine.Spy;

  const trail: RowAuditEntry[] = [
    { dateTime: '2026-06-04T14:30:00', userName: 'alice', actionType: 'Update', actionDesc: 'Title' },
    { dateTime: '2026-06-01T09:00:00', userName: 'bob', actionType: 'Insert', actionDesc: 'Intro' },
  ];

  function setup(rows: RowAuditEntry[]): void {
    getForRecord = jasmine.createSpy('getForRecord').and.returnValue(of(rows));

    TestBed.configureTestingModule({
      imports: [RowAuditBadge],
      providers: [
        provideNoopAnimations(),
        providePrimeNG({ theme: { preset: Aura } }),
        { provide: RowAuditService, useValue: { getForRecord } },
      ],
    });

    fixture = TestBed.createComponent(RowAuditBadge);
    component = fixture.componentInstance;
    component.tableName = 'Course';
    component.pkid = 123;
  }

  function el(selector: string): HTMLElement | null {
    // p-dialog content may render inline or be appended near the component — search the whole document.
    return (
      (fixture.nativeElement as HTMLElement).querySelector(selector) ??
      document.querySelector(selector)
    );
  }

  it('fetches the record history and shows the latest entry inline on the badge', () => {
    setup(trail);
    component.load();
    fixture.detectChanges();

    expect(getForRecord).toHaveBeenCalledWith('Course', 123);
    expect(component.latestSummary()).toBe('Update by alice · 2026-06-04 14:30');

    const inline = el('.audit-latest');
    expect(inline?.textContent).toContain('Update by alice');
    expect(inline?.textContent).toContain('2026-06-04 14:30');
  });

  it('opens a dialog listing the full trail newest first', () => {
    setup(trail);
    component.load();
    fixture.detectChanges();

    component.open();
    fixture.detectChanges();

    expect(component.dialogVisible()).toBeTrue();

    const rows = Array.from(document.querySelectorAll('.audit-table tbody tr'));
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('Update');
    expect(rows[0].textContent).toContain('alice');
    expect(rows[1].textContent).toContain('Insert');
    expect(rows[1].textContent).toContain('bob');
  });

  it('renders a neutral empty state when there is no history', () => {
    setup([]);
    component.load();
    fixture.detectChanges();

    expect(component.hasHistory()).toBeFalse();
    expect(el('.audit-empty')?.textContent).toContain('No history');

    component.open();
    fixture.detectChanges();
    expect(el('.audit-empty-state')?.textContent).toContain('No history yet');
  });

  it('does not call the API when there is no pkid yet (new record)', () => {
    setup(trail);
    component.pkid = 0;
    component.load();
    fixture.detectChanges();

    expect(getForRecord).not.toHaveBeenCalled();
    expect(component.hasHistory()).toBeFalse();
  });
});
