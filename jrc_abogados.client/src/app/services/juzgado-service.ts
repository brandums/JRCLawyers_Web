import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Juzgado } from '../Models/Juzgado';
import { API_BASE_URL } from '../configuracion';

@Injectable({
  providedIn: 'root'
})
export class JuzgadoService {

  private baseUrl = `${API_BASE_URL}/Juzgado`;

  constructor(private http: HttpClient) { }

  getJuzgados(): Observable<Juzgado[]> {
    return this.http.get<Juzgado[]>(`${this.baseUrl}`);
  }

  getJuzgado(id: number): Observable<Juzgado> {
    return this.http.get<Juzgado>(`${this.baseUrl}/${id}`);
  }

  crearJuzgado(juzgado: Juzgado): Observable<Juzgado> {
    return this.http.post<Juzgado>(`${this.baseUrl}`, juzgado);
  }

  actualizarJuzgado(id: number, juzgado: Juzgado): Observable<any> {
    return this.http.put(`${this.baseUrl}/${id}`, juzgado);
  }

  eliminarJuzgado(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/${id}`);
  }
}
