import type { EstadoEnvio, EstadoEscaneo as Estado, Hallazgo, Severidad } from "../api/tipos";

const ROTULO_ESTADO: Record<Estado, string> = {
  Pendiente: "Pendiente",
  EnCurso: "En curso",
  Terminado: "Terminado",
  Fallido: "Fallido",
};

export function InsigniaSeveridad({ severidad }: { severidad: Severidad }) {
  return <span className={`insignia sev-${severidad.toLowerCase()}`}>{severidad}</span>;
}

/** FR-026: estilo distinto al de severidad (contorno + icono) y rotulado "Prioridad IA". */
export function InsigniaPrioridadIA({ ia }: { ia: Hallazgo["ia"] }) {
  if (ia.estado === "Pendiente") {
    return <span className="ia ia-pendiente" title="El módulo de IA no estaba disponible; se reintentará solo">⏳ análisis IA pendiente</span>;
  }
  return (
    <span className="ia-celda">
      <span className={`ia ia-${ia.prioridad?.toLowerCase()}`} title={ia.justificacion ?? ""}>
        ✦ {ia.prioridad}
      </span>
      {ia.esPosibleFalsoPositivo && (
        <span className="fp" title={`Probabilidad de falso positivo: ${Math.round((ia.probabilidadFalsoPositivo ?? 0) * 100)} %`}>
          posible falso positivo
        </span>
      )}
    </span>
  );
}

export function EstadoEscaneo({ estado, motivo }: { estado: Estado; motivo?: string | null }) {
  return (
    <span className={`estado estado-${estado.toLowerCase()}`} title={motivo ?? ""}>
      {(estado === "Pendiente" || estado === "EnCurso") && <span className="giro" aria-hidden />}
      {ROTULO_ESTADO[estado]}
      {estado === "Fallido" && motivo ? ` – ${motivo}` : ""}
    </span>
  );
}

export function EstadoCorreo({ estado }: { estado: EstadoEnvio }) {
  const texto = { Pendiente: "Correo pendiente", Enviado: "Correo enviado", Fallido: "Correo fallido" }[estado];
  return <span className={`estado envio-${estado.toLowerCase()}`}>{texto}</span>;
}

export const fecha = (iso: string | null | undefined) =>
  iso ? new Date(iso).toLocaleString("es-CO", { dateStyle: "medium", timeStyle: "short" }) : "—";

export function duracion(inicio: string | null, fin: string | null) {
  if (!inicio || !fin) return "—";
  const s = Math.round((new Date(fin).getTime() - new Date(inicio).getTime()) / 1000);
  return s >= 60 ? `${Math.floor(s / 60)} min ${s % 60} s` : `${s} s`;
}
