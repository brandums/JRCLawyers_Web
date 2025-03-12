import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../configuracion';
import { TipoDocumento } from '../Models/TipoDocumento';

@Injectable({
  providedIn: 'root'
})
export class TipoDocumentoService {

  private baseUrl = `${API_BASE_URL}/TipoDocumento`;

  constructor(private http: HttpClient) { }

  getTipoDeDocumento(): Observable<TipoDocumento[]> {
    return this.http.get<TipoDocumento[]>(`${this.baseUrl}`);
  }
}
