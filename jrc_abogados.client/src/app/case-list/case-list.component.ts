import { Component, OnInit } from '@angular/core';
import { Caso } from '../Models/Caso';
import { CasoService } from '../services/caso-service';
import { AlertService } from '../services/AlertService';
import { AuthService } from '../services/AuthService';

@Component({
  selector: 'app-case-list',
  templateUrl: './case-list.component.html'
})
export class CaseListComponent implements OnInit {
  user: any;
  casos: Caso[] = [];
  buscarPalabra: string = '';
  casoSeleccionado: Caso | null = null;

  paginaActual: number = 1;
  clientesPorPagina: number = 10;

  campoFiltro: string = 'cliente.nombre';

  constructor(
    private casoService: CasoService,
    private alertService: AlertService,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    this.authService.usuario.subscribe(usuario => {
      this.user = usuario;
    });

    this.getCasos();

    this.casoService.$listaCasos.subscribe(data => this.casos = data);
  }

  seleccionarCaso(casoId: number) {
    this.casoService.seleccionarCaso(casoId);
  }

  verCaso(cliente: Caso) {
    this.casoSeleccionado = cliente;
  }

  editarCaso(clienteId: number) {
    this.seleccionarCaso(clienteId);
  }

  getCasos(): void {
    this.casoService.getCasos()
      .subscribe((casos: Caso[]) => this.casos = casos);
  }

  get filtroCasos(): Caso[] {
    return this.casos.filter(caso => {
      const valor = this.obtenerValor(caso, this.campoFiltro);
      return valor && valor.toString().toLowerCase().includes(this.buscarPalabra.toLowerCase());
    });
  }

  obtenerValor(obj: any, ruta: string): any {
    return ruta.split('.').reduce((o, i) => o?.[i], obj);
  }

  eliminarCaso(id: number): void {
    this.casoService.eliminarCaso(id, this.user.id)
      .subscribe(() => {
        this.casos = this.casos.filter(caso => caso.id !== id);
        this.alertService.showMessage('Caso eliminado con exito.');
      });
  }

  // Métodos para la paginación
  get clientesPaginados(): Caso[] {
    const inicio = (this.paginaActual - 1) * this.clientesPorPagina;
    const fin = inicio + this.clientesPorPagina;
    return this.filtroCasos.slice(inicio, fin);
  }

  cambiarPagina(pagina: number): void {
    if (pagina >= 1 && pagina <= this.totalPaginas) {
      this.paginaActual = pagina;
    }
  }

  get totalPaginas(): number {
    return Math.ceil(this.filtroCasos.length / this.clientesPorPagina);
  }
}
