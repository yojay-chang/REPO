import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import { RowAuditEntry } from '@core/models/row-audit.model';

@Injectable({ providedIn: 'root' })
export class RowAuditService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/rowaudit`;

  /** The audit trail for one record (TableName + numeric pkid), newest first. */
  getForRecord(tableName: string, pkid: number): Observable<RowAuditEntry[]> {
    const params = new HttpParams().set('tableName', tableName).set('pkid', pkid);
    return this.http.get<RowAuditEntry[]>(this.baseUrl, { params });
  }
}
