import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { AppRole, AppRoleQuery, AppRoleRequest } from '@core/models/app-role.model';

@Injectable({ providedIn: 'root' })
export class AppRoleService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/app-roles`;

  getAll(): Observable<AppRole[]> {
    return this.http.get<AppRole[]>(this.baseUrl);
  }

  query(query: AppRoleQuery): Observable<AppRole[]> {
    return this.http.post<AppRole[]>(`${this.baseUrl}/query`, query);
  }

  getById(roleId: string): Observable<AppRole> {
    return this.http.get<AppRole>(`${this.baseUrl}/${encodeURIComponent(roleId)}`);
  }

  create(request: AppRoleRequest): Observable<AppRole> {
    return this.http.post<AppRole>(this.baseUrl, request);
  }

  update(request: AppRoleRequest): Observable<void> {
    return this.http.put<void>(this.baseUrl, request);
  }

  delete(roleId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${encodeURIComponent(roleId)}`);
  }
}
