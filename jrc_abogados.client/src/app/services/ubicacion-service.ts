import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Ubicacion } from '../Models/Ubicacion';
import { API_BASE_URL } from '../configuracion';

@Injectable({
  providedIn: 'root'
})
export class UbicacionService {

  private baseUrl = `${API_BASE_URL}/Ubicacion`;

  constructor(private http: HttpClient) { }

  getUbicaciones(): Observable<Ubicacion[]> {
    return this.http.get<Ubicacion[]>(`${this.baseUrl}`);
  }

  getUbicacion(id: number): Observable<Ubicacion> {
    return this.http.get<Ubicacion>(`${this.baseUrl}/${id}`);
  }

  crearUbicacion(ubicacion: Ubicacion): Observable<Ubicacion> {
    return this.http.post<Ubicacion>(`${this.baseUrl}`, ubicacion);
  }

  actualizarUbicacion(id: number, ubicacion: Ubicacion): Observable<any> {
    return this.http.put(`${this.baseUrl}/${id}`, ubicacion);
  }

  eliminarUbicacion(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/${id}`);
  }
}
