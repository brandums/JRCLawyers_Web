import { Component, OnInit } from '@angular/core';
import { CasoService } from '../services/caso-service';
import { Caso } from '../Models/Caso';
import { UbicacionService } from '../services/ubicacion-service';
import { TipoCaso } from '../Models/TipoCaso';
import { Estado } from '../Models/Estado';
import { TipoCasoService } from '../services/tipoCaso-service';
import { EstadoService } from '../services/estado-service';
import { JuzgadoService } from '../services/juzgado-service';
import { Cliente } from '../Models/Cliente';
import { ClienteService } from '../services/cliente-service';
import { CityStateService } from '../services/city-state.service';
import { AlertService } from '../services/AlertService';
import { AuthService } from '../services/AuthService';

@Component({
  selector: 'app-create-case',
  templateUrl: './create-case.component.html',
  styleUrls: ['./create-case.component.css']
})
export class CreateCaseComponent implements OnInit {
  user: any;
  caso: Caso = new Caso;
  casoId = 0;
  tiposCaso: TipoCaso[] = [];
  estados: Estado[] = [];
  clientes: Cliente[] = [];
  creandoCaso = false;
  tituloForm = "Crear nuevo Caso";
  formValidado = false;
  errorMensaje: string | null = null;

  estadosUbi: string[] = [];
  estadoActual: string = "";
  ciudades: string[] = [];
  esModoActualizacion = false;

