import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, catchError, map, throwError } from 'rxjs';
import { Caso } from '../Models/Caso';
import { API_BASE_URL } from '../configuracion';


@Injectable({
  providedIn: 'root'
})
export class CasoService {

  private baseUrl = `${API_BASE_URL}/Caso`;
  public $listaCasos: BehaviorSubject<Caso[]> = new BehaviorSubject<Caso[]>([]);
  public $casoId: BehaviorSubject<number> = new BehaviorSubject<number>(0);

  constructor(private http: HttpClient) { }

  getCasos(): Observable<Caso[]> {
    return this.http.get<Caso[]>(`${this.baseUrl}`).pipe(
      map((casos: Caso[]) => {
        return casos.map(caso => {
          caso.fechaInicio = new Date(caso.fechaInicio).toISOString().split('T')[0];
          caso.fechaTermino = new Date(caso.fechaTermino).toISOString().split('T')[0];
          
          return caso;
        });
      })
    );
  }

  getCaso(id: number): Observable<Caso> {
    return this.http.get<Caso>(`${this.baseUrl}/${id}`).pipe(
      map(caso => {
        caso.fechaInicio = new Date(caso.fechaInicio).toISOString().split('T')[0];
        caso.fechaTermino = new Date(caso.fechaTermino).toISOString().split('T')[0];
        return caso;
      })
    );
  }

  getCasosByClient(id: number, casoId: number): Observable<Caso[]> {
    return this.http.get<Caso[]>(`${this.baseUrl}/expedientesByClient/${id}/${casoId}`).pipe(
      map((casos: Caso[]) => {
        return casos.map(caso => {
          caso.fechaInicio = new Date(caso.fechaInicio).toISOString().split('T')[0];
          caso.fechaTermino = new Date(caso.fechaTermino).toISOString().split('T')[0];

          return caso;
        });
      })
    );
  }

  crearCaso(caso: Caso): Observable<Caso> {
    if (!caso.fechaTermino) {
      caso.fechaTermino = caso.fechaInicio;
    }
    return this.http.post<Caso>(`${this.baseUrl}`, caso)
  }

  actualizarCaso(id: number, empleadoId: number, caso: Caso): Observable<any> {
    return this.http.put(`${this.baseUrl}/${id}/${empleadoId}`, caso);
  }

  eliminarCaso(id: number, empleadoId: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/${id}/${empleadoId}`);
  }

  nuevoCaso() {
    this.getCasos().subscribe(data => {
      this.$listaCasos.next(data);
    })
  }

  seleccionarCaso(id: number) {
    this.$casoId.next(id)
  }
}
