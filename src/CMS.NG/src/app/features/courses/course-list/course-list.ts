import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, switchMap } from 'rxjs';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { CheckboxModule } from 'primeng/checkbox';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService, ConfirmationService } from 'primeng/api';

import { Course, CourseQuery, CourseRequest } from '@core/models/course.model';
import { CourseService } from '@core/services/course.service';
import { LookupService } from '@core/services/lookup.service';
import { toIsoDate, parseIsoDate } from '@core/utils/date.util';
import { Autofocus } from '@app/shared/autofocus';

const FILTERS_KEY = 'course-list-filters';
const SORT_KEY = 'course-list-sort';
const PAGE_KEY = 'course-list-page';

/** Columns the user may edit inline; the other visible columns (pkid, partnerName, courseGroupDescription) are read-only. */
type EditableField =
  | 'displayOrder'
  | 'courseId'
  | 'prodCourseId'
  | 'title'
  | 'publishStatusPkid'
  | 'scheduleOn'
  | 'scheduleOff'
  | 'hour'
  | 'listPrice'
  | 'learningCredit'
  | 'canRepeat';

/** Read-only list columns — primary key and the two FK-lookup labels. */
const READONLY_COLUMNS = ['pkid', 'partnerName', 'courseGroupDescription'];

interface Option {
  pkid: number;
  label: string;
}

interface TriStateOption {
  label: string;
  value: boolean | null;
}

function emptyFilter(): CourseQuery {
  return {
    keyword: null,
    partnerPkid: null,
    courseGroupPkid: null,
    publishStatusPkid: null,
    scheduleOnFrom: null,
    scheduleOnTo: null,
    scheduleOffFrom: null,
    scheduleOffTo: null,
    canRepeat: null,
  };
}

