import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, catchError, map, throwError } from 'rxjs';
import { API_BASE_URL } from '../configuracion';
import { Reporte } from '../Models/Reporte';

@Injectable({
  providedIn: 'root'
})
export class ReporteService {

  private baseUrl = `${API_BASE_URL}/Reporte`;
  public $listaReportes: BehaviorSubject<Reporte[]> = new BehaviorSubject<Reporte[]>([]);
  public $reporteId: BehaviorSubject<number> = new BehaviorSubject<number>(0);

  constructor(private http: HttpClient) { }

  getReportes(): Observable<Reporte[]> {
    return this.http.get<Reporte[]>(`${this.baseUrl}`).pipe(
      map((reportes: Reporte[]) => {
        return reportes.map(reporte => {
          if (reporte.fechaInicio) {
            reporte.fechaInicio = new Date(reporte.fechaInicio).toISOString().split('T')[0];
            reporte.fechaFin = new Date(reporte.fechaFin).toISOString().split('T')[0];
          }
          reporte.fechaGeneracion = new Date(reporte.fechaGeneracion).toISOString().split('T')[0];
          
          return reporte;
        });
      })
    );
  }

  getReporte(id: number): Observable<Reporte> {
    return this.http.get<Reporte>(`${this.baseUrl}/${id}`).pipe(
      map(reporte => {
        if (reporte.fechaInicio) {
          reporte.fechaInicio = new Date(reporte.fechaInicio).toISOString().split('T')[0];
          reporte.fechaFin = new Date(reporte.fechaFin).toISOString().split('T')[0];
        }
        reporte.fechaGeneracion = new Date(reporte.fechaGeneracion).toISOString().split('T')[0];
        return reporte;
      })
    );
  }

  sendPDF(id: number, mail: string): Observable<any> {
    return this.http.get(`${this.baseUrl}/sendPDF/${id}/${mail}`);
  }

  crearReporte(reporte: Reporte): Observable<Reporte> {
    if (!reporte.clienteId) {
      reporte.clienteId = null;
    }
    if (!reporte.empleadoId) {
      reporte.empleadoId = null;
    }
    if (reporte.fechaInicio == "") {
      reporte.fechaInicio = null;
    }
    if (reporte.fechaFin == "") {
      reporte.fechaFin = null;
    }
    reporte.fechaGeneracion = null;
    return this.http.post<Reporte>(`${this.baseUrl}`, reporte).pipe(
      catchError((error) => {
        if (error.status === 409) {
          return throwError(() => new Error('Ya existe un reporte con el mismo nombre.'));
        }
        return throwError(() => new Error('Error creando el cliente. Inténtalo de nuevo más tarde.'));
      })
    );
  }

  actualizarReporte(id: number, empleadoId: number, reporte: Reporte): Observable<any> {
    return this.http.put(`${this.baseUrl}/${id}/${empleadoId}`, reporte);
  }

  eliminarReporte(id: number, empleadoId: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/${id}/${empleadoId}`);
  }

  nuevoReporte() {
    this.getReportes().subscribe(data => {
      this.$listaReportes.next(data);
    })
  }

  seleccionarReporte(id: number) {
    this.$reporteId.next(id)
  }
}
