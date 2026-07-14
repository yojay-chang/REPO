import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import { AppRole, AppRoleQuery, AppRoleRequest, AppUserLookup } from '@core/models/app-role.model';

@Injectable({ providedIn: 'root' })
export class AppRoleService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/app-roles`;

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

  update(request: AppRoleRequest): Observable<AppRole> {
    return this.http.put<AppRole>(this.baseUrl, request);
  }

  delete(roleId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${encodeURIComponent(roleId)}`);
  }

  getAppUsers(): Observable<AppUserLookup[]> {
    return this.http.get<AppUserLookup[]>(`${environment.apiUrl}/lookups/app-users`);
  }
}
