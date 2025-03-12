import { Component, OnInit } from '@angular/core';
import { Cita } from '../Models/Cita';
import { CitaService } from '../services/cita-service';
import { AlertService } from '../services/AlertService';
import { AuthService } from '../services/AuthService';

@Component({
  selector: 'app-appointment-list',
  templateUrl: './appointment-list.component.html'
})
export class AppointmentListComponent implements OnInit {
  user: any;
  citas: Cita[] = [];
  buscarPalabra: string = '';
  citaSeleccionada: Cita | null = null;

  paginaActual: number = 1;
  clientesPorPagina: number = 10;

  campoFiltro: string = 'cliente.nombre';

  constructor(
    private citaService: CitaService,
    private alertService: AlertService,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    this.authService.usuario.subscribe(usuario => {
      this.user = usuario;
    });

    this.getCitas();

    this.citaService.$listaCitas.subscribe(data => this.citas = data);
  }

  seleccionarCita(citaId: number) {
    this.citaService.seleccionarCita(citaId);
  }

  verCita(cliente: Cita) {
    this.citaSeleccionada = cliente;
  }

  editarCita(clienteId: number) {
    this.seleccionarCita(clienteId);
  }

  getCitas(): void {
    this.citaService.getCitas().subscribe((cita: Cita[]) => this.citas = cita);
  }

  get filtroCitas(): Cita[] {
    return this.citas.filter(cita => {
      const valor = this.obtenerValor(cita, this.campoFiltro);
      return valor && valor.toString().toLowerCase().includes(this.buscarPalabra.toLowerCase());
    });
  }

  obtenerValor(obj: any, ruta: string): any {
    return ruta.split('.').reduce((o, i) => o?.[i], obj);
  }


  eliminarCita(id: number): void {
    this.citaService.eliminarCita(id, this.user.id).subscribe(() => {
      this.citas = this.citas.filter(cita => cita.id !== id);
      this.alertService.showMessage('Cita eliminada con exito.');
    });
  }

  // Métodos para la paginación
  get clientesPaginados(): Cita[] {
    const inicio = (this.paginaActual - 1) * this.clientesPorPagina;
    const fin = inicio + this.clientesPorPagina;
    return this.filtroCitas.slice(inicio, fin);
  }

  cambiarPagina(pagina: number): void {
    if (pagina >= 1 && pagina <= this.totalPaginas) {
      this.paginaActual = pagina;
    }
  }

  get totalPaginas(): number {
    return Math.ceil(this.filtroCitas.length / this.clientesPorPagina);
  }
}
