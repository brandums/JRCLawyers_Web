import { Component, OnInit } from '@angular/core';
import { RecordatorioService } from '../services/recordatorio-service';
import { Recordatorio } from '../Models/Recordatorio';
import { Cliente } from '../Models/Cliente';
import { ClienteService } from '../services/cliente-service';
import { AlertService } from '../services/AlertService';
import { AuthService } from '../services/AuthService';

@Component({
  selector: 'app-create-reminder',
  templateUrl: './create-reminder.component.html'
})
export class CreateReminderComponent implements OnInit {
  user: any;
  recordatorio: Recordatorio = new Recordatorio;
  clientes: Cliente[] = [];
  recordatorioId: number = 0;
  creandoRecordatorio = false;
  tituloForm = "Crear nuevo Recordatorio";
  formValidado = false;
  errorMensaje: string | null = null;

  constructor(
    private recordatorioService: RecordatorioService,
    private clienteService: ClienteService,
    private alertService: AlertService,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    this.authService.usuario.subscribe(usuario => {
      this.user = usuario;
    });

    this.clienteService.getClientes().subscribe(data => this.clientes = data);

    this.recordatorioService.$recordatorioId.subscribe(id => {
      this.recordatorioId = id;
      if (id != 0) {
        this.recordatorioService.getRecordatorio(id).subscribe(data => {
          this.recordatorio = data;
          this.tituloForm = "Actualizar datos del Recordatorio"
        })
      }
      else {
        this.limpiarForm()
      }
    })
  }

  obtenerFecha(): string {
    const fechaActual = new Date();
    const formatoFecha = fechaActual.toISOString().split('T')[0];

    return formatoFecha;
  }

  FechaMaxima(): string {
    const fechaActual = new Date();
    const fechaFutura = new Date(fechaActual);
    fechaFutura.setFullYear(fechaActual.getFullYear() + 3);

    const formatoFecha = fechaFutura.toISOString().split('T')[0];
    return formatoFecha;
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

  validarHora(event: Event) {
    const inputElement = event.target as HTMLInputElement;
    const horaSeleccionada = inputElement.value;

    const inicioManana = "08:00";
    const finManana = "12:00";

    const inicioTarde = "14:00";
    const finTarde = "19:00";

    if (
      (horaSeleccionada >= inicioManana && horaSeleccionada <= finManana) ||
      (horaSeleccionada >= inicioTarde && horaSeleccionada <= finTarde)
    ) {
      inputElement.setCustomValidity("");
    } else {
      inputElement.setCustomValidity("Por favor seleccione una hora válida.");
    }

    inputElement.reportValidity();
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
          if (this.recordatorioId != 0) {
            this.actualizarRecordatorio();
          }
          else {
            this.crearRecordatorio();
          }
        }

        typedForm.classList.add('was-validated');
      }, false);
    });
  }

  crearRecordatorio() {
    this.creandoRecordatorio = true;
    this.errorMensaje = null;

    this.recordatorio.empleadoId = this.user.id;
    this.recordatorio.clienteId = parseInt(this.recordatorio.clienteId);
    this.recordatorioService.crearRecordatorio(this.recordatorio).subscribe(() => {
      this.recordatorioService.nuevoRecordatorio();

      this.cerrarForm();
      this.creandoRecordatorio = false;
      this.alertService.showMessage('Recordatorio creado con exito.');
    }, (error) => {
      this.creandoRecordatorio = false;
      this.errorMensaje = error?.error?.message || 'Ocurrió un error al crear el recordatorio. Inténtalo nuevamente.';
    });
  }

  actualizarRecordatorio() {
    this.creandoRecordatorio = true;
    this.errorMensaje = null;

    this.recordatorioService.actualizarRecordatorio(this.recordatorioId, this.user.id, this.recordatorio).subscribe(() => {
      this.recordatorioService.nuevoRecordatorio();

      this.cerrarForm();
      this.creandoRecordatorio = false;
      this.alertService.showMessage('Recordatorio actualizado con exito.');
    }, (error) => {
      this.creandoRecordatorio = false;
      this.errorMensaje = error?.error?.message || 'Ocurrió un error al actualizar el recordatorio. Inténtalo nuevamente.';
    });
  }

  cerrarForm() {
    const button = document.getElementById('bClose');
    if (button instanceof HTMLElement) {
      button.click()
    }
  }

  limpiarForm() {
    this.errorMensaje = null;
    this.recordatorio = new Recordatorio;
    this.tituloForm = "Crear nuevo Recordatorio";
    this.formValidado = false;

    const forms = document.querySelectorAll('.needs-validation');
    Array.from(forms).forEach((form: Element) => {
      form.classList.remove('was-validated');
    });
  }
}
