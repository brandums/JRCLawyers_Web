import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, catchError, map, throwError } from 'rxjs';
import { Expediente } from '../Models/Expediente';
import { API_BASE_URL } from '../configuracion';

@Injectable({
  providedIn: 'root'
})
export class ExpedienteService {

  private baseUrl = `${API_BASE_URL}/Expediente`;
  public $listaExpedientes: BehaviorSubject<Expediente[]> = new BehaviorSubject<Expediente[]>([]);
  public $expedienteId: BehaviorSubject<number> = new BehaviorSubject<number>(0);

  constructor(private http: HttpClient) { }

  getExpedientes(): Observable<Expediente[]> {
    return this.http.get<Expediente[]>(`${this.baseUrl}`).pipe(
      map((expedientes: Expediente[]) => {
        return expedientes.map(expediente => {
          expediente.fechaInicio = new Date(expediente.fechaInicio).toISOString().split('T')[0];
          return expediente;
        });
      })
    );
  }

  getExpediente(id: number): Observable<Expediente> {
    return this.http.get<Expediente>(`${this.baseUrl}/${id}`).pipe(
      map(expediente => {
        expediente.fechaInicio = new Date(expediente.fechaInicio).toISOString().split('T')[0];
        return expediente;
      })
    );
  }

  sendPDF(id: number, mail: string): Observable<any> {
    return this.http.get(`${this.baseUrl}/sendPDF/${id}/${mail}`);
  }

  crearExpediente(expediente: Expediente): Observable<Expediente> {
    return this.http.post<Expediente>(`${this.baseUrl}`, expediente);
  }

  actualizarExpediente(id: number, empleadoId: number, expediente: Expediente): Observable<any> {
    return this.http.put(`${this.baseUrl}/${id}/${empleadoId}`, expediente);
  }

  eliminarExpediente(id: number, empleadoId: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/${id}/${empleadoId}`);
  }

  nuevoExpediente() {
    this.getExpedientes().subscribe(data => {
      this.$listaExpedientes.next(data);
    })
  }

  seleccionarExpediente(id: number) {
    this.$expedienteId.next(id)
  }
}
