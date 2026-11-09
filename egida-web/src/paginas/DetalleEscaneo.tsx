import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useParams } from "react-router";
import { api, ErrorApi } from "../api/cliente";
import type { Escaneo, Hallazgo, Severidad } from "../api/tipos";
import { useSesion } from "../autenticacion/ContextoSesion";
import { duracion, EstadoCorreo, EstadoEscaneo, fecha } from "../componentes/Insignias";
import { TablaHallazgos, type Orden } from "../componentes/TablaHallazgos";

export function DetalleEscaneo() {
  const { id } = useParams();
  const { esAdmin } = useSesion();
  const [severidad, setSeveridad] = useState<Severidad | "">("");
  const [orden, setOrden] = useState<Orden>("severidad");

  const escaneo = useQuery({ queryKey: ["escaneo", id], queryFn: () => api<Escaneo>(`/api/escaneos/${id}`), retry: false });
  const terminado = escaneo.data?.estado === "Terminado";
  const hallazgos = useQuery({
    queryKey: ["hallazgos", id, severidad, orden],
    queryFn: () =>
      api<Hallazgo[]>(`/api/escaneos/${id}/hallazgos?orden=${orden}${severidad ? `&severidad=${severidad}` : ""}`),
    enabled: terminado,
  });

  if (escaneo.isLoading) return <p className="tenue">Cargando…</p>;
  if (escaneo.error) {
    const noExiste = escaneo.error instanceof ErrorApi && escaneo.error.estado === 404;
    return <div className="aviso error">{noExiste ? "El escaneo no existe o no tienes acceso." : "No se pudo cargar el escaneo."}</div>;
  }
  const e = escaneo.data!;
  const c = e.conteoPorSeveridad;
  const pendientesIA = hallazgos.data?.filter((h) => h.ia.estado === "Pendiente").length ?? 0;

  return (
    <>
      <p className="migas"><Link to="/escaneos">Escaneos</Link> / {e.activo.nombre}</p>
      <h1>
        Escaneo de {e.activo.nombre} <EstadoEscaneo estado={e.estado} motivo={e.motivoFallo} />
      </h1>

      <section className="tarjeta resumen">
        <div><span className="etiqueta">URL</span>{e.activo.url}</div>
        {esAdmin && <div><span className="etiqueta">Propietario</span>{e.activo.propietario.nombre}</div>}
        <div><span className="etiqueta">Lanzado por</span>{e.solicitadoPor.nombre} ({e.solicitadoPor.rol})</div>
        <div><span className="etiqueta">Solicitado</span>{fecha(e.solicitadoEn)}</div>
        <div><span className="etiqueta">Duración</span>{duracion(e.iniciadoEn, e.finalizadoEn)}</div>
        {e.alerta && <div><span className="etiqueta">Alerta</span><EstadoCorreo estado={e.alerta.estadoEnvio} /></div>}
      </section>

      {(e.estado === "Pendiente" || e.estado === "EnCurso") && (
        <div className="aviso info">
          <span className="giro" /> El escaneo está {e.estado === "Pendiente" ? "en cola" : "en curso"} (perfil demo: semillas, spider,
          análisis pasivo y pruebas activas acotadas). Esta página se actualiza sola; puedes seguir navegando.
        </div>
      )}
      {e.estado === "Fallido" && <div className="aviso error">El escaneo falló: {e.motivoFallo}</div>}

      {terminado && (
        <>
          <section className="tarjetas">
            <div className="tarjeta indicador sev-borde-alta"><span className="etiqueta">Alta</span><strong className="txt-alta">{c.alta}</strong></div>
            <div className="tarjeta indicador sev-borde-media"><span className="etiqueta">Media</span><strong className="txt-media">{c.media}</strong></div>
            <div className="tarjeta indicador sev-borde-baja"><span className="etiqueta">Baja</span><strong className="txt-baja">{c.baja}</strong></div>
            <div className="tarjeta indicador sev-borde-info"><span className="etiqueta">Informativa</span><strong className="txt-info">{c.informativa}</strong></div>
          </section>

          {pendientesIA > 0 && (
            <div className="aviso aviso-ia">
              {pendientesIA} hallazgo(s) con análisis IA pendiente: el módulo de IA no respondió. Se mantiene la severidad original
              y se reintentará automáticamente.
            </div>
          )}

          <section className="tarjeta">
            <h2>Hallazgos</h2>
            {e.totalHallazgos === 0 ? (
              <div className="aviso ok">No se encontraron vulnerabilidades en este escaneo.</div>
            ) : hallazgos.isLoading ? (
              <p className="tenue">Cargando hallazgos…</p>
            ) : (
              <TablaHallazgos hallazgos={hallazgos.data ?? []} severidad={severidad} orden={orden}
                alCambiarSeveridad={setSeveridad} alCambiarOrden={setOrden} />
            )}
          </section>
        </>
      )}
    </>
  );
}
