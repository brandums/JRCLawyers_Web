import { Estado } from "./Estado";
import { Juzgado } from "./Juzgado";
import { TipoCaso } from "./TipoCaso";
import { Ubicacion } from "./Ubicacion";

export class Caso {
  id: number = 0;
  tipoCasoId = "";
  tipoCaso: TipoCaso = new TipoCaso;
  juzgadoId: number = 0;
  juzgado: Juzgado = new Juzgado;
  ubicacionId: number = 0;
  ubicacion: Ubicacion = new Ubicacion;
  descripcion: string = "";
  fechaInicio = "";
  fechaTermino = "";
  estadoId = "";
  estado: Estado = new Estado;
  empleadoId: number = 0;
  empleado: any;
  clienteId = "";
  cliente: any;
  [key: string]: any;
}
