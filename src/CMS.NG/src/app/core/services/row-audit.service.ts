import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { RowAuditEntry, RowAuditListItem, RowAuditQuery } from '@core/models/row-audit.model';

@Injectable({ providedIn: 'root' })
export class RowAuditService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/rowaudit`;

  /** The audit history for one record (TableName + pkid), newest first. */
  getForRecord(tableName: string, pkid: number | string): Observable<RowAuditEntry[]> {
    const params = new HttpParams().set('tableName', tableName).set('pkid', String(pkid));
    return this.http.get<RowAuditEntry[]>(this.baseUrl, { params });
  }

  /** The whole audit log across all tables, newest first (capped). */
  getAll(): Observable<RowAuditListItem[]> {
    return this.http.get<RowAuditListItem[]>(`${this.baseUrl}/all`);
  }

  /** Filtered audit log (table / action / keyword), newest first (capped). */
  query(query: RowAuditQuery): Observable<RowAuditListItem[]> {
    return this.http.post<RowAuditListItem[]>(`${this.baseUrl}/query`, query);
  }

  /** Distinct table names present in the log — for the filter picker. */
  getTableNames(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/tables`);
  }
}
