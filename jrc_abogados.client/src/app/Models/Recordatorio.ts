import { Cliente } from "./Cliente";

export class Recordatorio {
  id: number = 0;
  titulo: string = "";
  descripcion: string = "";
  fecha = "";
  hora = "";
  clienteId: any = "";
  cliente: Cliente = new Cliente;
  [key: string]: any;
  empleadoId: number = 0;
  empleado: any;
}
