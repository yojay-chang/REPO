import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import { AppUser, AppUserQuery, AppUserRequest, AppRoleLookup } from '@core/models/app-user.model';

@Injectable({ providedIn: 'root' })
export class AppUserService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/app-users`;

  getAll(): Observable<AppUser[]> {
    return this.http.get<AppUser[]>(this.baseUrl);
  }

  query(query: AppUserQuery): Observable<AppUser[]> {
    return this.http.post<AppUser[]>(`${this.baseUrl}/query`, query);
  }

  getById(userId: string): Observable<AppUser> {
    return this.http.get<AppUser>(`${this.baseUrl}/${encodeURIComponent(userId)}`);
  }

  create(request: AppUserRequest): Observable<AppUser> {
    return this.http.post<AppUser>(this.baseUrl, request);
  }

  update(request: AppUserRequest): Observable<AppUser> {
    return this.http.put<AppUser>(this.baseUrl, request);
  }

  delete(userId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${encodeURIComponent(userId)}`);
  }

  /**
   * Reset the given user's password to the system default (Admin-only on the backend). Only the target
   * userId is sent — the client never sends or receives any password or hash.
   */
  resetPassword(userId: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/Auth/reset-password`, { userId });
  }

  getAppRoles(): Observable<AppRoleLookup[]> {
    return this.http.get<AppRoleLookup[]>(`${environment.apiUrl}/lookups/app-roles`);
  }
}
