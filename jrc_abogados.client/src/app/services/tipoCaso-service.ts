import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TipoCaso } from '../Models/TipoCaso';
import { API_BASE_URL } from '../configuracion';

@Injectable({
  providedIn: 'root'
})
export class TipoCasoService {

  private baseUrl = `${API_BASE_URL}/TipoCaso`;

  constructor(private http: HttpClient) { }

  getTipoDeCasos(): Observable<TipoCaso[]> {
    return this.http.get<TipoCaso[]>(`${this.baseUrl}`);
  }
}
