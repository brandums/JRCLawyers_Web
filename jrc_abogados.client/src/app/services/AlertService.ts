import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AlertService {
  private messageSource = new Subject<string>();
  message$ = this.messageSource.asObservable();

  private showModalSource = new Subject<void>();
  showModal$ = this.showModalSource.asObservable();

  showMessage(message: string) {
    this.messageSource.next(message);
    this.showModalSource.next();
  }
}
