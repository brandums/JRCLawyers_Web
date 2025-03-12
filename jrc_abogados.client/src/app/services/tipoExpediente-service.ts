import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../configuracion';
import { TipoExpediente } from '../Models/TipoExpediente';

@Injectable({
  providedIn: 'root'
})
export class TipoExpedienteService {

  private baseUrl = `${API_BASE_URL}/TipoExpediente`;

  constructor(private http: HttpClient) { }

  getTipoDeExpediente(): Observable<TipoExpediente[]> {
    return this.http.get<TipoExpediente[]>(`${this.baseUrl}`);
  }
}