  constructor(
    private clienteService: ClienteService,
    private casoService: CasoService,
    private ubicacionService: UbicacionService,
    private juzgadoService: JuzgadoService,
    private tipoCasoService: TipoCasoService,
    private estadoService: EstadoService,
    private cityStateService: CityStateService,
    private alertService: AlertService,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    this.authService.usuario.subscribe(usuario => {
      this.user = usuario;
    });

    this.tipoCasoService.getTipoDeCasos().subscribe(data => this.tiposCaso = data);
    this.estadoService.getEstados().subscribe(data => this.estados = data);
    this.clienteService.getClientes().subscribe(data => this.clientes = data);

    this.casoService.$casoId.subscribe(id => {
      this.casoId = id;
      if (id != 0) {
        this.casoService.getCaso(id).subscribe(data => {
          this.caso = data;
          this.estadoActual = data.ubicacion.estado;
          this.onStateChange(this.caso.ubicacion.estado);
        })
        this.tituloForm = "Actualizar datos del Caso";
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
      this.caso.ubicacion.ciudad = "";
    }

    this.cityStateService.getCitiesByState(state).subscribe(ciudades => {
      this.ciudades = ciudades;

      if (this.caso.ubicacion.ciudad) {
        this.caso.ubicacion.ciudad = this.caso.ubicacion.ciudad;
      }
    });
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
    const patron = /^[a-zA-Z0-9\/\-]$/;

    if (event instanceof KeyboardEvent) {
      if (!patron.test(teclaPresionada) && teclaPresionada !== 'Backspace' && teclaPresionada !== 'Delete' && teclaPresionada !== 'ArrowLeft' && teclaPresionada !== 'ArrowRight') {
        event.preventDefault();
      }
    }

    if (event instanceof ClipboardEvent) {
      const textoPegado = event.clipboardData?.getData('text') || '';
      if (!/^[a-zA-Z0-9\/\-]+$/.test(textoPegado)) {
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
    
    const caracteresEspeciales = [' ', '/', '-'];
    
    if (valorActual.length <= minChars && caracteresEspeciales.includes(teclaPresionada)) {
      event.preventDefault();
      return;
    }
    
    if (caracteresEspeciales.includes(valorActual.slice(-1)) && caracteresEspeciales.includes(teclaPresionada)) {
      event.preventDefault();
      return;
    }
  }

  esFormatoCorrecto(numeroExpediente: string): boolean {
    if (!numeroExpediente) {
      return true;
    }
    return numeroExpediente.includes('/') && numeroExpediente.includes('-');
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
          if (this.casoId != 0) {
            this.actualizarCaso();
          }
          else {
            this.crearCaso();
          }
        }

        typedForm.classList.add('was-validated');
      }, false);
    });
  }

  crearCaso() {
    this.creandoCaso = true;
    this.errorMensaje = null;

    this.juzgadoService.crearJuzgado(this.caso.juzgado).subscribe(
      juzgado => {
        this.ubicacionService.crearUbicacion(this.caso.ubicacion).subscribe(
          ubicacion => {
            this.caso.ubicacionId = ubicacion.id;
            this.caso.juzgadoId = juzgado.id;
            this.caso.empleadoId = this.user.id;

            this.casoService.crearCaso(this.caso).subscribe(
              () => {
                this.casoService.nuevoCaso();
                this.cerrarForm();
                this.creandoCaso = false;
                this.alertService.showMessage('Caso creado con éxito.');
              },
              error => {
                this.errorMensaje = error.error?.message || 'Ocurrió un error al crear el caso.';
                this.creandoCaso = false;
                this.formValidado = false;
              }
            );
          },
          error => {
            this.errorMensaje = error.error?.message || 'Ocurrió un error al crear la ubicación.';
            this.creandoCaso = false;
            this.formValidado = false;
          }
        );
      },
      error => {
        this.errorMensaje = error.error?.message || 'Ocurrió un error al crear el juzgado.';
        this.creandoCaso = false;
        this.formValidado = false;
      }
    );
  }

  actualizarCaso() {
    this.creandoCaso = true;
    this.errorMensaje = null;

    this.juzgadoService.getJuzgado(this.caso.juzgadoId).subscribe(
      juzgadoOriginal => {
        let juzgadoCambiado =
          juzgadoOriginal.nombre !== this.caso.juzgado.nombre ||
          juzgadoOriginal.numeroExpediente !== this.caso.juzgado.numeroExpediente;

        if (juzgadoCambiado) {
          this.juzgadoService.crearJuzgado(this.caso.juzgado).subscribe(
            juzgado => {
              this.caso.juzgadoId = juzgado.id;
              this.actualizarUbicacionYCaso();
            },
            error => {
              this.errorMensaje = error.error?.message || 'Ocurrió un error al crear el juzgado.';
              this.creandoCaso = false;
              this.formValidado = false;
            }
          );
        } else {
          this.actualizarUbicacionYCaso();
        }
      },
      error => {
        this.errorMensaje = error.error?.message || 'Ocurrió un error al obtener el juzgado original.';
        this.creandoCaso = false;
        this.formValidado = false;
      }
    );
  }

  private actualizarUbicacionYCaso() {
    this.ubicacionService.getUbicacion(this.caso.ubicacionId).subscribe(
      ubicacionOriginal => {
        let ubicacionCambiada =
          ubicacionOriginal.direccion !== this.caso.ubicacion.direccion ||
          ubicacionOriginal.ciudad !== this.caso.ubicacion.ciudad ||
          ubicacionOriginal.estado !== this.caso.ubicacion.estado ||
          ubicacionOriginal.codigoPostal !== this.caso.ubicacion.codigoPostal;

        if (ubicacionCambiada) {
          this.ubicacionService.crearUbicacion(this.caso.ubicacion).subscribe(
            ubicacion => {
              this.caso.ubicacionId = ubicacion.id;
              this.actualizarDatosCaso();
            },
            error => {
              this.errorMensaje = error.error?.message || 'Ocurrió un error al crear la ubicación.';
              this.creandoCaso = false;
              this.formValidado = false;
            }
          );
        } else {
          this.actualizarDatosCaso();
        }
      },
      error => {
        this.errorMensaje = error.error?.message || 'Ocurrió un error al obtener la ubicación original.';
        this.creandoCaso = false;
        this.formValidado = false;
      }
    );
  }

  private actualizarDatosCaso() {
    this.casoService.actualizarCaso(this.casoId, this.user.id, this.caso).subscribe(
      () => {
        this.casoService.nuevoCaso();
        this.cerrarForm();
        this.creandoCaso = false;
        this.alertService.showMessage('Caso actualizado con éxito.');
      },
      error => {
        this.errorMensaje = error.error?.message || 'Ocurrió un error al actualizar el caso.';
        this.creandoCaso = false;
        this.formValidado = false;
      }
    );
  }

  cerrarForm() {
    const button = document.getElementById('bClose');
    if (button instanceof HTMLElement) {
      button.click()
    }
  }

  limpiarForm() {
    this.caso = new Caso;
    this.tituloForm = "Crear nuevo Caso"
    this.formValidado = false;
    this.errorMensaje = null;
    this.creandoCaso = false;
    this.esModoActualizacion = false;

    const forms = document.querySelectorAll('.needs-validation');
    Array.from(forms).forEach((form: Element) => {
      form.classList.remove('was-validated');
    });
  }
}
