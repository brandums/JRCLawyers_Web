import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Rol } from '../Models/Rol';
import { API_BASE_URL } from '../configuracion';

@Injectable({
  providedIn: 'root'
})
export class RolService {

  private baseUrl = `${API_BASE_URL}/Rol`;

  constructor(private http: HttpClient) { }

  getRoles(): Observable<Rol[]> {
    return this.http.get<Rol[]>(`${this.baseUrl}`);
  }
}
