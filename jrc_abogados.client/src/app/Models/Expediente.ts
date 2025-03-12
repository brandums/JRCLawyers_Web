import { Caso } from "./Caso";
import { Cliente } from "./Cliente";
import { TipoExpediente } from "./TipoExpediente";

export class Expediente {
  id: number = 0;
  nombre: string = "";
  tipoExpedienteId = "";
  tipoExpediente: TipoExpediente = new TipoExpediente;
  fechaInicio = "";
  clienteId = "";
  casoId: any = '';
  caso: Caso = new Caso;
  cliente: Cliente = new Cliente;
  empleadoId: number = 0;
  empleado: any;
}
