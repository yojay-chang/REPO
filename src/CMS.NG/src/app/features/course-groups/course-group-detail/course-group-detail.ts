import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

import { CourseGroup } from '@core/models/course-group.model';
import { CourseGroupService } from '@core/services/course-group.service';
import { RowAuditBadge } from '@app/shared/row-audit-badge/row-audit-badge';

@Component({
  selector: 'app-course-group-detail',
  imports: [ButtonModule, ToastModule, RowAuditBadge],
  templateUrl: './course-group-detail.html',
  styleUrl: './course-group-detail.scss',
})
export class CourseGroupDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(CourseGroupService);
  private readonly messageService = inject(MessageService);

  readonly courseGroup = signal<CourseGroup | null>(null);
  readonly loading = signal(true);

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam === null) {
      this.router.navigate(['/course-groups']);
      return;
    }

    this.service.getById(Number(idParam)).subscribe({
      next: (courseGroup) => {
        this.courseGroup.set(courseGroup);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: '載入失敗', detail: '找不到指定課程群組。' });
      },
    });
  }

  edit(): void {
    const courseGroup = this.courseGroup();
    if (courseGroup) {
      this.router.navigate(['/course-groups', courseGroup.pkid, 'edit']);
    }
  }

  back(): void {
    this.router.navigate(['/course-groups']);
  }

  // Primary-Foreign navigation — child lists pre-filtered by this course group.
  viewCourses(): void {
    const courseGroup = this.courseGroup();
    if (courseGroup) {
      this.router.navigate(['/courses'], { queryParams: { courseGroupPkid: courseGroup.pkid } });
    }
  }

  viewPartnerCourseGroups(): void {
    const courseGroup = this.courseGroup();
    if (courseGroup) {
      this.router.navigate(['/partner-course-groups'], { queryParams: { courseGroupPkid: courseGroup.pkid } });
    }
  }
}
