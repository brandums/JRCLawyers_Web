import { Component, OnInit } from '@angular/core';
import { AlertService } from '../services/AlertService';
import { Expediente } from '../Models/Expediente';
import { ExpedienteService } from '../services/expediente-service';
import { Router } from '@angular/router';
import { AuthService } from '../services/AuthService';

@Component({
  selector: 'app-expedient-list',
  templateUrl: './expedient-list.component.html'
})
export class ExpedientListComponent implements OnInit {
  user: any;
  expedientes: Expediente[] = [];
  buscarPalabra: string = '';
  expedienteSeleccionado: Expediente | null = null;
  formValidado = false;
  extencionEmail = "gmail";
  nickEmail = ""
  enviandoPDF = false;

  paginaActual: number = 1;
  expedientesPorPagina: number = 10;

  campoFiltro: string = 'nombre';

  constructor(
    private expedienteService: ExpedienteService,
    private alertService: AlertService,
    private router: Router,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    this.authService.usuario.subscribe(usuario => {
      this.user = usuario;
    });

    this.getExpedientes();

    this.expedienteService.$listaExpedientes.subscribe(data => this.expedientes = data);
  }

  openDocumentList(expedienteId: number) {
    this.router.navigate(['/document-list'], { queryParams: { expedienteId: expedienteId } });
  }

  seleccionarExpediente(expedienteId: number) {
    this.expedienteService.seleccionarExpediente(expedienteId);
  }

  verExpediente(expediente: Expediente) {
    this.expedienteSeleccionado = expediente;
  }

  editarExpediente(clienteId: number) {
    this.seleccionarExpediente(clienteId);
  }

  getExpedientes(): void {
    this.expedienteService.getExpedientes()
      .subscribe((expedientes: Expediente[]) => this.expedientes = expedientes);
  }

  get filtroExpedientes(): Expediente[] {
    return this.expedientes.filter(expediente => {
      const valor = this.obtenerValor(expediente, this.campoFiltro);
      return valor && valor.toString().toLowerCase().includes(this.buscarPalabra.toLowerCase());
    });
  }

  obtenerValor(obj: any, ruta: string): any {
    return ruta.split('.').reduce((o, i) => o?.[i], obj);
  }

  eliminarExpediente(id: number): void {
    this.expedienteService.eliminarExpediente(id, this.user.id).subscribe(() => {
        this.expedientes = this.expedientes.filter(caso => caso.id !== id);
        this.alertService.showMessage('Expediente eliminado con exito.');
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
          this.enviarExpediente();
        }

        typedForm.classList.add('was-validated');
      }, false);
    });
  }

  enviarExpediente() {
    this.enviandoPDF = true;
    var email = this.nickEmail + "@" + this.extencionEmail + ".com"
    if (this.expedienteSeleccionado) {
      this.expedienteService.sendPDF(this.expedienteSeleccionado.id, email).subscribe(() => {

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
  get expedientesPaginados(): Expediente[] {
    const inicio = (this.paginaActual - 1) * this.expedientesPorPagina;
    const fin = inicio + this.expedientesPorPagina;
    return this.filtroExpedientes.slice(inicio, fin);
  }

  cambiarPagina(pagina: number): void {
    if (pagina >= 1 && pagina <= this.totalPaginas) {
      this.paginaActual = pagina;
    }
  }

  get totalPaginas(): number {
    return Math.ceil(this.filtroExpedientes.length / this.expedientesPorPagina);
  }
}
