import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService, ConfirmationService } from 'primeng/api';

import { AppUser, AppUserQuery, AppRoleLookup } from '@core/models/app-user.model';
import { AppUserService } from '@core/services/app-user.service';

const FILTERS_KEY = 'app-user-list-filters';
const SORT_KEY = 'app-user-list-sort';
const PAGE_KEY = 'app-user-list-page';

interface TriStateOption {
  label: string;
  value: boolean | null;
}

interface RoleOption {
  roleId: string;
  label: string;
}

@Component({
  selector: 'app-app-user-list',
  imports: [
    DatePipe,
    FormsModule,
    TableModule,
    ButtonModule,
    DrawerModule,
    InputTextModule,
    SelectModule,
    ToastModule,
    ConfirmDialogModule,
    TooltipModule,
  ],
  templateUrl: './app-user-list.html',
  styleUrl: './app-user-list.scss',
})
export class AppUserList implements OnInit {
  private readonly service = inject(AppUserService);
  private readonly router = inject(Router);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  readonly users = signal<AppUser[]>([]);
  readonly loading = signal(false);
  readonly filterVisible = signal(false);
  readonly roleOptions = signal<RoleOption[]>([]);

  readonly triStateOptions: TriStateOption[] = [
    { label: '全部', value: null },
    { label: '是', value: true },
    { label: '否', value: false },
  ];

  filter: AppUserQuery = { keyword: null, isActive: null, roleId: null };

  sortField = 'userId';
  sortOrder = 1;
  first = 0;
  rows = 20;

  ngOnInit(): void {
    this.restoreState();
    this.service.getAppRoles().subscribe({
      next: (roles) => this.roleOptions.set(this.buildRoleOptions(roles)),
      error: () => {},
    });
    this.load();
  }

  private buildRoleOptions(roles: AppRoleLookup[]): RoleOption[] {
    return roles.map((r) => ({ roleId: r.roleId, label: `${r.roleName} (${r.roleId})` }));
  }

  private restoreState(): void {
    const savedFilters = sessionStorage.getItem(FILTERS_KEY);
    if (savedFilters) {
      this.filter = { ...this.filter, ...JSON.parse(savedFilters) };
    }
    const savedSort = sessionStorage.getItem(SORT_KEY);
    if (savedSort) {
      const s = JSON.parse(savedSort);
      this.sortField = s.sortField ?? this.sortField;
      this.sortOrder = s.sortOrder ?? this.sortOrder;
    }
    const savedPage = sessionStorage.getItem(PAGE_KEY);
    if (savedPage) {
      const p = JSON.parse(savedPage);
      this.first = p.first ?? 0;
      this.rows = p.rows ?? 20;
    }
  }

  load(): void {
    this.loading.set(true);
    this.service.query(this.filter).subscribe({
      next: (data) => {
        this.users.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: '載入失敗', detail: '無法取得使用者資料。' });
      },
    });
  }

  applyFilter(): void {
    sessionStorage.setItem(FILTERS_KEY, JSON.stringify(this.filter));
    this.first = 0;
    this.persistPage();
    this.filterVisible.set(false);
    this.load();
  }

  clearFilter(): void {
    this.filter = { keyword: null, isActive: null, roleId: null };
    sessionStorage.removeItem(FILTERS_KEY);
    this.load();
  }

  onSort(event: { field?: string; order?: number }): void {
    if (event.field) {
      this.sortField = event.field;
      this.sortOrder = event.order ?? 1;
      sessionStorage.setItem(
        SORT_KEY,
        JSON.stringify({ sortField: this.sortField, sortOrder: this.sortOrder }),
      );
    }
  }

  onPage(event: { first?: number; rows?: number }): void {
    this.first = event.first ?? 0;
    this.rows = event.rows ?? 20;
    this.persistPage();
  }

  private persistPage(): void {
    sessionStorage.setItem(PAGE_KEY, JSON.stringify({ first: this.first, rows: this.rows }));
  }

  add(): void {
    this.router.navigate(['/app-users/new']);
  }

  view(user: AppUser): void {
    this.router.navigate(['/app-users', user.userId]);
  }

  edit(user: AppUser): void {
    this.router.navigate(['/app-users', user.userId, 'edit']);
  }

  confirmDelete(user: AppUser): void {
    this.confirmationService.confirm({
      header: '刪除確認',
      message: `確定要刪除主代碼 <b>${user.pkid}</b>「${user.userId}」？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '刪除',
      rejectLabel: '取消',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.delete(user),
    });
  }

  private delete(user: AppUser): void {
    this.service.delete(user.userId).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: '已刪除', detail: `使用者「${user.userId}」已刪除。` });
        this.load();
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: '刪除失敗', detail: '無法刪除使用者，可能仍有關聯資料。' });
      },
    });
  }
}
