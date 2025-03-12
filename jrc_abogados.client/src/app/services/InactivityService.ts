import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { EventBusService } from './event-logout.service';

@Injectable({
  providedIn: 'root'
})
export class InactivityService {
  private timeoutId: any;
  private readonly inactivityLimit = 10 * 60 * 1000; // 10 minutos en milisegundos

  constructor(private router: Router, private eventBus: EventBusService) {
    this.resetTimer();
    this.setupActivityListeners();
  }

  private setupActivityListeners(): void {
    document.addEventListener('mousemove', () => this.resetTimer());
    document.addEventListener('keydown', () => this.resetTimer());
    document.addEventListener('click', () => this.resetTimer());
    document.addEventListener('scroll', () => this.resetTimer());
  }

  public resetTimer(): void {
    if (this.timeoutId) {
      clearTimeout(this.timeoutId);
    }

    this.timeoutId = setTimeout(() => this.logout(), this.inactivityLimit);
    localStorage.setItem('lastActivity', Date.now().toString());
  }

  private logout(): void {
    this.eventBus.emitLogout();
    this.router.navigate(['/login']);
  }

  public resetInactivityTimerExternally(): void {
    this.resetTimer();
  }
}
