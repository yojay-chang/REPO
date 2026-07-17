import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService, ConfirmationService } from 'primeng/api';

import { Partner, PartnerQuery } from '@core/models/partner.model';
import { PartnerService } from '@core/services/partner.service';

const FILTERS_KEY = 'partner-list-filters';
const SORT_KEY = 'partner-list-sort';
const PAGE_KEY = 'partner-list-page';

@Component({
  selector: 'app-partner-list',
  imports: [
    FormsModule,
    TableModule,
    ButtonModule,
    DrawerModule,
    InputTextModule,
    ToastModule,
    ConfirmDialogModule,
    TooltipModule,
  ],
  templateUrl: './partner-list.html',
  styleUrl: './partner-list.scss',
})
export class PartnerList implements OnInit {
  private readonly service = inject(PartnerService);
  private readonly router = inject(Router);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  readonly partners = signal<Partner[]>([]);
  readonly loading = signal(false);
  readonly filterVisible = signal(false);

  filter: PartnerQuery = {
    keyword: null,
  };

  sortField = 'displayOrder';
  sortOrder = 1;
  first = 0;
  rows = 20;

  ngOnInit(): void {
    this.restoreState();
    this.load();
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
        this.partners.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: '載入失敗', detail: '無法取得合作廠商資料。' });
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
    this.filter = { keyword: null };
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
    this.router.navigate(['/partners/new']);
  }

  view(partner: Partner): void {
    this.router.navigate(['/partners', partner.pkid]);
  }

  edit(partner: Partner): void {
    this.router.navigate(['/partners', partner.pkid, 'edit']);
  }

  confirmDelete(partner: Partner): void {
    this.confirmationService.confirm({
      header: '刪除確認',
      message: `確定要刪除主代碼 <b>${partner.pkid}</b>「${partner.name}」？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '刪除',
      rejectLabel: '取消',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.delete(partner),
    });
  }

  private delete(partner: Partner): void {
    this.service.delete(partner.pkid).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: '已刪除', detail: `合作廠商「${partner.name}」已刪除。` });
        this.load();
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: '刪除失敗', detail: '無法刪除合作廠商，可能仍有課程關聯。' });
      },
    });
  }
}
