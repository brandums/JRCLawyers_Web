import { Component, OnInit } from '@angular/core';
import { ClienteService } from '../services/cliente-service';
import { Cliente } from '../Models/Cliente';
import { UbicacionService } from '../services/ubicacion-service';
import { CityStateService } from '../services/city-state.service';
import { AlertService } from '../services/AlertService';
import { AuthService } from '../services/AuthService';

@Component({
  selector: 'app-create-client',
  templateUrl: './create-client.component.html'
})
export class CreateClientComponent implements OnInit {
  user: any;
  cliente: Cliente = new Cliente;
  clienteId = 0;
  extencionEmail = "gmail";
  extencionEmail2 = "com";
  tituloForm = "Crear nuevo Cliente";
  nickEmail = ""
  creandoCliente = false;
  formValidado = false;
  errorMensaje: string | null = null;

  estados: string[] = [];
  estadoActual: string = "";
  ciudades: string[] = [];

  constructor(
    private clienteService: ClienteService,
    private ubicacionService: UbicacionService,
    private cityStateService: CityStateService,
    private alertService: AlertService,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    this.authService.usuario.subscribe(usuario => {
      this.user = usuario;
    });

    this.clienteService.$clienteId.subscribe(id => {
      this.clienteId = id;
      if (id != 0) {
        this.clienteService.getCliente(id).subscribe(data => {
          this.cliente = data;
          const partesEmail = data.correoElectronico.split('@');
          this.nickEmail = partesEmail[0];
          this.extencionEmail = partesEmail[1].split('.')[0];
          this.extencionEmail2 = partesEmail[1].split('.')[1];
          this.telefono = data.telefono;
          this.estadoActual = data.ubicacion.estado;
          this.onStateChange(this.cliente.ubicacion.estado);
        })
        this.tituloForm = "Actualizar datos del Cliente"
      }
      else {
        this.limpiarForm();
      }
    })

    this.cityStateService.getStates().subscribe(data => {
      this.estados = data;
    });
  }

  onStateChange(state: string): void {
    if (state !== this.estadoActual) {
      this.cliente.ubicacion.ciudad = "";
    }

    this.cityStateService.getCitiesByState(state).subscribe(ciudades => {
      this.ciudades = ciudades;
      
      if (this.cliente.ubicacion.ciudad) {
        this.cliente.ubicacion.ciudad = this.cliente.ubicacion.ciudad;
      }
    });
  }

  FechaMinima(): string {
    const fechaActual = new Date();
    fechaActual.setFullYear(fechaActual.getFullYear() - 95);
    const formatoFecha = fechaActual.toISOString().split('T')[0];

    return formatoFecha;
  }

