import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Estado } from '../Models/Estado';
import { API_BASE_URL } from '../configuracion';

@Injectable({
  providedIn: 'root'
})
export class EstadoService {

  private baseUrl = `${API_BASE_URL}/Estado`;

  constructor(private http: HttpClient) { }

  getEstados(): Observable<Estado[]> {
    return this.http.get<Estado[]>(`${this.baseUrl}`);
  }
}

