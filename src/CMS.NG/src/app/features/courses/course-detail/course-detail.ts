import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { formatDate } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

import { Course } from '@core/models/course.model';
import { CourseService } from '@core/services/course.service';
import { LookupService } from '@core/services/lookup.service';
import { QrCode } from '@app/shared/qr-code/qr-code';
import { RowAuditBadge } from '@app/shared/row-audit-badge/row-audit-badge';

/** Public site base used for the course QR-code deep link. */
const COURSE_SITE_BASE = 'https://www.uuu.com.tw/Course/Show';

@Component({
  selector: 'app-course-detail',
  imports: [ButtonModule, TagModule, ToastModule, QrCode, RowAuditBadge],
  templateUrl: './course-detail.html',
  styleUrl: './course-detail.scss',
})
export class CourseDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(CourseService);
  private readonly lookups = inject(LookupService);
  private readonly messageService = inject(MessageService);

  readonly course = signal<Course | null>(null);
  readonly loading = signal(true);
  readonly certificationLabels = signal<string[]>([]);
  readonly jobCategoryLabels = signal<string[]>([]);

  /** Public-site deep link encoded in the QR code: `${base}/{pkid}/{courseId}`. */
  readonly qrUrl = computed(() => {
    const c = this.course();
    return c ? `${COURSE_SITE_BASE}/${c.pkid}/${c.courseId}` : '';
  });

  /** Stamp shown in the print-only footer band (matches the UI's yyyy-MM-dd style). */
  readonly printDate = formatDate(new Date(), 'yyyy-MM-dd', 'en-US');

  /** Open the browser print dialog; the user chooses "Save as PDF" as the destination. */
  print(): void {
    window.print();
  }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam === null) {
      this.router.navigate(['/courses']);
      return;
    }

    forkJoin({
      course: this.service.getById(Number(idParam)),
      certifications: this.lookups.getCertifications(),
      jobCategories: this.lookups.getJobCategories(),
    }).subscribe({
      next: ({ course, certifications, jobCategories }) => {
        this.course.set(course);
        const certById = new Map(certifications.map((c) => [c.pkid, c.title ?? `#${c.pkid}`]));
        const jobById = new Map(jobCategories.map((j) => [j.pkid, j.description]));
        this.certificationLabels.set(course.certificationPkids.map((id) => certById.get(id) ?? `#${id}`));
        this.jobCategoryLabels.set(course.jobCategoryPkids.map((id) => jobById.get(id) ?? `#${id}`));
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: '載入失敗', detail: '找不到指定課程。' });
      },
    });
  }

  edit(): void {
    const course = this.course();
    if (course) {
      this.router.navigate(['/courses', course.pkid, 'edit']);
    }
  }

  back(): void {
    this.router.navigate(['/courses']);
  }

  // Foreign-Primary navigation — jump to each parent's detail page.
  viewPartner(): void {
    const course = this.course();
    if (course) {
      this.router.navigate(['/partners', course.partnerPkid]);
    }
  }

  viewCourseGroup(): void {
    const course = this.course();
    if (course?.courseGroupPkid != null) {
      this.router.navigate(['/course-groups', course.courseGroupPkid]);
    }
  }

  viewPublishStatus(): void {
    const course = this.course();
    if (course) {
      this.router.navigate(['/publish-statuses', course.publishStatusPkid]);
    }
  }

  // Primary-Foreign navigation — child lists pre-filtered by this course (features pending).
  viewFaqs(): void {
    const course = this.course();
    if (course) {
      this.router.navigate(['/course-faqs'], { queryParams: { coursePkid: course.pkid } });
    }
  }

  viewRelatedLinks(): void {
    const course = this.course();
    if (course) {
      this.router.navigate(['/course-related-links'], { queryParams: { coursePkid: course.pkid } });
    }
  }

  viewHotCourses(): void {
    const course = this.course();
    if (course) {
      this.router.navigate(['/hot-courses'], { queryParams: { coursePkid: course.pkid } });
    }
  }
}
