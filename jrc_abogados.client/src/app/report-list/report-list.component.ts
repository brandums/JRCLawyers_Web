import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { AlertService } from '../services/AlertService';
import { AuthService } from '../services/AuthService';
import { Reporte } from '../Models/Reporte';
import { ReporteService } from '../services/reporte-service';

@Component({
  selector: 'app-report-list',
  templateUrl: './report-list.component.html'
})
export class ReportListComponent implements OnInit {
  user: any;
  reportes: Reporte[] = [];
  buscarPalabra: string = '';
  reporteSeleccionado: Reporte | null = null;
  formValidado = false;
  extencionEmail = "gmail";
  extencionEmail2 = "com";
  nickEmail = ""
  enviandoPDF = false;

  paginaActual: number = 1;
  reportesPorPagina: number = 10;

  campoFiltro: string = 'nombre';

  constructor(
    private reporteService: ReporteService,
    private alertService: AlertService,
    private authService: AuthService
  ) {
    this.authService.usuario.subscribe(usuario => {
      this.user = usuario;
    });
  }

  ngOnInit(): void {
    this.getReportes();

    this.reporteService.$listaReportes.subscribe(data => this.reportes = data);
  }

  defLink: any;
  @ViewChild('modalPDF') _modal!: ElementRef;
  displayStyle = "none";
  openModal() {
    this.defLink = `${this.reporteSeleccionado?.path}#toolbar=0`

    this._modal.nativeElement.setAttribute('src', this.defLink)
    this.displayStyle = "block";
  }

  closeModal() {
    this.displayStyle = "none";
    this._modal.nativeElement.setAttribute('src', '')
  }

  printPDF() {
    const iframe: HTMLIFrameElement = this._modal.nativeElement;
    if (iframe && iframe.contentWindow) {
      iframe.contentWindow.print();
    }
  }

  downloadPDF() {
    const iframe: HTMLIFrameElement = this._modal.nativeElement;
    if (iframe) {
      const link = document.createElement('a');
      link.href = iframe.src;
      link.download = this.reporteSeleccionado?.nombre || 'reporte.pdf';
      link.click();
    }
  }


  seleccionarReporte(casoId: number) {
    this.reporteService.seleccionarReporte(casoId);
  }

  verReporte(reporte: Reporte) {
    this.reporteSeleccionado = reporte;
  }

  editarReporte(clienteId: number) {
    this.seleccionarReporte(clienteId);
  }

  getReportes(): void {
    this.reporteService.getReportes()
      .subscribe((reportes: Reporte[]) => this.reportes = reportes);
  }

  get filtroReportes(): Reporte[] {
    return this.reportes.filter(reporte => {
      const valor = this.obtenerValor(reporte, this.campoFiltro);
      return valor && valor.toString().toLowerCase().includes(this.buscarPalabra.toLowerCase());
    });
  }

  obtenerValor(obj: any, ruta: string): any {
    return ruta.split('.').reduce((o, i) => o?.[i], obj);
  }

  eliminarReporte(id: number): void {
    this.reporteService.eliminarReporte(id, this.user.id)
      .subscribe(() => {
        this.reportes = this.reportes.filter(reporte => reporte.id !== id);
        this.alertService.showMessage('Reporte eliminado con exito.');
      });
  }

  validarEmail(event: KeyboardEvent | ClipboardEvent) {
    const teclaPresionada = (event as KeyboardEvent).key;
    const patron = /^[^\s'"@]$/;

    if (event instanceof KeyboardEvent) {
      if (!patron.test(teclaPresionada) && teclaPresionada !== 'Backspace' && teclaPresionada !== 'Delete' && teclaPresionada !== 'ArrowLeft' && teclaPresionada !== 'ArrowRight') {
        event.preventDefault();
      }
    }

    if (event instanceof ClipboardEvent) {
      const clipboardData = event.clipboardData?.getData('text') || '';
      if (/[@^\s'"]/.test(clipboardData)) {
        event.preventDefault();
      }
    }
  }

  iniciarValidadores() {
    const forms = document.querySelectorAll('.needs-validation');

    Array.from(forms).forEach((form: Element) => {
      const typedForm = form as HTMLFormElement;
      typedForm.addEventListener('submit', (event) => {
        if (this.formValidado) {
          event.preventDefault();
          event.stopPropagation();
          return;
        }
        if (!typedForm.checkValidity()) {
          event.preventDefault();
          event.stopPropagation();
        }
        else {
          this.formValidado = true;
          this.enviarReporte();
        }

        typedForm.classList.add('was-validated');
      }, false);
    });
  }

  enviarReporte() {
    this.enviandoPDF = true;
    var email = this.nickEmail + "@" + this.extencionEmail + ".com"
    if (this.reporteSeleccionado) {
      this.reporteService.sendPDF(this.reporteSeleccionado.id, email).subscribe(() => {

        this.cerrarForm();
        this.enviandoPDF = false;
        this.alertService.showMessage('PDF enviado con exito.');
      }, () => {
        this.enviandoPDF = false;
        this.formValidado = false;
      })
    }    
  }

  cerrarForm() {
    const button = document.getElementById('bClose');
    if (button instanceof HTMLElement) {
      button.click()
    }
  }

  limpiarForm() {
    this.nickEmail = "";
    this.formValidado = false;
    this.enviandoPDF = false;

    const forms = document.querySelectorAll('.needs-validation');
    Array.from(forms).forEach((form: Element) => {
      form.classList.remove('was-validated');
    });
  }

  // Métodos para la paginación
  get reportesPaginados(): Reporte[] {
    const inicio = (this.paginaActual - 1) * this.reportesPorPagina;
    const fin = inicio + this.reportesPorPagina;
    return this.filtroReportes.slice(inicio, fin);
  }

  cambiarPagina(pagina: number): void {
    if (pagina >= 1 && pagina <= this.totalPaginas) {
      this.paginaActual = pagina;
    }
  }

  get totalPaginas(): number {
    return Math.ceil(this.filtroReportes.length / this.reportesPorPagina);
  }
}
