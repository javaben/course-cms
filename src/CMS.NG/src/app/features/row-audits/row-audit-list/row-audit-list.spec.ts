import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';
import Aura from '@primeng/themes/aura';
import { of } from 'rxjs';

import { RowAuditList } from './row-audit-list';
import { RowAuditService } from '@core/services/row-audit.service';
import { RowAuditListItem } from '@core/models/row-audit.model';

describe('RowAuditList', () => {
  let fixture: ComponentFixture<RowAuditList>;
  let component: RowAuditList;
  let query: jasmine.Spy;
  let getTableNames: jasmine.Spy;

  const rows: RowAuditListItem[] = [
    {
      pkid: 2,
      tableName: 'Partner',
      primaryKeyValues: '5',
      userName: 'alice',
      actionType: 'Update',
      actionDesc: 'Name',
      dateTime: '2026-06-06T08:00:00',
    },
    {
      pkid: 1,
      tableName: 'Course',
      primaryKeyValues: '123',
      userName: 'bob',
      actionType: 'Insert',
      actionDesc: 'Intro',
      dateTime: '2026-06-01T09:00:00',
    },
  ];

  function setup(list: RowAuditListItem[], tables: string[] = ['Course', 'Partner']): void {
    sessionStorage.clear();
    query = jasmine.createSpy('query').and.returnValue(of(list));
    getTableNames = jasmine.createSpy('getTableNames').and.returnValue(of(tables));

    TestBed.configureTestingModule({
      imports: [RowAuditList],
      providers: [
        provideNoopAnimations(),
        providePrimeNG({ theme: { preset: Aura } }),
        { provide: MessageService, useValue: { add: jasmine.createSpy('add') } },
        { provide: RowAuditService, useValue: { query, getTableNames } },
      ],
    });

    fixture = TestBed.createComponent(RowAuditList);
    component = fixture.componentInstance;
  }

  it('loads the global log on init and renders rows newest first', () => {
    setup(rows);
    fixture.detectChanges();

    expect(query).toHaveBeenCalledWith({ tableName: null, actionType: null, keyword: null });
    expect(component.items().length).toBe(2);

    const bodyRows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(bodyRows.length).toBe(2);
    expect(bodyRows[0].textContent).toContain('Partner');
    expect(bodyRows[0].textContent).toContain('alice');
    expect(bodyRows[1].textContent).toContain('Course');
  });

  it('loads the distinct table names for the filter picker', () => {
    setup(rows);
    fixture.detectChanges();

    expect(getTableNames).toHaveBeenCalled();
    expect(component.tableNames()).toEqual(['Course', 'Partner']);
  });

  it('applies the current filters through the service query', () => {
    setup(rows);
    fixture.detectChanges();
    query.calls.reset();

    component.filters = { tableName: 'Course', actionType: 'Update', keyword: 'bob' };
    component.applyFilters();

    expect(query).toHaveBeenCalledWith({ tableName: 'Course', actionType: 'Update', keyword: 'bob' });
  });

  it('shows the empty state when there is no history', () => {
    setup([]);
    fixture.detectChanges();

    expect(component.items().length).toBe(0);
    const text: string = fixture.nativeElement.textContent;
    expect(text).toContain('No history yet');
  });
});
