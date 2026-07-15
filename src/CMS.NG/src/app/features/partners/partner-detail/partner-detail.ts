import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

import { Partner } from '@core/models/partner.model';
import { PartnerService } from '@core/services/partner.service';

@Component({
  selector: 'app-partner-detail',
  imports: [ButtonModule, ToastModule],
  templateUrl: './partner-detail.html',
  styleUrl: './partner-detail.scss',
})
export class PartnerDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(PartnerService);
  private readonly messageService = inject(MessageService);

  readonly partner = signal<Partner | null>(null);
  readonly loading = signal(true);

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam === null) {
      this.router.navigate(['/partners']);
      return;
    }

    this.service.getById(Number(idParam)).subscribe({
      next: (partner) => {
        this.partner.set(partner);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: '載入失敗', detail: '找不到指定合作廠商。' });
      },
    });
  }

  edit(): void {
    const partner = this.partner();
    if (partner) {
      this.router.navigate(['/partners', partner.pkid, 'edit']);
    }
  }

  back(): void {
    this.router.navigate(['/partners']);
  }

  // Primary-Foreign navigation — child lists pre-filtered by this partner.
  viewCourses(): void {
    const partner = this.partner();
    if (partner) {
      this.router.navigate(['/courses'], { queryParams: { partnerPkid: partner.pkid } });
    }
  }

  viewCertifications(): void {
    const partner = this.partner();
    if (partner) {
      this.router.navigate(['/certifications'], { queryParams: { partnerPkid: partner.pkid } });
    }
  }

  viewCourseGroups(): void {
    const partner = this.partner();
    if (partner) {
      this.router.navigate(['/partner-course-groups'], { queryParams: { partnerPkid: partner.pkid } });
    }
  }
}
