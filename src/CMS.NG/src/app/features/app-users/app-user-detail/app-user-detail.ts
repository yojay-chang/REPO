import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

import { AppUser, AppRoleLookup } from '@core/models/app-user.model';
import { AppUserService } from '@core/services/app-user.service';
import { RowAuditBadge } from '@app/shared/row-audit-badge/row-audit-badge';

@Component({
  selector: 'app-app-user-detail',
  imports: [DatePipe, ButtonModule, TagModule, ToastModule, RowAuditBadge],
  templateUrl: './app-user-detail.html',
  styleUrl: './app-user-detail.scss',
})
export class AppUserDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(AppUserService);
  private readonly messageService = inject(MessageService);

  readonly user = signal<AppUser | null>(null);
  readonly loading = signal(true);
  readonly roleLabels = signal<string[]>([]);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.router.navigate(['/app-users']);
      return;
    }

    forkJoin({
      user: this.service.getById(id),
      roles: this.service.getAppRoles(),
    }).subscribe({
      next: ({ user, roles }) => {
        this.user.set(user);
        this.roleLabels.set(this.buildRoleLabels(user, roles));
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: '載入失敗', detail: '找不到指定使用者。' });
      },
    });
  }

  private buildRoleLabels(user: AppUser, roles: AppRoleLookup[]): string[] {
    const byId = new Map(roles.map((r) => [r.roleId, r.roleName]));
    return user.roleIds.map((id) => {
      const name = byId.get(id);
      return name ? `${name} (${id})` : id;
    });
  }

  edit(): void {
    const user = this.user();
    if (user) {
      this.router.navigate(['/app-users', user.userId, 'edit']);
    }
  }

  back(): void {
    this.router.navigate(['/app-users']);
  }
}
