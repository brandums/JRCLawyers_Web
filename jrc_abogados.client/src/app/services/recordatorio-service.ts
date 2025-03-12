import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, map } from 'rxjs';
import { Recordatorio } from '../Models/Recordatorio';
import { API_BASE_URL } from '../configuracion';

@Injectable({
  providedIn: 'root'
})
export class RecordatorioService {

  private baseUrl = `${API_BASE_URL}/Recordatorio`;
  public $listaRecordatorios: BehaviorSubject<Recordatorio[]> = new BehaviorSubject<Recordatorio[]>([]);
  public $recordatorioId: BehaviorSubject<number> = new BehaviorSubject<number>(0);

  constructor(private http: HttpClient) { }

  getRecordatorios(): Observable<Recordatorio[]> {
    return this.http.get<Recordatorio[]>(`${this.baseUrl}`).pipe(
      map((recordatorios: Recordatorio[]) => {
        return recordatorios.map(recordatorio => {
          recordatorio.fecha = new Date(recordatorio.fecha).toISOString().split('T')[0];
          return recordatorio;
        });
      })
    );
  }

  getRecordatorio(id: number): Observable<Recordatorio> {
    return this.http.get<Recordatorio>(`${this.baseUrl}/${id}`).pipe(
      map(recordatorio => {
        recordatorio.fecha = new Date(recordatorio.fecha).toISOString().split('T')[0];
        return recordatorio;
      })
    );
  }

  crearRecordatorio(recordatorio: Recordatorio): Observable<Recordatorio> {
    return this.http.post<Recordatorio>(`${this.baseUrl}`, recordatorio);
  }

  actualizarRecordatorio(id: number, empleadoId: number, recordatorio: Recordatorio): Observable<any> {
    return this.http.put(`${this.baseUrl}/${id}/${empleadoId}`, recordatorio);
  }

  eliminarRecordatorio(id: number, empleadoId: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/${id}/${empleadoId}`);
  }

  nuevoRecordatorio() {
    this.getRecordatorios().subscribe(data => {
      this.$listaRecordatorios.next(data);
    })
  }

  seleccionarRecordatorio(id: number) {
    this.$recordatorioId.next(id)
  }
}
