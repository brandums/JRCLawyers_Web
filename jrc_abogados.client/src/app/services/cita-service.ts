import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, map } from 'rxjs';
import { Cita } from '../Models/Cita';
import { API_BASE_URL } from '../configuracion';

@Injectable({
  providedIn: 'root'
})
export class CitaService {

  private baseUrl = `${API_BASE_URL}/Cita`;
  public $listaCitas: BehaviorSubject<Cita[]> = new BehaviorSubject<Cita[]>([]);
  public $citaId: BehaviorSubject<number> = new BehaviorSubject<number>(0);

  constructor(private http: HttpClient) { }

  getCitas(): Observable<Cita[]> {
    return this.http.get<Cita[]>(`${this.baseUrl}`).pipe(
      map((citas: Cita[]) => {
        return citas.map(cita => {
          if (cita.fechaInicio) {
            cita.fechaInicio = new Date(cita.fechaInicio).toISOString().split('T')[0];
          }
          return cita;
        });
      })
    );
  }

  getCita(id: number): Observable<Cita> {
    return this.http.get<Cita>(`${this.baseUrl}/${id}`).pipe(
      map(cita => {
        if (cita.fechaInicio) {
          cita.fechaInicio = new Date(cita.fechaInicio).toISOString().split('T')[0];
        }
        return cita;
      })
    );
  }

  crearCita(cita: Cita): Observable<Cita> {
    cita.estadoId = 1;
    return this.http.post<Cita>(`${this.baseUrl}`, cita);
  }

  actualizarCita(id: number, empleadoId: number, cita: Cita): Observable<any> {
    return this.http.put(`${this.baseUrl}/${id}/${empleadoId}`, cita);
  }

  eliminarCita(id: number, empleadoId: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/${id}/${empleadoId}`);
  }

  nuevaCita() {
    this.getCitas().subscribe(data => {
      this.$listaCitas.next(data);
    })
  }

  seleccionarCita(id: number) {
    this.$citaId.next(id)
  }
}
