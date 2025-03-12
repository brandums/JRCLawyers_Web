import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { AlertService } from '../services/AlertService';
import { Documento } from '../Models/Documento';
import { DocumentoService } from '../services/document-service';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '../services/AuthService';

@Component({
  selector: 'app-document-list',
  templateUrl: './document-list.component.html'
})
export class DocumentListComponent implements OnInit {
  user: any;
  documentos: Documento[] = [];
  buscarPalabra: string = '';
  documentoSeleccionado: Documento | null = null;
  expedienteId: any;

  paginaActual: number = 1;
  documentosPorPagina: number = 10;

  campoFiltro: string = 'nombre';

  constructor(
    private documentoService: DocumentoService,
    private alertService: AlertService,
    private route: ActivatedRoute,
    private authService: AuthService,
  ) { }

  ngOnInit(): void {
    this.authService.usuario.subscribe(usuario => {
      this.user = usuario;
    });

    this.route.queryParams.subscribe(params => {
      this.expedienteId = +params['expedienteId'];
    });

    this.getDocumentos();

    this.documentoService.$listaDocumentos.subscribe(() => {
      this.getDocumentos()
    });
  }

  defLink: any;
  @ViewChild('modalPDF') _modal!: ElementRef;
  displayStyle = "none";
  openModal() {
    this.defLink = `${this.documentoSeleccionado?.path}#toolbar=0`

    this._modal.nativeElement.setAttribute('src', this.defLink)
    this.displayStyle = "block";
  }

  closeModal() {
    this.displayStyle = "none";
    this._modal.nativeElement.setAttribute('src', '')
  }

  seleccionarDocumento(documentoId: number) {
    this.documentoService.seleccionarDocumento(documentoId);
  }

  verDocumento(documento: Documento) {
    this.documentoSeleccionado = documento;
  }

  editarDocumento(clienteId: number) {
    this.seleccionarDocumento(clienteId);
  }

  getDocumentos(): void {
    this.documentoService.getDocumentos(this.expedienteId)
      .subscribe((documentos: Documento[]) => this.documentos = documentos);
  }

  get filtroDocumentos(): Documento[] {
    return this.documentos.filter(expediente => {
      const valor = this.obtenerValor(expediente, this.campoFiltro);
      return valor && valor.toString().toLowerCase().includes(this.buscarPalabra.toLowerCase());
    });
  }

  obtenerValor(obj: any, ruta: string): any {
    return ruta.split('.').reduce((o, i) => o?.[i], obj);
  }

  eliminarDocumento(id: number): void {
    this.documentoService.eliminarDocumento(id, this.user.id)
      .subscribe(() => {
        this.documentos = this.documentos.filter(caso => caso.id !== id);
        this.alertService.showMessage('Documento eliminado con exito.');
      });
  }

  // Métodos para la paginación
  get documentosPaginados(): Documento[] {
    const inicio = (this.paginaActual - 1) * this.documentosPorPagina;
    const fin = inicio + this.documentosPorPagina;
    return this.filtroDocumentos.slice(inicio, fin);
  }

  cambiarPagina(pagina: number): void {
    if (pagina >= 1 && pagina <= this.totalPaginas) {
      this.paginaActual = pagina;
    }
  }

  get totalPaginas(): number {
    return Math.ceil(this.filtroDocumentos.length / this.documentosPorPagina);
  }
}
