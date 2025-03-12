import { Component, OnInit } from '@angular/core';
import { Cliente } from '../Models/Cliente';
import { ClienteService } from '../services/cliente-service';
import { AlertService } from '../services/AlertService';
import { AuthService } from '../services/AuthService';
import { Reporte } from '../Models/Reporte';
import { Usuario } from '../Models/Usuario';
import { UsuarioService } from '../services/usuario-service';
import { ReporteService } from '../services/reporte-service';
import { Overlay, OverlayConfig } from '@angular/cdk/overlay';
import { MatSelect } from '@angular/material/select';

@Component({
  selector: 'app-create-report',
  templateUrl: './create-report.component.html'
})
export class CreateReportComponent implements OnInit {
  user: any;
  reporte: Reporte = new Reporte;
  reporteId = 0;
  empleados: Usuario[] = [];
  clientes: Cliente[] = [];
  tituloForm = "Crear nuevo Reporte";
  creandoReporte = false;
  formValidado = false;
  isEdit = false;
  errorMensaje: string | null = null;

  tablas: string[] = ['Cliente', 'Caso', 'Cita', 'Recordatorio', 'Expediente', 'Documento'];
  tablasSeleccionadas: string[] = [];
  opcionFecha: string = "";
  opcionCliente: string = "";
  opcionEmpleado: string = "";

  constructor(
    private clienteService: ClienteService,
    private reporteService: ReporteService,
    private alertService: AlertService,
    private authService: AuthService,
    private usuarioService: UsuarioService,

    private overlay: Overlay
  ) { }

  ngOnInit(): void {
    this.authService.usuario.subscribe(usuario => {
      this.user = usuario;
    });

    this.usuarioService.getUsuarios().subscribe(data => this.empleados = data);
    this.clienteService.getClientes().subscribe(data => this.clientes = data);

    this.reporteService.$reporteId.subscribe(id => {
      this.reporteId = id;
      if (id != 0) {
        this.reporteService.getReporte(id).subscribe(data => {
          this.reporte = data;
          this.isEdit = true;

          if (this.reporte.clienteId) {
            this.opcionCliente = '1';
          }
          else {
            this.opcionCliente = '0'
          }
          if (this.reporte.empleadoId) {
            this.opcionEmpleado = '1';
          }
          else {
            this.opcionEmpleado = '0';
          }
          if (this.reporte.fechaInicio) {
            this.opcionFecha = '1'
          }
          else {
            this.opcionFecha = '0'
          }
          if (this.reporte.tablasSeleccionadas) {
            this.tablasSeleccionadas = this.reporte.tablasSeleccionadas.split(', ').map(tabla => tabla.trim());
          }
        })
        this.tituloForm = "Actualizar datos del Reporte"
      }
      else {
        this.limpiarForm()
      }
    });
  }

  openSelect(select: MatSelect) {
    const overlayConfig = new OverlayConfig({
      scrollStrategy: this.overlay.scrollStrategies.noop()
    });
    select.open();
  }

  onOpcionFechasChange(event: any) {
    this.opcionFecha = event.target.value;
    if (this.opcionFecha) {
      this.reporte.fechaInicio = "";
      this.reporte.fechaFin = "";
    }
  }

  onOpcionClienteChange(event: any) {
    this.opcionCliente = event.target.value;
    if (this.opcionCliente) {
      this.reporte.clienteId = "";
    }
  }

  onOpcionEmpleadoChange(event: any) {
    this.opcionEmpleado = event.target.value;
    if (this.opcionEmpleado) {
      this.reporte.empleadoId = "";
    }
  }

  onSelectionChange(event: any) {
    this.tablasSeleccionadas = event.value;
  }
  
  getTablasSeleccionadasString(): string {
    return this.tablasSeleccionadas.join(', ');
  }


  FechaMinima(): string {
    const fechaMinima = new Date(2024, 8, 24);
    const formatoFecha = fechaMinima.toISOString().split('T')[0];

    return formatoFecha;
  }

  FechaMaxima(): string {
    const fechaActual = new Date();

    const formatoFecha = fechaActual.toISOString().split('T')[0];
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
          if (this.reporteId != 0) {
            this.actualizarReporte();
          }
          else {
            this.crearReporte();
          }
        }

        typedForm.classList.add('was-validated');
      }, false);
    });
  }

  crearReporte() {
    this.creandoReporte = true;
    this.reporte.empleadoAccionId = this.user.id;
    this.reporte.tablasSeleccionadas = this.getTablasSeleccionadasString();

    this.reporteService.crearReporte(this.reporte).subscribe((respuesta) => {
      this.reporteService.nuevoReporte();

      this.cerrarForm();
      this.creandoReporte = false;
      this.alertService.showMessage('Reporte creado con exito.');
    }, (error) => {
      this.errorMensaje = error.message;
      this.creandoReporte = false;
      this.formValidado = false;
    })
  }

  actualizarReporte() {
    this.creandoReporte = true;

    this.reporteService.actualizarReporte(this.reporteId, this.user.id, this.reporte).subscribe(() => {
      this.reporteService.nuevoReporte();

      this.cerrarForm();
      this.creandoReporte = false;
      this.alertService.showMessage('Reporte actualizado con exito.');
    }, (error) => {
      this.errorMensaje = error.message;
      this.creandoReporte = false;
      this.formValidado = false;
    })
  }

  cerrarForm() {
    const button = document.getElementById('bClose');
    if (button instanceof HTMLElement) {
      button.click()
    }
  }

  limpiarForm() {
    this.reporte = new Reporte;
    this.tituloForm = "Crear nuevo Reporte"
    this.formValidado = false;
    this.tablasSeleccionadas = [];
    this.isEdit = false;
    this.opcionFecha = "";
    this.opcionCliente = "";
    this.opcionEmpleado = "";
    this.errorMensaje = null;

    const forms = document.querySelectorAll('.needs-validation');
    Array.from(forms).forEach((form: Element) => {
      form.classList.remove('was-validated');
    });
  }
}
