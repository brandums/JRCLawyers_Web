import { Cliente } from "./Cliente";

export class Reporte {
  id: number = 0;
  nombre: string = "";
  descripcion: string = "";
  path = "";
  tablasSeleccionadas = "";
  fechaInicio: any = "";
  fechaFin: any = "";
  fechaGeneracion: any = "";
  clienteId: any = "";
  cliente: Cliente = new Cliente;
  [key: string]: any;
  empleadoId: any = "";
  empleado: any;
  empleadoAccionId: number = 0;
  empleadoAccion: any;
}
