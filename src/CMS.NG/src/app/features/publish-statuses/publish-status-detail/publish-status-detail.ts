import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

import { PublishStatus } from '@core/models/publish-status.model';
import { PublishStatusService } from '@core/services/publish-status.service';

@Component({
  selector: 'app-publish-status-detail',
  imports: [ButtonModule, TagModule, ToastModule],
  templateUrl: './publish-status-detail.html',
  styleUrl: './publish-status-detail.scss',
})
export class PublishStatusDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(PublishStatusService);
  private readonly messageService = inject(MessageService);

  readonly status = signal<PublishStatus | null>(null);
  readonly loading = signal(true);

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam === null) {
      this.router.navigate(['/publish-statuses']);
      return;
    }

    this.service.getById(Number(idParam)).subscribe({
      next: (status) => {
        this.status.set(status);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: '載入失敗', detail: '找不到指定發布狀態。' });
      },
    });
  }

  edit(): void {
    const status = this.status();
    if (status) {
      this.router.navigate(['/publish-statuses', status.pkid, 'edit']);
    }
  }

  back(): void {
    this.router.navigate(['/publish-statuses']);
  }
}
