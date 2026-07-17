import { Component, computed, inject, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { ToastModule } from 'primeng/toast';
import { AuthService } from '@core/auth/auth.service';
import { GLOBAL_TOAST_KEY } from '@core/auth/auth.interceptor';

interface NavItem {
  label: string;
  route?: string;
}

interface NavGroup {
  icon: string;
  label: string;
  expanded: boolean;
  items: NavItem[];
  /** When true, the group is only shown to users whose roles include "Admin". */
  adminOnly?: boolean;
}

const ADMIN_GROUP_LABEL = '系統管理 Admin';

@Component({
  selector: 'app-root',
  imports: [NgClass, RouterOutlet, RouterLink, RouterLinkActive, ToastModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** Key of the app-level toast that displays global (interceptor-raised) errors. */
  protected readonly globalToastKey = GLOBAL_TOAST_KEY;

  protected readonly collapsed = signal(false);

  /**
   * The nav shell renders only for a fully signed-in user. A user still on the default password is held
   * on the forced change-password page, which stands alone like the login page — showing the nav around
   * it would offer links the backend refuses anyway.
   */
  protected readonly showShell = computed(
    () => this.auth.isAuthenticated() && !this.auth.mustChangePassword(),
  );

  private readonly groups = signal<NavGroup[]>([
    {
      icon: 'pi pi-home',
      label: '首頁管理 Home',
      expanded: false,
      items: [{ label: '上稿作業 FeaturedPromoItem', route: '/featured-promo-items' }],
    },
    {
      icon: 'pi pi-folder',
      label: '課程管理 Course',
      expanded: false,
      items: [
        { label: '課程 Course', route: '/courses' },
        { label: '合作廠商 Partner', route: '/partners' },
        { label: '課程群組 CourseGroup', route: '/course-groups' },
      ],
    },
    { icon: 'pi pi-comments', label: '說明會 Seminar', expanded: false, items: [] },
    { icon: 'pi pi-megaphone', label: '活動管理 Promotion', expanded: false, items: [] },
    { icon: 'pi pi-file-edit', label: '線上報名 Forms', expanded: false, items: [] },
    { icon: 'pi pi-globe', label: '網站資訊 WebInfo', expanded: false, items: [] },
    { icon: 'pi pi-verified', label: '考試中心 TestingCenter', expanded: false, items: [] },
    {
      icon: 'pi pi-shield',
      label: ADMIN_GROUP_LABEL,
      expanded: true,
      adminOnly: true,
      items: [
        { label: '角色 AppRole', route: '/app-roles' },
        { label: '發布狀態 PublishStatus', route: '/publish-statuses' },
        { label: '使用者 AppUser', route: '/app-users' },
      ],
    },
  ]);

  /** Groups visible to the current user — the Admin group is hidden unless the token grants "Admin". */
  protected readonly visibleGroups = computed(() =>
    this.groups().filter((group) => !group.adminOnly || this.auth.isAdmin()),
  );

  protected toggleGroup(group: NavGroup): void {
    group.expanded = !group.expanded;
    this.groups.update((g) => [...g]);
  }

  protected toggleSidebar(): void {
    this.collapsed.update((c) => !c);
  }

  protected logout(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }
}
