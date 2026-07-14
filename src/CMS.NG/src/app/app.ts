import { Component, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';

interface NavItem {
  label: string;
  route?: string;
}

interface NavGroup {
  icon: string;
  label: string;
  expanded: boolean;
  items: NavItem[];
}

@Component({
  selector: 'app-root',
  imports: [NgClass, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly collapsed = signal(false);

  protected readonly groups = signal<NavGroup[]>([
    { icon: 'pi pi-home', label: '首頁管理 Home', expanded: false, items: [] },
    { icon: 'pi pi-folder', label: '課程管理 Course', expanded: false, items: [] },
    { icon: 'pi pi-comments', label: '說明會 Seminar', expanded: false, items: [] },
    { icon: 'pi pi-megaphone', label: '活動管理 Promotion', expanded: false, items: [] },
    { icon: 'pi pi-file-edit', label: '線上報名 Forms', expanded: false, items: [] },
    { icon: 'pi pi-globe', label: '網站資訊 WebInfo', expanded: false, items: [] },
    { icon: 'pi pi-verified', label: '考試中心 TestingCenter', expanded: false, items: [] },
    {
      icon: 'pi pi-shield',
      label: '系統管理 Admin',
      expanded: true,
      items: [
        { label: '角色 AppRole', route: '/app-roles' },
        { label: '使用者 AppUser' },
      ],
    },
  ]);

  protected toggleGroup(group: NavGroup): void {
    group.expanded = !group.expanded;
    this.groups.update((g) => [...g]);
  }

  protected toggleSidebar(): void {
    this.collapsed.update((c) => !c);
  }
}
