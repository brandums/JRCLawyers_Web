import { Cliente } from "./Cliente";
import { Expediente } from "./Expediente";

export class Documento {
  id: number = 0;
  nombre: string = "";
  path: string = "";
  fechaInicio: string = "";
  descripcion: string = "";
  clienteId = "";
  cliente: Cliente = new Cliente;
  expedienteId = 0;
  expediente: Expediente = new Expediente;
  empleadoId: number = 0;
  empleado: any;
}