@Component({
  selector: 'app-course-list',
  imports: [
    FormsModule,
    TableModule,
    ButtonModule,
    DrawerModule,
    InputTextModule,
    InputNumberModule,
    SelectModule,
    DatePickerModule,
    CheckboxModule,
    ToastModule,
    ConfirmDialogModule,
    TooltipModule,
    Autofocus,
  ],
  templateUrl: './course-list.html',
  styleUrl: './course-list.scss',
})
export class CourseList implements OnInit {
  private readonly service = inject(CourseService);
  private readonly lookups = inject(LookupService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  readonly courses = signal<Course[]>([]);
  readonly loading = signal(false);
  readonly filterVisible = signal(false);

  readonly partnerOptions = signal<Option[]>([]);
  readonly courseGroupOptions = signal<Option[]>([]);
  readonly publishStatusOptions = signal<Option[]>([]);

  readonly triStateOptions: TriStateOption[] = [
    { label: '全部', value: null },
    { label: '是', value: true },
    { label: '否', value: false },
  ];

  filter: CourseQuery = emptyFilter();

  // p-datepicker binds Date objects; the filter carries ISO strings.
  scheduleOnFromDate: Date | null = null;
  scheduleOnToDate: Date | null = null;
  scheduleOffFromDate: Date | null = null;
  scheduleOffToDate: Date | null = null;

  sortField = 'displayOrder';
  sortOrder = 1;
  first = 0;
  rows = 20;

  // ---- Inline cell editing ----
  /** The cell currently in edit mode, or null. */
  readonly editing = signal<{ pkid: number; field: EditableField } | null>(null);
  /** Inline validation error for the open cell. */
  readonly editError = signal<string | null>(null);
  /** True while a blur-triggered save is in flight. */
  readonly savingCell = signal(false);
  /** Working value two-way-bound to the open editor (type varies by column). */
  editValue: any = null;

  ngOnInit(): void {
    this.restoreState();

    forkJoin({
      partners: this.lookups.getPartners(),
      courseGroups: this.lookups.getCourseGroups(),
      publishStatuses: this.lookups.getPublishStatuses(),
    }).subscribe({
      next: ({ partners, courseGroups, publishStatuses }) => {
        this.partnerOptions.set(partners.map((p) => ({ pkid: p.pkid, label: p.name })));
        this.courseGroupOptions.set(courseGroups.map((g) => ({ pkid: g.pkid, label: g.description })));
        this.publishStatusOptions.set(publishStatuses.map((s) => ({ pkid: s.pkid, label: s.description })));
        this.applyIncomingParams();
        this.load();
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: '載入失敗', detail: '無法取得下拉選單資料。' });
        this.load();
      },
    });
  }

  private restoreState(): void {
    const savedFilters = sessionStorage.getItem(FILTERS_KEY);
    if (savedFilters) {
      this.filter = { ...this.filter, ...JSON.parse(savedFilters) };
      this.syncDatePickers();
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

  /** Cross-entity navigation (e.g. Partner《查看課程》) pre-filters the list. */
  private applyIncomingParams(): void {
    const qp = this.route.snapshot.queryParamMap;
    const partnerPkid = qp.get('partnerPkid');
    const courseGroupPkid = qp.get('courseGroupPkid');
    const publishStatusPkid = qp.get('publishStatusPkid');
    if (partnerPkid !== null) this.filter.partnerPkid = Number(partnerPkid);
    if (courseGroupPkid !== null) this.filter.courseGroupPkid = Number(courseGroupPkid);
    if (publishStatusPkid !== null) this.filter.publishStatusPkid = Number(publishStatusPkid);
  }

  private syncDatePickers(): void {
    this.scheduleOnFromDate = parseIsoDate(this.filter.scheduleOnFrom);
    this.scheduleOnToDate = parseIsoDate(this.filter.scheduleOnTo);
    this.scheduleOffFromDate = parseIsoDate(this.filter.scheduleOffFrom);
    this.scheduleOffToDate = parseIsoDate(this.filter.scheduleOffTo);
  }

  load(): void {
    this.loading.set(true);
    this.cancelEdit();
    this.service.query(this.filter).subscribe({
      next: (data) => {
        this.courses.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: '載入失敗', detail: '無法取得課程資料。' });
      },
    });
  }

  applyFilter(): void {
    this.filter.scheduleOnFrom = toIsoDate(this.scheduleOnFromDate);
    this.filter.scheduleOnTo = toIsoDate(this.scheduleOnToDate);
    this.filter.scheduleOffFrom = toIsoDate(this.scheduleOffFromDate);
    this.filter.scheduleOffTo = toIsoDate(this.scheduleOffToDate);

    sessionStorage.setItem(FILTERS_KEY, JSON.stringify(this.filter));
    this.first = 0;
    this.persistPage();
    this.filterVisible.set(false);
    this.load();
  }

  clearFilter(): void {
    this.filter = emptyFilter();
    this.scheduleOnFromDate = null;
    this.scheduleOnToDate = null;
    this.scheduleOffFromDate = null;
    this.scheduleOffToDate = null;
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
    this.router.navigate(['/courses/new']);
  }

  view(course: Course): void {
    this.router.navigate(['/courses', course.pkid]);
  }

  edit(course: Course): void {
    this.router.navigate(['/courses', course.pkid, 'edit']);
  }

  confirmDelete(course: Course): void {
    this.confirmationService.confirm({
      header: '刪除確認',
      message: `確定要刪除主代碼 <b>${course.pkid}</b>「${course.title}」？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '刪除',
      rejectLabel: '取消',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.delete(course),
    });
  }

  private delete(course: Course): void {
    this.service.delete(course.pkid).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: '已刪除', detail: `課程「${course.title}」已刪除。` });
        this.load();
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: '刪除失敗', detail: '無法刪除課程，可能仍有關聯資料。' });
      },
    });
  }

  // ---------- Inline cell editing ----------

  /** True for the three read-only columns (primary key + FK labels). */
  isReadOnly(field: string): boolean {
    return READONLY_COLUMNS.includes(field);
  }

  isEditing(course: Course, field: EditableField): boolean {
    const e = this.editing();
    return e !== null && e.pkid === course.pkid && e.field === field;
  }

  /** Double-click handler — opens the editor for an editable cell (read-only columns are ignored). */
  startEdit(course: Course, field: string): void {
    if (this.isReadOnly(field) || this.savingCell()) {
      return;
    }
    const editable = field as EditableField;
    this.editError.set(null);
    this.editValue = this.initialValue(course, editable);
    this.editing.set({ pkid: course.pkid, field: editable });
  }

  private initialValue(course: Course, field: EditableField): any {
    switch (field) {
      case 'scheduleOn':
        return parseIsoDate(course.scheduleOn);
      case 'scheduleOff':
        return parseIsoDate(course.scheduleOff);
      default:
        return (course as unknown as Record<string, unknown>)[field];
    }
  }

  cancelEdit(): void {
    this.editing.set(null);
    this.editError.set(null);
    this.editValue = null;
  }

  /**
   * Commit the open cell (blur for text/number/date, change for select/checkbox).
   * Validates first — on failure shows an inline error and stays in edit mode; on a valid value
   * persists via the update endpoint, preserving the row's N-N lists (fetched with getById).
   */
  commitEdit(course: Course, field: EditableField): void {
    // Blur can fire spuriously; only act on the cell that is actually open.
    if (!this.isEditing(course, field) || this.savingCell()) {
      return;
    }

    const error = this.validate(field, this.editValue, course);
    if (error) {
      this.editError.set(error);
      return; // keep the cell in edit mode so the user can fix it
    }

    this.savingCell.set(true);
    // The list row lacks the N-N pkid lists; fetch the full record so update() does not wipe them.
    this.service
      .getById(course.pkid)
      .pipe(switchMap((full) => this.service.update(this.buildRequest(full, field, this.editValue))))
      .subscribe({
        next: (updated) => {
          this.courses.update((list) => list.map((c) => (c.pkid === updated.pkid ? updated : c)));
          this.savingCell.set(false);
          this.cancelEdit();
          this.messageService.add({ severity: 'success', summary: '已更新', detail: '欄位已儲存。' });
        },
        error: () => {
          // Save failed → revert (the row model was never mutated) and surface the error.
          this.savingCell.set(false);
          this.cancelEdit();
          this.messageService.add({ severity: 'error', summary: '儲存失敗', detail: '欄位更新失敗，已還原為原值。' });
        },
      });
  }

  /** Returns an error message when the edited value is invalid, or null when it is acceptable. */
  validate(field: EditableField, value: any, course: Course): string | null {
    switch (field) {
      case 'title':
      case 'courseId':
      case 'prodCourseId': {
        const text = typeof value === 'string' ? value.trim() : '';
        return text.length === 0 ? '此欄位為必填，不可清空。' : null;
      }
      case 'hour':
      case 'listPrice':
      case 'learningCredit': {
        const n = typeof value === 'number' ? value : Number(value);
        if (value === null || value === undefined || value === '' || Number.isNaN(n)) {
          return '請輸入有效的數字。';
        }
        return n < 0 ? '數值不可為負數。' : null;
      }
      case 'displayOrder': {
        const n = typeof value === 'number' ? value : Number(value);
        return value === null || value === undefined || value === '' || Number.isNaN(n)
          ? '請輸入有效的數字。'
          : null;
      }
      case 'publishStatusPkid':
        return value === null || value === undefined ? '請選擇上架狀態。' : null;
      case 'scheduleOn':
      case 'scheduleOff':
        return this.validateDate(field, value, course);
      case 'canRepeat':
        return null;
      default:
        return null;
    }
  }

  private validateDate(field: 'scheduleOn' | 'scheduleOff', value: any, course: Course): string | null {
    if (!(value instanceof Date) || Number.isNaN(value.getTime())) {
      return '請輸入有效的日期。';
    }
    const otherIso = field === 'scheduleOn' ? course.scheduleOff : course.scheduleOn;
    const other = parseIsoDate(otherIso);
    if (other) {
      const on = field === 'scheduleOn' ? value : other;
      const off = field === 'scheduleOn' ? other : value;
      if (on.getTime() > off.getTime()) {
        return '上架日期不可晚於下架日期。';
      }
    }
    return null;
  }

  /** Build a full update request from the fetched course, overriding only the edited field. */
  private buildRequest(full: Course, field: EditableField, value: any): CourseRequest {
    const request: CourseRequest = {
      pkid: full.pkid,
      title: full.title,
      officialTitle: full.officialTitle,
      courseId: full.courseId,
      prodCourseId: full.prodCourseId,
      friendlyUrl: full.friendlyUrl,
      displayOrder: full.displayOrder,
      partnerPkid: full.partnerPkid,
      courseGroupPkid: full.courseGroupPkid,
      publishStatusPkid: full.publishStatusPkid,
      scheduleOn: full.scheduleOn,
      scheduleOff: full.scheduleOff,
      hour: full.hour,
      listPrice: full.listPrice,
      learningCredit: full.learningCredit,
      material: full.material,
      objective: full.objective,
      target: full.target,
      prerequisites: full.prerequisites,
      outline: full.outline,
      towardCertOrExam: full.towardCertOrExam,
      note: full.note,
      otherInfo: full.otherInfo,
      canRepeat: full.canRepeat,
      certificationPkids: full.certificationPkids,
      jobCategoryPkids: full.jobCategoryPkids,
    };

    switch (field) {
      case 'title':
        request.title = (value as string).trim();
        break;
      case 'courseId':
        request.courseId = (value as string).trim();
        break;
      case 'prodCourseId':
        request.prodCourseId = (value as string).trim();
        break;
      case 'displayOrder':
        request.displayOrder = Number(value);
        break;
      case 'hour':
        request.hour = Number(value);
        break;
      case 'listPrice':
        request.listPrice = Number(value);
        break;
      case 'learningCredit':
        request.learningCredit = Number(value);
        break;
      case 'publishStatusPkid':
        request.publishStatusPkid = Number(value);
        break;
      case 'canRepeat':
        request.canRepeat = Boolean(value);
        break;
      case 'scheduleOn':
        request.scheduleOn = toIsoDate(value as Date)!;
        break;
      case 'scheduleOff':
        request.scheduleOff = toIsoDate(value as Date)!;
        break;
    }
    return request;
  }
}
