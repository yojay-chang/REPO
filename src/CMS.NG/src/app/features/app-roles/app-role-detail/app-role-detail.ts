import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

import { AppRole, AppUserLookup } from '@core/models/app-role.model';
import { AppRoleService } from '@core/services/app-role.service';
import { RowAuditBadge } from '@app/shared/row-audit-badge/row-audit-badge';

@Component({
  selector: 'app-app-role-detail',
  imports: [ButtonModule, TagModule, ToastModule, RowAuditBadge],
  templateUrl: './app-role-detail.html',
  styleUrl: './app-role-detail.scss',
})
export class AppRoleDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(AppRoleService);
  private readonly messageService = inject(MessageService);

  readonly role = signal<AppRole | null>(null);
  readonly loading = signal(true);
  readonly userLabels = signal<string[]>([]);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.router.navigate(['/app-roles']);
      return;
    }

    forkJoin({
      role: this.service.getById(id),
      users: this.service.getAppUsers(),
    }).subscribe({
      next: ({ role, users }) => {
        this.role.set(role);
        this.userLabels.set(this.buildUserLabels(role, users));
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: '載入失敗', detail: '找不到指定角色。' });
      },
    });
  }

  private buildUserLabels(role: AppRole, users: AppUserLookup[]): string[] {
    const byId = new Map(users.map((u) => [u.userId, u.userName]));
    return role.userIds.map((id) => {
      const name = byId.get(id);
      return name ? `${name} (${id})` : id;
    });
  }

  edit(): void {
    const role = this.role();
    if (role) {
      this.router.navigate(['/app-roles', role.roleId, 'edit']);
    }
  }

  back(): void {
    this.router.navigate(['/app-roles']);
  }
}
