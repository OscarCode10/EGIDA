import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router";
import { api } from "../api/cliente";
import type { Dashboard as DatosDashboard } from "../api/tipos";
import { useSesion } from "../autenticacion/ContextoSesion";
import { EstadoCorreo, EstadoEscaneo, fecha } from "../componentes/Insignias";

export function Dashboard() {
  const { esAdmin } = useSesion();
  const { data, isLoading, error } = useQuery({ queryKey: ["dashboard"], queryFn: () => api<DatosDashboard>("/api/dashboard") });

  if (isLoading) return <p className="tenue">Cargando…</p>;
  if (error || !data) return <div className="aviso error">No se pudo cargar el panel.</div>;

  const s = data.hallazgosPorSeveridad;
  const total = s.alta + s.media + s.baja + s.informativa;

  return (
    <>
      <h1>{esAdmin ? "Panel global de la plataforma" : "Mi panel"}</h1>

      {data.totalActivos === 0 && !esAdmin && (
        <div className="aviso info">
          Aún no tienes activos. <Link to="/activos">Registra tu primer activo</Link> para escanearlo.
        </div>
      )}

      <section className="tarjetas">
        <div className="tarjeta indicador">
          <span className="etiqueta">Activos</span>
          <strong>{data.totalActivos}</strong>
        </div>
        <div className="tarjeta indicador sev-borde-alta">
          <span className="etiqueta">Hallazgos Alta</span>
          <strong className="txt-alta">{s.alta}</strong>
        </div>
        <div className="tarjeta indicador sev-borde-media">
          <span className="etiqueta">Hallazgos Media</span>
          <strong className="txt-media">{s.media}</strong>
        </div>
        <div className="tarjeta indicador sev-borde-baja">
          <span className="etiqueta">Hallazgos Baja</span>
          <strong className="txt-baja">{s.baja}</strong>
        </div>
        <div className="tarjeta indicador sev-borde-info">
          <span className="etiqueta">Informativos</span>
          <strong className="txt-info">{s.informativa}</strong>
        </div>
      </section>

      {total > 0 && (
        <div className="barra-sev" aria-label="Distribución de hallazgos por severidad">
          {(["alta", "media", "baja", "informativa"] as const).map((k) =>
            s[k] > 0 ? <span key={k} className={`seg seg-${k}`} style={{ flexGrow: s[k] }} title={`${k}: ${s[k]}`} /> : null,
          )}
        </div>
      )}

      <div className="dos-columnas">
        <section className="tarjeta">
          <h2>Escaneos recientes</h2>
          {data.escaneosRecientes.length === 0 ? (
            <p className="tenue">Sin escaneos todavía.</p>
          ) : (
            <ul className="lista">
              {data.escaneosRecientes.map((e) => (
                <li key={e.id}>
                  <Link to={`/escaneos/${e.id}`}>{e.activo.nombre}</Link>
                  {esAdmin && <span className="tenue"> · {e.activo.propietario.nombre}</span>}
                  <span className="tenue"> · {fecha(e.solicitadoEn)}</span>
                  <span className="derecha"><EstadoEscaneo estado={e.estado} motivo={e.motivoFallo} /></span>
                </li>
              ))}
            </ul>
          )}
        </section>
        <section className="tarjeta">
          <h2>Últimas alertas</h2>
          {data.alertasRecientes.length === 0 ? (
            <p className="tenue">Sin alertas.</p>
          ) : (
            <ul className="lista">
              {data.alertasRecientes.map((a) => (
                <li key={a.id}>
                  <Link to={`/escaneos/${a.escaneoId}`}>{a.resumen}</Link>
                  <span className="tenue"> · {fecha(a.creadaEn)}</span>
                  <span className="derecha"><EstadoCorreo estado={a.estadoEnvio} /></span>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </>
  );
}
