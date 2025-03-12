import { Injectable } from '@angular/core';
import { Usuario } from '../Models/Usuario';
import { BehaviorSubject, Observable, map } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from '../configuracion';
import { InactivityService } from './InactivityService';
import { EventBusService } from './event-logout.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private usuarioSubject: BehaviorSubject<Usuario | null>;
  usuario: Observable<Usuario | null>;
  private baseUrl = `${API_BASE_URL}/Auth`;

  constructor(private http: HttpClient, private inactivityService: InactivityService, private eventBus: EventBusService) {
    this.checkAndClearStorage();

    const usuarioString = localStorage.getItem('usuario');
    const usuario = usuarioString ? JSON.parse(usuarioString) : null;
    this.usuarioSubject = new BehaviorSubject<Usuario | null>(usuario);
    this.usuario = this.usuarioSubject.asObservable();
    this.inactivityService.resetTimer();

    window.addEventListener('beforeunload', this.setSessionStorage);

    this.eventBus.logout$.subscribe(() => this.logout());
  }

  private checkAndClearStorage(): void {
    if (!sessionStorage.getItem('keepLoggedIn')) {
      localStorage.removeItem('usuario');
      localStorage.removeItem('token');
    }
    sessionStorage.setItem('keepLoggedIn', 'true');
  }

  private setSessionStorage(): void {
    sessionStorage.setItem('keepLoggedIn', 'true');
  }

  login(correoElectronico: string, contraseña: string, captchaResponse: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/login`, { correoElectronico, contraseña, captchaResponse })
      .pipe(map(response => {
        localStorage.setItem('usuario', JSON.stringify(response.usuario));
        localStorage.setItem('token', response.token);
        this.inactivityService.resetTimer();
        this.usuarioSubject.next(response.usuario);
        return response.usuario;
      }));
  }

  logout(): void {
    localStorage.removeItem('usuario');
    localStorage.removeItem('token');
    this.usuarioSubject.next(null);
    sessionStorage.removeItem('keepLoggedIn');
  }

  getUser(): Usuario | null {
    return this.usuarioSubject.value;
  }

  isLoggedIn(): boolean {
    return this.usuarioSubject.value !== null;
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }
}
