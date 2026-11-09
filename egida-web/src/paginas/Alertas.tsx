import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router";
import { api } from "../api/cliente";
import type { Alerta } from "../api/tipos";
import { useSesion } from "../autenticacion/ContextoSesion";
import { EstadoCorreo, fecha } from "../componentes/Insignias";

export function Alertas() {
  const { esAdmin } = useSesion();
  const { data, isLoading } = useQuery({ queryKey: ["alertas"], queryFn: () => api<Alerta[]>("/api/alertas?limite=50") });

  return (
    <>
      <h1>{esAdmin ? "Alertas de la plataforma" : "Mis alertas"}</h1>
      <p className="tenue">Se genera una alerta por correo cuando un escaneo termina con al menos un hallazgo de severidad Alta.</p>
      <section className="tarjeta">
        {isLoading ? (
          <p className="tenue">Cargando…</p>
        ) : !data?.length ? (
          <p className="tenue">No hay alertas.</p>
        ) : (
          <div className="tabla-envoltura">
            <table className="tabla">
              <thead>
                <tr>
                  <th>Fecha</th>
                  <th>Activo</th>
                  {esAdmin && <th>Destinatario</th>}
                  <th>Altas</th>
                  <th>Posibles FP (IA)</th>
                  <th>Resumen</th>
                  <th>Correo</th>
                </tr>
              </thead>
              <tbody>
                {data.map((a) => (
                  <tr key={a.id}>
                    <td>{fecha(a.creadaEn)}</td>
                    <td>{a.activo.nombre}</td>
                    {esAdmin && <td>{a.destinatario.nombre}</td>}
                    <td className="txt-alta"><strong>{a.totalAltas}</strong></td>
                    <td>{a.totalPosiblesFp}</td>
                    <td><Link to={`/escaneos/${a.escaneoId}`}>{a.resumen}</Link></td>
                    <td><EstadoCorreo estado={a.estadoEnvio} /></td>
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
