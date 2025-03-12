import { Component, Input, OnInit } from '@angular/core';
import { Cliente } from '../Models/Cliente';
import { AlertService } from '../services/AlertService';
import { Documento } from '../Models/Documento';
import { TipoDocumento } from '../Models/TipoDocumento';
import { TipoDocumentoService } from '../services/tipoDocumento-service';
import { DocumentoService } from '../services/document-service';
import { AuthService } from '../services/AuthService';

@Component({
  selector: 'app-create-document',
  templateUrl: './create-document.component.html'
})
export class CreateDocumentComponent implements OnInit {
  user: any;
  documento: Documento = new Documento;
  documentoId = 0;
  clientes: Cliente[] = [];
  creandoDocumento = false;
  tituloForm = "Crear nuevo Documento";
  formValidado = false;
  errorMensaje: string | null = null;
  archivoSeleccionado: File | null = null;

  @Input() expedienteId!: number; 

  constructor(
    private documentoService: DocumentoService,
    private alertService: AlertService,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    this.authService.usuario.subscribe(usuario => {
      this.user = usuario;
    });

    this.documentoService.$documentoId.subscribe(id => {
      this.documentoId = id;
      if (id != 0) {
        this.documentoService.getDocumento(id).subscribe(data => {
          this.documento = data;
        })
        this.tituloForm = "Actualizar datos del Documento"
      }
      else {
        this.limpiarForm()
      }
    })
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

  onFileSelected(event: any) {
    const file: File = event.target.files[0];
    const fileType = file.type;

    if (fileType === 'application/pdf') {
      this.archivoSeleccionado = file;
    } else {
      this.archivoSeleccionado = null;
      alert('Por favor, seleccione un archivo en formato PDF.');
      event.target.value = '';
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
          if (this.documentoId != 0) {
            this.actualizarDocumento();
          }
          else {
            this.crearDocumento();
          }
        }

        typedForm.classList.add('was-validated');
      }, false);
    });
  }

  crearDocumento() {
    this.creandoDocumento = true;
    this.errorMensaje = null;

    this.documento.expedienteId = this.expedienteId;
    this.documento.empleadoId = this.user.id;

    const formData = new FormData();
    formData.append('file', this.archivoSeleccionado as Blob);
    formData.append('nombre', this.documento.nombre);
    formData.append('descripcion', this.documento.descripcion);
    formData.append('expedienteId', this.documento.expedienteId.toString());
    formData.append('empleadoId', this.user.id);

    this.documentoService.crearDocumento(formData).subscribe({
      next: (response) => {
        this.documentoService.nuevoDocumento();
        this.cerrarForm();
        this.creandoDocumento = false;
        this.alertService.showMessage('Documento creado con éxito.');
      },
      error: (error) => {
        this.errorMensaje = error.error?.message || 'Ocurrió un error al crear el documento.';
        this.creandoDocumento = false;
        this.formValidado = false;
      },
    });
  }

  actualizarDocumento() {
    this.creandoDocumento = true;
    this.errorMensaje = null;

    this.documentoService.actualizarDocumento(this.documentoId, this.user.id, this.documento).subscribe({
      next: () => {
        this.documentoService.nuevoDocumento();
        this.cerrarForm();
        this.creandoDocumento = false;
        this.alertService.showMessage('Documento actualizado con éxito.');
      },
      error: (error) => {
        this.errorMensaje = error.error?.message || 'No se pudo actualizar el documento. Intente nuevamente.';
        this.creandoDocumento = false;
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
    this.documento = new Documento;
    this.tituloForm = "Crear nuevo Documento"
    this.formValidado = false;
    this.errorMensaje = null;

    const forms = document.querySelectorAll('.needs-validation');
    Array.from(forms).forEach((form: Element) => {
      form.classList.remove('was-validated');
    });
  }
}
