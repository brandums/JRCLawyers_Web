import { Rol } from "./Rol";

export class Usuario {
  id: number = 0;
  nombre: string = "";
  correoElectronico: string = "";
  contraseña: string = "";
  rolId: any = "";
  rol: Rol = new Rol;
  [key: string]: any;
}
