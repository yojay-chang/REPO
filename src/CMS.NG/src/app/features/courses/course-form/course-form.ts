import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { MultiSelectModule } from 'primeng/multiselect';
import { DatePickerModule } from 'primeng/datepicker';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

import { CourseRequest } from '@core/models/course.model';
import { CourseService } from '@core/services/course.service';
import { LookupService } from '@core/services/lookup.service';
import { toIsoDate, parseIsoDate } from '@core/utils/date.util';

interface Option {
  pkid: number;
  label: string;
}

@Component({
  selector: 'app-course-form',
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    TextareaModule,
    SelectModule,
    MultiSelectModule,
    DatePickerModule,
    ToggleSwitchModule,
    ToastModule,
  ],
  templateUrl: './course-form.html',
  styleUrl: './course-form.scss',
})
export class CourseForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(CourseService);
  private readonly lookups = inject(LookupService);
  private readonly messageService = inject(MessageService);

  readonly isEdit = signal(false);
  readonly loading = signal(true);
  readonly saving = signal(false);

  // pkid is a system-assigned IDENTITY — displayed read-only in edit mode, never entered.
  readonly pkidDisplay = signal<number | null>(null);

  readonly partnerOptions = signal<Option[]>([]);
  readonly courseGroupOptions = signal<Option[]>([]);
  readonly publishStatusOptions = signal<Option[]>([]);
  readonly certificationOptions = signal<Option[]>([]);
  readonly jobCategoryOptions = signal<Option[]>([]);

  private pkid: number | null = null;

  readonly form = this.fb.group({
    title: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(200)]),
    officialTitle: this.fb.control<string | null>(null, [Validators.maxLength(300)]),
    courseId: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(50)]),
    prodCourseId: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(50)]),
    friendlyUrl: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(100)]),
    displayOrder: this.fb.nonNullable.control(0, [Validators.required]),
    partnerPkid: this.fb.control<number | null>(null, [Validators.required]),
    courseGroupPkid: this.fb.control<number | null>(null),
    publishStatusPkid: this.fb.control<number | null>(null, [Validators.required]),
    scheduleOn: this.fb.control<Date | null>(null, [Validators.required]),
    scheduleOff: this.fb.control<Date | null>(null, [Validators.required]),
    hour: this.fb.nonNullable.control(0, [Validators.required]),
    listPrice: this.fb.nonNullable.control(0, [Validators.required]),
    learningCredit: this.fb.nonNullable.control(0, [Validators.required]),
    material: this.fb.control<string | null>(null, [Validators.maxLength(500)]),
    objective: this.fb.control<string | null>(null, [Validators.maxLength(4000)]),
    target: this.fb.control<string | null>(null, [Validators.maxLength(500)]),
    prerequisites: this.fb.control<string | null>(null, [Validators.maxLength(4000)]),
    outline: this.fb.control<string | null>(null),
    towardCertOrExam: this.fb.control<string | null>(null),
    note: this.fb.control<string | null>(null, [Validators.maxLength(4000)]),
    otherInfo: this.fb.control<string | null>(null, [Validators.maxLength(4000)]),
    canRepeat: this.fb.nonNullable.control(false),
    certificationPkids: this.fb.nonNullable.control<number[]>([]),
    jobCategoryPkids: this.fb.nonNullable.control<number[]>([]),
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.pkid = idParam === null ? null : Number(idParam);
    this.isEdit.set(this.pkid !== null);

    const course$ = this.pkid === null ? of(null) : this.service.getById(this.pkid);

    forkJoin({
      course: course$,
      partners: this.lookups.getPartners(),
      courseGroups: this.lookups.getCourseGroups(),
      publishStatuses: this.lookups.getPublishStatuses(),
      certifications: this.lookups.getCertifications(),
      jobCategories: this.lookups.getJobCategories(),
    }).subscribe({
      next: ({ course, partners, courseGroups, publishStatuses, certifications, jobCategories }) => {
        this.partnerOptions.set(partners.map((p) => ({ pkid: p.pkid, label: p.name })));
        this.courseGroupOptions.set(courseGroups.map((g) => ({ pkid: g.pkid, label: g.description })));
        this.publishStatusOptions.set(publishStatuses.map((s) => ({ pkid: s.pkid, label: s.description })));
        this.certificationOptions.set(certifications.map((c) => ({ pkid: c.pkid, label: c.title ?? `#${c.pkid}` })));
        this.jobCategoryOptions.set(jobCategories.map((j) => ({ pkid: j.pkid, label: j.description })));

        if (course) {
          this.pkidDisplay.set(course.pkid);
          this.form.patchValue({
            title: course.title,
            officialTitle: course.officialTitle,
            courseId: course.courseId,
            prodCourseId: course.prodCourseId,
            friendlyUrl: course.friendlyUrl,
            displayOrder: course.displayOrder,
            partnerPkid: course.partnerPkid,
            courseGroupPkid: course.courseGroupPkid,
            publishStatusPkid: course.publishStatusPkid,
            scheduleOn: parseIsoDate(course.scheduleOn),
            scheduleOff: parseIsoDate(course.scheduleOff),
            hour: course.hour,
            listPrice: course.listPrice,
            learningCredit: course.learningCredit,
            material: course.material,
            objective: course.objective,
            target: course.target,
            prerequisites: course.prerequisites,
            outline: course.outline,
            towardCertOrExam: course.towardCertOrExam,
            note: course.note,
            otherInfo: course.otherInfo,
            canRepeat: course.canRepeat,
            certificationPkids: course.certificationPkids,
            jobCategoryPkids: course.jobCategoryPkids,
          });
        }
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: '載入失敗', detail: '無法載入資料。' });
      },
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.messageService.add({ severity: 'warn', summary: '欄位未完成', detail: '請確認必填欄位。' });
      return;
    }

    const raw = this.form.getRawValue();
    const trimOrNull = (v: string | null) => (v?.trim() ? v.trim() : null);

    const request: CourseRequest = {
      pkid: this.pkid ?? 0, // IDENTITY: ignored on create, matched on update.
      title: raw.title,
      officialTitle: trimOrNull(raw.officialTitle),
      courseId: raw.courseId,
      prodCourseId: raw.prodCourseId,
      friendlyUrl: raw.friendlyUrl,
      displayOrder: raw.displayOrder,
      partnerPkid: raw.partnerPkid!,
      courseGroupPkid: raw.courseGroupPkid ?? null,
      publishStatusPkid: raw.publishStatusPkid!,
      scheduleOn: toIsoDate(raw.scheduleOn)!,
      scheduleOff: toIsoDate(raw.scheduleOff)!,
      hour: raw.hour,
      listPrice: raw.listPrice,
      learningCredit: raw.learningCredit,
      material: trimOrNull(raw.material),
      objective: trimOrNull(raw.objective),
      target: trimOrNull(raw.target),
      prerequisites: trimOrNull(raw.prerequisites),
      outline: trimOrNull(raw.outline),
      towardCertOrExam: trimOrNull(raw.towardCertOrExam),
      note: trimOrNull(raw.note),
      otherInfo: trimOrNull(raw.otherInfo),
      canRepeat: raw.canRepeat,
      certificationPkids: raw.certificationPkids,
      jobCategoryPkids: raw.jobCategoryPkids,
    };

    this.saving.set(true);
    const op$ = this.isEdit() ? this.service.update(request) : this.service.create(request);

    op$.subscribe({
      next: (saved) => {
        this.saving.set(false);
        this.messageService.add({ severity: 'success', summary: '已儲存', detail: `課程「${saved.title}」已儲存。` });
        this.router.navigate(['/courses', saved.pkid]);
      },
      error: () => {
        this.saving.set(false);
        this.messageService.add({ severity: 'error', summary: '儲存失敗', detail: '儲存失敗，請稍後再試。' });
      },
    });
  }

  cancel(): void {
    if (this.isEdit() && this.pkid !== null) {
      this.router.navigate(['/courses', this.pkid]);
    } else {
      this.router.navigate(['/courses']);
    }
  }
}
