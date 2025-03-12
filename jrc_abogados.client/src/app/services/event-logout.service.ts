import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class EventBusService {
  private logoutSubject = new Subject<void>();
  logout$ = this.logoutSubject.asObservable();

  emitLogout() {
    this.logoutSubject.next();
  }
}