  FechaMaxima(): string {
    const fechaActual = new Date();
    fechaActual.setFullYear(fechaActual.getFullYear() - 18);
    const formatoFecha = fechaActual.toISOString().split('T')[0];

    return formatoFecha;
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

  validarEmail2(event: KeyboardEvent | ClipboardEvent) {
    const input = event.target as HTMLInputElement;
    const inputValue = input.value;

    const regex = /^[A-Za-z]*$/;

    if (event instanceof KeyboardEvent) {
      if (!regex.test(event.key) && event.key !== 'Backspace' && event.key !== 'Delete') {
        event.preventDefault();
      }
    }

    if (event instanceof ClipboardEvent) {
      const clipboardData = event.clipboardData;
      if (clipboardData) {
        const pastedText = clipboardData.getData('text');

        if (!regex.test(pastedText)) {
          event.preventDefault();
        }
      }
    }
  }


  _telefono: string = '';

  get telefono(): string {
    return this.formatPhone(this._telefono);
  }
  
  set telefono(value: string) {
    this._telefono = value.replace(/\D/g, '');
  }
  
  limitarNumeros(event: KeyboardEvent | ClipboardEvent) {
    const teclaPresionada = (event as KeyboardEvent).key;
    const patron = /^[0-9]$/;

    if (event instanceof ClipboardEvent) {
      event.preventDefault();
      return;
    }

    if (event instanceof KeyboardEvent) {
      if (!patron.test(teclaPresionada) &&
        teclaPresionada !== 'Backspace' &&
        teclaPresionada !== 'Delete' &&
        teclaPresionada !== 'ArrowLeft' &&
        teclaPresionada !== 'ArrowRight') {
        event.preventDefault();
      }
    }
  }

  validarTeclas(event: KeyboardEvent) {
    const input = event.target as HTMLInputElement;
    const teclaPresionada = event.key;
    const valorActual = input.value;
    const posicionCursor = input.selectionStart ?? 0;

    const teclasEspeciales = ['Backspace', 'Delete', 'ArrowLeft', 'ArrowRight', 'Tab'];
    
    const patron = /^[0-9() \-]$/;
    
    if (!patron.test(teclaPresionada) && !teclasEspeciales.includes(teclaPresionada)) {
      event.preventDefault();
      return;
    }

    if (teclaPresionada === '(' && posicionCursor !== 0) {
      event.preventDefault();
      return;
    }

    if (teclaPresionada === ')' && posicionCursor !== 3) {
      event.preventDefault();
      return;
    }
    
    if (teclaPresionada === ' ' && posicionCursor !== 4) {
      event.preventDefault();
      return;
    }

    if (teclaPresionada === '-' && posicionCursor !== 9) {
      event.preventDefault();
      return;
    }

    if (/[0-9]/.test(teclaPresionada) && (posicionCursor === 0 || posicionCursor === 3 || posicionCursor === 4 || posicionCursor === 9)) {
      event.preventDefault();
      return;
    }
    
    if (valorActual.length >= 14 && !teclasEspeciales.includes(teclaPresionada)) {
      event.preventDefault();
      return;
    }
  }

  
  formatPhone(value: string): string {
    if (value.length != 0) {
      if (value.length <= 2) {
        return `(${value}`;
      } else if (value.length <= 6) {
        return `(${value.slice(0, 2)}) ${value.slice(2)}`;
      } else {
        return `(${value.slice(0, 2)}) ${value.slice(2, 6)}-${value.slice(6, 10)}`;
      }
    }
    return ""
  }

  validarTexto(event: KeyboardEvent, minChars: number) {
    const teclaPresionada = event.key;
    const valorActual = (event.target as HTMLInputElement).value;
    
    const patron = /^[a-zA-Z\s]*$/;

    if (valorActual.length <= minChars && teclaPresionada === ' ') {
      event.preventDefault();
      return;
    }
    
    if (valorActual.slice(-1) === ' ' && teclaPresionada === ' ') {
      event.preventDefault();
      return;
    }
    
    if (!patron.test(teclaPresionada)) {
      event.preventDefault();
    }
  }

  validarDireccion(event: KeyboardEvent, minChars: number) {
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

  unirEmail() {
    this.cliente.correoElectronico = this.nickEmail + "@" + this.extencionEmail + "." + this.extencionEmail2
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
          if (!this.creandoCliente) {
            this.formValidado = true;
            if (this.clienteId != 0) {
              this.actualizarCliente();
            }
            else {
              this.crearCliente();
            }
          }
        }

        typedForm.classList.add('was-validated');
      }, false);
    });
  }

  crearCliente() {
    this.creandoCliente = true;
    this.errorMensaje = null;
    this.unirEmail()
    this.cliente.telefono = this.telefono;

    this.ubicacionService.crearUbicacion(this.cliente.ubicacion).subscribe(data => {
      this.cliente.ubicacionId = data.id;
      this.cliente.empleadoId = this.user.id;

      this.clienteService.crearCliente(this.cliente).subscribe({
        next: (response) => {
          this.clienteService.nuevoCliente();

          this.cerrarForm();
          this.creandoCliente = false;
          this.alertService.showMessage('Cliente creado con exito.');
        },
        error: (error) => {
          this.errorMensaje = error.error?.message || 'Ocurrió un error al crear el cliente.';
          this.creandoCliente = false;
          this.formValidado = false;
        },
      });
    })
  }

  actualizarCliente() {
    this.errorMensaje = null;
    this.unirEmail();
    this.cliente.telefono = this.telefono;

    this.ubicacionService.getUbicacion(this.cliente.ubicacionId).subscribe({
      next: (ubicacionOriginal) => {
        if (
          ubicacionOriginal.direccion !== this.cliente.ubicacion.direccion ||
          ubicacionOriginal.ciudad !== this.cliente.ubicacion.ciudad ||
          ubicacionOriginal.estado !== this.cliente.ubicacion.estado ||
          ubicacionOriginal.codigoPostal !== this.cliente.ubicacion.codigoPostal
        ) {
          this.ubicacionService.crearUbicacion(this.cliente.ubicacion).subscribe({
            next: (ubicacion) => {
              this.cliente.ubicacionId = ubicacion.id;
              this.actualizarDatosCliente();
            },
            error: (error) => {
              this.errorMensaje = error.error?.message || 'Ocurrió un error al crear la ubicación.';
              this.creandoCliente = false;
              this.formValidado = false;
            },
          });
        } else {
          this.actualizarDatosCliente();
        }
      },
      error: (error) => {
        this.errorMensaje = error.error?.message || 'No se pudo obtener la información de la ubicación.';
        this.creandoCliente = false;
        this.formValidado = false;
      },
    });
  }

  private actualizarDatosCliente() {
    this.clienteService.actualizarCliente(this.clienteId, this.user.id, this.cliente).subscribe({
      next: () => {
        this.clienteService.nuevoCliente();
        this.cerrarForm();
        this.creandoCliente = false;
        this.alertService.showMessage('Cliente actualizado con éxito.');
      },
      error: (error) => {
        this.errorMensaje = error.error?.message || 'No se pudo actualizar el cliente. Intente nuevamente.';
        this.creandoCliente = false;
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
    this.nickEmail = "";
    this.cliente = new Cliente;
    this.tituloForm = "Crear nuevo Cliente"
    this.formValidado = false;
    this.errorMensaje = null;
    this._telefono = "";

    const forms = document.querySelectorAll('.needs-validation');
    Array.from(forms).forEach((form: Element) => {
      form.classList.remove('was-validated');
    });
  }
}
