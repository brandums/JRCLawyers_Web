import { Component, OnInit } from '@angular/core';
import { Usuario } from '../Models/Usuario';
import { UsuarioService } from '../services/usuario-service';
import { RolService } from '../services/rol-service';
import { Rol } from '../Models/Rol';
import { AlertService } from '../services/AlertService';
import { AuthService } from '../services/AuthService';

@Component({
  selector: 'app-create-user',
  templateUrl: './create-user.component.html',
  styleUrls: ['./create-user.component.css']
})
export class CreateUserComponent implements OnInit {
  user: any;
  usuario: Usuario = new Usuario;
  usuarioId: number = 0;
  roles: Rol[] = [];
  extencionEmail = "gmail";
  extencionEmail2 = "com";
  contrasenia = "";
  nickEmail = "";
  verContrasenia = false;
  creandoUsuario: boolean = false;
  formValidado = false;
  viewPassword = true;
  errorMensaje: string | null = null;

  tituloForm = "Crear nuevo Empleado";

  constructor(
    private usuarioService: UsuarioService,
    private rolService: RolService,
    private alertService: AlertService,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    this.authService.usuario.subscribe(usuario => {
      this.user = usuario;
    });

    this.rolService.getRoles().subscribe(data => {
      this.roles = data
    })

    this.usuarioService.$usuarioId.subscribe(id => {
      this.usuarioId = id;
      if (id != 0) {
        this.usuarioService.getUsuario(id).subscribe(data => {
          this.usuario = data;
          this.contrasenia = data.contraseña;
          const partesEmail = data.correoElectronico.split('@');
          this.nickEmail = partesEmail[0];
          this.extencionEmail = partesEmail[1].split('.')[0];
          this.extencionEmail2 = partesEmail[1].split('.')[1];
        })
        this.tituloForm = "Actualizar datos del Empleado"
        this.viewPassword = false;
      }
      else {
        this.limpiarForm()
      }
    })
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

  cambiarvistaContrasenia(event: any) {
    this.verContrasenia = event.target.checked;
  }

  unirEmail() {
    this.usuario.correoElectronico = this.nickEmail + "@" + this.extencionEmail + "." + this.extencionEmail2
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
          if (this.usuarioId != 0) {
            this.actualizarUsuario();
          }
          else {
            this.crearUsuario();
          }
        }

        typedForm.classList.add('was-validated');
      }, false);
    });
  }

  crearUsuario() {
    this.creandoUsuario = true;
    this.errorMensaje = null;
    this.unirEmail()
    this.usuario.contraseña = this.contrasenia;
    this.usuarioService.crearUsuario(this.user.id, this.usuario).subscribe(() => {
      this.usuarioService.nuevoUsuario();

      this.cerrarForm();
      this.creandoUsuario = false;
      this.alertService.showMessage('Empleado creado con exito.');
    }, error => {
      this.errorMensaje = error.message;
      this.creandoUsuario = false;
      this.formValidado = false;
    })
  }

  actualizarUsuario() {
    this.unirEmail();
    this.usuarioService.actualizarUsuario(this.usuarioId, this.user.id, this.usuario).subscribe(() => {
      this.usuarioService.nuevoUsuario();

      this.cerrarForm();
      this.creandoUsuario = false;
      this.alertService.showMessage('Empleado actualizado con exito.');
    }, () => {
      this.creandoUsuario = false;
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
    this.nickEmail = "";
    this.usuario = new Usuario;
    this.contrasenia = "";
    this.tituloForm = "Crear nuevo Empleado"
    this.formValidado = false;
    this.viewPassword = true;
    this.errorMensaje = null;

    const forms = document.querySelectorAll('.needs-validation');
    Array.from(forms).forEach((form: Element) => {
      form.classList.remove('was-validated');
    });
  }
}
