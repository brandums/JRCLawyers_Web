import { Component, OnInit } from '@angular/core';
import { UsuarioService } from '../services/usuario-service';
import { Usuario } from '../Models/Usuario';
import { AlertService } from '../services/AlertService';
import { AuthService } from '../services/AuthService';

@Component({
  selector: 'app-user-list',
  templateUrl: './user-list.component.html'
})
export class UserListComponent implements OnInit {
  user: any;
  usuarios: Usuario[] = [];
  buscarPalabra: string = '';
  usuarioSeleccionado: Usuario | null = null;

  paginaActual: number = 1;
  usuariosPorPagina: number = 10;

  campoFiltro: string = 'nombre';

  constructor(
    private usuarioService: UsuarioService,
    private alertService: AlertService,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    this.authService.usuario.subscribe(usuario => {
      this.user = usuario;
    });

    this.getUsuarios();

    this.usuarioService.$listaUsuarios.subscribe(data => {
      this.usuarios = data;
    })
  }

  seleccionarUsuario(usuarioId: number) {
    this.usuarioService.seleccionarUsuario(usuarioId);
  }

  verUsuario(usuario: Usuario): void {
    this.usuarioSeleccionado = usuario;
  }

  editarUsuario(usuarioId: number): void {
    this.seleccionarUsuario(usuarioId);
  }

  getUsuarios(): void {
    this.usuarioService.getUsuarios()
      .subscribe((clientes: Usuario[]) => this.usuarios = clientes);
  }

  get filtroUsuarios(): Usuario[] {
    return this.usuarios.filter(usuario => {
      const valor = this.obtenerValor(usuario, this.campoFiltro);
      return valor && valor.toString().toLowerCase().includes(this.buscarPalabra.toLowerCase());
    });
  }

  obtenerValor(obj: any, ruta: string): any {
    return ruta.split('.').reduce((o, i) => o?.[i], obj);
  }

  eliminarUsuario(id: number): void {
    this.usuarioService.eliminarUsuario(id, this.user.id)
      .subscribe(() => {
        this.usuarios = this.usuarios.filter(usuario => usuario.id !== id);
        this.alertService.showMessage('Empleado eliminado con exito.');
      });
  }

  // Métodos para la paginación
  get usuariosPaginados(): Usuario[] {
    const inicio = (this.paginaActual - 1) * this.usuariosPorPagina;
    const fin = inicio + this.usuariosPorPagina;
    return this.filtroUsuarios.slice(inicio, fin);
  }

  cambiarPagina(pagina: number): void {
    if (pagina >= 1 && pagina <= this.totalPaginas) {
      this.paginaActual = pagina;
    }
  }

  get totalPaginas(): number {
    return Math.ceil(this.filtroUsuarios.length / this.usuariosPorPagina);
  }
}

