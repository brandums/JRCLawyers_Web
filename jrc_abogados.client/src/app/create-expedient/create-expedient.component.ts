import { Component, OnInit } from '@angular/core';
import { Cliente } from '../Models/Cliente';
import { ClienteService } from '../services/cliente-service';
import { AlertService } from '../services/AlertService';
import { Expediente } from '../Models/Expediente';
import { TipoExpediente } from '../Models/TipoExpediente';
import { TipoExpedienteService } from '../services/tipoExpediente-service';
import { ExpedienteService } from '../services/expediente-service';
import { CasoService } from '../services/caso-service';
import { AuthService } from '../services/AuthService';

@Component({
  selector: 'app-create-expedient',
  templateUrl: './create-expedient.component.html'
})
export class CreateExpedientComponent implements OnInit {
  user: any;
  expediente: Expediente = new Expediente;
  expedienteId = 0;
  tiposExpediente: TipoExpediente[] = [];
  clientes: Cliente[] = [];
  creandoExpediente = false;
  tituloForm = "Crear nuevo Expediente";
  formValidado = false;
  errorMensaje: string | null = null;
  casos: any;

  constructor(
    private clienteService: ClienteService,
    private expedienteService: ExpedienteService,
    private tipoExpedienteService: TipoExpedienteService,
    private casoService: CasoService,
    private alertService: AlertService,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    this.authService.usuario.subscribe(usuario => {
      this.user = usuario;
    });

    this.tipoExpedienteService.getTipoDeExpediente().subscribe(data => this.tiposExpediente = data);
    this.clienteService.getClientes().subscribe(data => this.clientes = data);

    this.expedienteService.$expedienteId.subscribe(id => {
      this.expedienteId = id;
      if (id != 0) {
        this.expedienteService.getExpediente(id).subscribe(data => {
          this.expediente = data;
          this.getCasosByClient(this.expediente.casoId);
        })
        this.tituloForm = "Actualizar datos del Expediente"
      }
      else {
        this.limpiarForm()
      }
    })
  }

  onClienteChange(): void {
    this.getCasosByClient(0);
  }

  getCasosByClient(number: number) {
    if (this.expediente.clienteId) {
      this.casoService.getCasosByClient(parseInt(this.expediente.clienteId), number).subscribe(data => {
        this.casos = data;
      }, error => {
        this.casos = [];
      });
    } else {
      this.casos = [];
    }
  }

  onTypeExpedientChange() {
    if (this.expediente.tipoExpedienteId == '1') {
      this.expediente.casoId = "";
    }
  }

  obtenerFecha(): string {
    const fechaActual = new Date();
    const formatoFecha = fechaActual.toISOString().split('T')[0];

    return formatoFecha;
  }

  FechaMaxima(): string {
    const fechaActual = new Date();
    const fechaFutura = new Date(fechaActual);
    fechaFutura.setFullYear(fechaActual.getFullYear() + 1);

    const formatoFecha = fechaFutura.toISOString().split('T')[0];
    return formatoFecha;
  }


  limitarAlfanumerico(event: KeyboardEvent | ClipboardEvent) {
    const teclaPresionada = (event as KeyboardEvent).key;
    const patron = /^[a-zA-Z0-9]$/;

    if (event instanceof KeyboardEvent) {
      if (!patron.test(teclaPresionada) && teclaPresionada !== 'Backspace' && teclaPresionada !== 'Delete' && teclaPresionada !== 'ArrowLeft' && teclaPresionada !== 'ArrowRight') {
        event.preventDefault();
      }
    }

    if (event instanceof ClipboardEvent) {
      const textoPegado = event.clipboardData?.getData('text') || '';
      if (!/^[a-zA-Z0-9]+$/.test(textoPegado)) {
        event.preventDefault();
      }
    }
  }

  limitarNumeros(event: KeyboardEvent | ClipboardEvent) {
    const teclaPresionada = (event as KeyboardEvent).key;
    const patron = /^[0-9]$/;

    if (event instanceof KeyboardEvent) {
      if (!patron.test(teclaPresionada) && teclaPresionada !== 'Backspace' && teclaPresionada !== 'Delete' && teclaPresionada !== 'ArrowLeft' && teclaPresionada !== 'ArrowRight') {
        event.preventDefault();
      }
    }

    if (event instanceof ClipboardEvent) {
      event.preventDefault();
    }
  }

  validarTexto(event: KeyboardEvent, minChars: number) {
    const teclaPresionada = event.key;
    const valorActual = (event.target as HTMLInputElement).value;

    if (valorActual.length <= minChars && teclaPresionada === ' ') {
      event.preventDefault();
      return;
    }

    if (valorActual.slice(-1) === ' ' && teclaPresionada === ' ') {
      event.preventDefault();
      return;
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
          if (this.expedienteId != 0) {
            this.actualizarExpediente();
          }
          else {
            this.crearExpediente();
          }
        }

        typedForm.classList.add('was-validated');
      }, false);
    });
  }

  crearExpediente() {
    this.creandoExpediente = true;
    this.errorMensaje = null;
    this.expediente.empleadoId = this.user.id

    if (this.expediente.tipoExpedienteId == '1') this.expediente.casoId = null;

    this.expedienteService.crearExpediente(this.expediente).subscribe({
      next: () => {
        this.expedienteService.nuevoExpediente();
        this.cerrarForm();
        this.creandoExpediente = false;
        this.alertService.showMessage('Expediente creado con éxito.');
      },
      error: (error) => {
        this.errorMensaje = error.error?.message || 'Ocurrió un error al crear el expediente.';
        this.creandoExpediente = false;
        this.formValidado = false;
      },
    });
  }

  actualizarExpediente() {
    this.creandoExpediente = true;
    this.errorMensaje = null;

    if (this.expediente.tipoExpedienteId == '1') this.expediente.casoId = null;

    this.expedienteService.actualizarExpediente(this.expedienteId, this.user.id, this.expediente).subscribe({
      next: () => {
        this.expedienteService.nuevoExpediente();
        this.cerrarForm();
        this.creandoExpediente = false;
        this.alertService.showMessage('Expediente actualizado con éxito.');
      },
      error: (error) => {
        this.errorMensaje = error.error?.message || 'No se pudo actualizar el expediente. Intente nuevamente.';
        this.creandoExpediente = false;
        this.formValidado = false;
      },
    });
  }

  cerrarForm() {
    const button = document.getElementById('bClose');
    if (button instanceof HTMLElement) {
      button.click()
    }
  }

  limpiarForm() {
    this.expediente = new Expediente;
    this.tituloForm = "Crear nuevo Expediente"
    this.formValidado = false;
    this.errorMensaje = null;

    const forms = document.querySelectorAll('.needs-validation');
    Array.from(forms).forEach((form: Element) => {
      form.classList.remove('was-validated');
    });
  }
}
