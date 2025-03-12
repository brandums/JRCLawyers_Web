import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { AuthService } from './services/AuthService';
import { Router } from '@angular/router';
import { AlertService } from './services/AlertService';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  user: any;

  constructor(
    private authService: AuthService,
    private cdr: ChangeDetectorRef,
    private router: Router,
    private alertService: AlertService
    )
  { }

  ngOnInit(): void {
    this.authService.usuario.subscribe(usuario => {
      if (!usuario) {
        this.user = null;
        /*this.router.navigate(['/login']);*/
      }
      else {
        this.user = usuario;
      }
      this.cdr.detectChanges();
    });

    this.alertService.showModal$.subscribe(() => {
      this.openModal();
    });

    this.alertService.message$.subscribe(message => {
      this.setMessage(message);
    });
  }

  openModal() {
    const btnOpen = document.getElementById('abrir-modal');
    btnOpen?.click();

    setTimeout(() => {
      const btnClose = document.getElementById('btn-close-modal');
      btnClose?.click();
    }, 2000);
  }

  setMessage(message: string) {
    const messageElement = document.getElementById('message-text');
    if (messageElement) {
      messageElement.textContent = message;
    }
  }

  title = 'jrc_abogados.client';
}
