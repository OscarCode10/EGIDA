// Tipos de contracts/api-rest.md

export type Rol = "Administrador" | "Cliente";
export type Severidad = "Alta" | "Media" | "Baja" | "Informativa";
export type PrioridadIA = "Alta" | "Media" | "Baja";
export type EstadoEscaneo = "Pendiente" | "EnCurso" | "Terminado" | "Fallido";
export type EstadoEnvio = "Pendiente" | "Enviado" | "Fallido";

export interface Usuario {
  id: string;
  nombre: string;
  correo: string;
  rol: Rol;
}

export interface RespuestaLogin {
  token: string;
  expiraEn: string;
  usuario: Usuario;
}

export interface Persona {
  id: string;
  nombre: string;
}

export interface ObjetivoPermitido {
  nombre: string;
  url: string;
}

export interface Activo {
  id: string;
  nombre: string;
  url: string;
  propietario: Persona;
  creadoEn: string;
  ultimoEscaneo: { id: string; estado: EstadoEscaneo; finalizadoEn: string | null } | null;
}

export interface ConteoSeveridad {
  alta: number;
  media: number;
  baja: number;
  informativa: number;
}

export interface Escaneo {
  id: string;
  activo: { id: string; nombre: string; url: string; propietario: Persona };
  estado: EstadoEscaneo;
  solicitadoEn: string;
  iniciadoEn: string | null;
  finalizadoEn: string | null;
  motivoFallo: string | null;
  totalHallazgos: number;
  conteoPorSeveridad: ConteoSeveridad;
  solicitadoPor: { id: string; nombre: string; rol: Rol };
  alerta: { id: string; estadoEnvio: EstadoEnvio } | null;
}

export interface Hallazgo {
  id: string;
  nombre: string;
  descripcion: string;
  severidad: Severidad;
  categoriaOwasp: string;
  origenCategoria: string;
  urlAfectada: string;
  recomendacion: string;
  metodoHttp: string | null;
  parametro: string | null;
  evidencia: string | null;
  cweId: number | null;
  ocurrencias: number;
  ia: {
    estado: "Analizado" | "Pendiente";
    prioridad: PrioridadIA | null;
    probabilidadFalsoPositivo: number | null;
    esPosibleFalsoPositivo: boolean | null;
    justificacion: string | null;
    versionModelo: string | null;
  };
}

export interface Alerta {
  id: string;
  escaneoId: string;
  activo: { id: string; nombre: string };
  creadaEn: string;
  totalAltas: number;
  totalPosiblesFp: number;
  resumen: string;
  estadoEnvio: EstadoEnvio;
  destinatario: Persona;
}

export interface Dashboard {
  totalActivos: number;
  escaneosRecientes: Escaneo[];
  hallazgosPorSeveridad: ConteoSeveridad;
  alertasRecientes: Alerta[];
}
