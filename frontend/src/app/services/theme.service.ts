import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private isDarkModeSubject = new BehaviorSubject<boolean>(false);
  isDarkMode$ = this.isDarkModeSubject.asObservable();

  constructor(@Inject(PLATFORM_ID) private platformId: Object) {
    if (isPlatformBrowser(this.platformId)) {
      const savedTheme = localStorage.getItem('darkMode');
      const isDark = savedTheme !== null ? savedTheme === 'true' : false;
      this.isDarkModeSubject.next(isDark);
      
      if (isDark) {
        document.body.classList.add('dark-mode');
      }
    }
  }

  get isDarkMode(): boolean {
    return this.isDarkModeSubject.value;
  }

  toggleDarkMode(): void {
    if (isPlatformBrowser(this.platformId)) {
      const newState = !this.isDarkMode;
      this.isDarkModeSubject.next(newState);
      
      if (newState) {
        document.body.classList.add('dark-mode');
      } else {
        document.body.classList.remove('dark-mode');
      }
      
      localStorage.setItem('darkMode', String(newState));
    }
  }
}
