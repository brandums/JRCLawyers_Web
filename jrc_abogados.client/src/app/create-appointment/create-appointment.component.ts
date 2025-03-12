import { Component, OnInit } from '@angular/core';
import { CitaService } from '../services/cita-service';
import { Cita } from '../Models/Cita';
import { UbicacionService } from '../services/ubicacion-service';
import { Estado } from '../Models/Estado';
import { EstadoService } from '../services/estado-service';
import { Cliente } from '../Models/Cliente';
import { ClienteService } from '../services/cliente-service';
import { CityStateService } from '../services/city-state.service';
import { AlertService } from '../services/AlertService';
import { AuthService } from '../services/AuthService';

@Component({
  selector: 'app-create-appointment',
  templateUrl: './create-appointment.component.html'
})
export class CreateAppointmentComponent implements OnInit {
  user: any;
  cita: Cita = new Cita;
  citaId = 0;
  estados: Estado[] = [];
  clientes: Cliente[] = [];
  tituloForm = "Crear nueva Cita";
  creandoCita = false;
  formValidado = false;
  errorMensaje: string | null = null;

  estadosUbi: string[] = [];
  estadoActual: string = "";
  ciudades: string[] = [];
  esModoActualizacion: boolean = false;

  constructor(
    private clienteService: ClienteService,
    private citaService: CitaService,
    private ubicacionService: UbicacionService,
    private estadoService: EstadoService,
    private cityStateService: CityStateService,
    private alertService: AlertService,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    this.authService.usuario.subscribe(usuario => {
      this.user = usuario;
    });

    this.estadoService.getEstados().subscribe(data => this.estados = data);
    this.clienteService.getClientes().subscribe(data => this.clientes = data);

    this.citaService.$citaId.subscribe(id => {
      this.citaId = id;
      if (id != 0) {
        this.citaService.getCita(id).subscribe(data => {
          this.cita = data;
          this.cita.ubicacion.direccion = "";
          this.cita.ubicacion.codigoPostal = "";
          this.cita.ubicacion.estado = "";
          this.cita.ubicacion.ciudad = "";
          this.estadoActual = data.ubicacion.estado;
          this.onStateChange(this.cita.ubicacion.estado);
        })
        this.tituloForm = "Actualizar datos de la Cita"
        this.esModoActualizacion = true;
      }
      else {
        this.limpiarForm()
      }
    })

    this.cityStateService.getStates().subscribe(data => {
      this.estadosUbi = data;
    });
  }

  onStateChange(state: string): void {
    if (state !== this.estadoActual) {
      this.cita.ubicacion.ciudad = "";
    }

    this.cityStateService.getCitiesByState(state).subscribe(ciudades => {
      this.ciudades = ciudades;

      if (this.cita.ubicacion.ciudad) {
        this.cita.ubicacion.ciudad = this.cita.ubicacion.ciudad;
      }
    });
  }

  FechaMinima(): string {
    const fechaActual = new Date();
    fechaActual.setDate(fechaActual.getDate() + 1);
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
          if (this.citaId != 0) {
            this.actualizarCita();
          }
          else {
            this.crearCita();
          }
        }

        typedForm.classList.add('was-validated');
      }, false);
    });
  }

  getUbication() {
    this.cita.ubicacion.direccion = "null";
    this.cita.ubicacion.ciudad = "null";
    this.cita.ubicacion.estado = "null";
    this.cita.ubicacion.codigoPostal = "0";
  }

  crearCita() {
    this.creandoCita = true;
    this.errorMensaje = null;

    if (this.cita.tipoCita !== "Presencial") {
      this.getUbication();
    }

    this.ubicacionService.crearUbicacion(this.cita.ubicacion).subscribe({
      next: (ubicacion) => {
        this.cita.ubicacionId = ubicacion.id;
        this.cita.empleadoId = this.user.id;

        this.citaService.crearCita(this.cita).subscribe({
          next: () => {
            this.citaService.nuevaCita();
            this.cerrarForm();
            this.creandoCita = false;
            this.alertService.showMessage('Cita creada con éxito.');
          },
          error: (error) => {
            this.errorMensaje = error.error?.message || 'Ocurrió un error al crear la cita.';
            this.creandoCita = false;
            this.formValidado = false;
          },
        });
      },
      error: (error) => {
        this.errorMensaje = error.error?.message || 'Ocurrió un error al crear la ubicación.';
        this.creandoCita = false;
        this.formValidado = false;
      },
    });
  }

  actualizarCita() {
    this.creandoCita = true;
    this.errorMensaje = null;

    if (this.cita.tipoCita !== "Presencial") {
      this.getUbication();
    }

    this.ubicacionService.getUbicacion(this.cita.ubicacionId).subscribe({
      next: (ubicacionOriginal) => {
        if (
          ubicacionOriginal.direccion !== this.cita.ubicacion.direccion ||
          ubicacionOriginal.ciudad !== this.cita.ubicacion.ciudad ||
          ubicacionOriginal.estado !== this.cita.ubicacion.estado ||
          ubicacionOriginal.codigoPostal !== this.cita.ubicacion.codigoPostal
        ) {
          this.ubicacionService.crearUbicacion(this.cita.ubicacion).subscribe({
            next: (ubicacion) => {
              this.cita.ubicacionId = ubicacion.id;
              this.actualizarDatosCita();
            },
            error: (error) => {
              this.errorMensaje = error.error?.message || 'Ocurrió un error al crear la ubicación.';
              this.creandoCita = false;
              this.formValidado = false;
            },
          });
        } else {
          this.actualizarDatosCita();
        }
      },
      error: (error) => {
        this.errorMensaje = error.error?.message || 'No se pudo obtener la información de la ubicación.';
        this.creandoCita = false;
        this.formValidado = false;
      },
    });
  }

  private actualizarDatosCita() {
    this.citaService.actualizarCita(this.citaId, this.user.id, this.cita).subscribe({
      next: () => {
        this.citaService.nuevaCita();
        this.cerrarForm();
        this.creandoCita = false;
        this.alertService.showMessage('Cita actualizada con éxito.');
      },
      error: (error) => {
        this.errorMensaje = error.error?.message || 'No se pudo actualizar la cita. Intente nuevamente.';
        this.creandoCita = false;
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
    this.errorMensaje = null;
    this.cita = new Cita;
    this.tituloForm = "Crear nueva Cita"
    this.formValidado = false;
    this.esModoActualizacion = false;

    const forms = document.querySelectorAll('.needs-validation');
    Array.from(forms).forEach((form: Element) => {
      form.classList.remove('was-validated');
    });
  }
}
