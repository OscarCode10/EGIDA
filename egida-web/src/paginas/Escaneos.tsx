import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router";
import { api } from "../api/cliente";
import type { Escaneo } from "../api/tipos";
import { useSesion } from "../autenticacion/ContextoSesion";
import { duracion, EstadoEscaneo, fecha } from "../componentes/Insignias";

export function Escaneos() {
  const { esAdmin } = useSesion();
  const { data, isLoading } = useQuery({ queryKey: ["escaneos"], queryFn: () => api<Escaneo[]>("/api/escaneos?limite=50") });

  return (
    <>
      <h1>{esAdmin ? "Escaneos de todos los clientes" : "Mis escaneos"}</h1>
      <section className="tarjeta">
        {isLoading ? (
          <p className="tenue">Cargando…</p>
        ) : !data?.length ? (
          <p className="tenue">No hay escaneos todavía. Lánzalos desde <Link to="/activos">Activos</Link>.</p>
        ) : (
          <div className="tabla-envoltura">
            <table className="tabla">
              <thead>
                <tr>
                  <th>Activo</th>
                  {esAdmin && <th>Propietario</th>}
                  <th>Estado</th>
                  <th>Solicitado</th>
                  <th>Duración</th>
                  <th>Hallazgos (A/M/B/I)</th>
                  <th>Lanzado por</th>
                </tr>
              </thead>
              <tbody>
                {data.map((e) => (
                  <tr key={e.id}>
                    <td><Link to={`/escaneos/${e.id}`}>{e.activo.nombre}</Link></td>
                    {esAdmin && <td>{e.activo.propietario.nombre}</td>}
                    <td><EstadoEscaneo estado={e.estado} motivo={e.motivoFallo} /></td>
                    <td>{fecha(e.solicitadoEn)}</td>
                    <td>{duracion(e.iniciadoEn, e.finalizadoEn)}</td>
                    <td>
                      <span className="txt-alta">{e.conteoPorSeveridad.alta}</span> /{" "}
                      <span className="txt-media">{e.conteoPorSeveridad.media}</span> /{" "}
                      <span className="txt-baja">{e.conteoPorSeveridad.baja}</span> /{" "}
                      <span className="txt-info">{e.conteoPorSeveridad.informativa}</span>
                    </td>
                    <td>{e.solicitadoPor.nombre} <span className="tenue">({e.solicitadoPor.rol})</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </>
  );
}
