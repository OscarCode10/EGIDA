import { Fragment, useState } from "react";
import type { Hallazgo, Severidad } from "../api/tipos";
import { InsigniaPrioridadIA, InsigniaSeveridad } from "./Insignias";

export type Orden = "severidad" | "prioridadIA";

interface Props {
  hallazgos: Hallazgo[];
  severidad: Severidad | "";
  orden: Orden;
  alCambiarSeveridad: (s: Severidad | "") => void;
  alCambiarOrden: (o: Orden) => void;
}

export function TablaHallazgos({ hallazgos, severidad, orden, alCambiarSeveridad, alCambiarOrden }: Props) {
  const [abierto, setAbierto] = useState<string | null>(null);

  return (
    <>
      <div className="filtros">
        <label>
          Severidad{" "}
          <select value={severidad} onChange={(e) => alCambiarSeveridad(e.target.value as Severidad | "")}>
            <option value="">Todas</option>
            <option>Alta</option>
            <option>Media</option>
            <option>Baja</option>
            <option>Informativa</option>
          </select>
        </label>
        <div className="segmentado" role="group" aria-label="Ordenar por">
          <button className={orden === "severidad" ? "activo" : ""} onClick={() => alCambiarOrden("severidad")}>
            Ordenar por severidad
          </button>
          <button className={orden === "prioridadIA" ? "activo" : ""} onClick={() => alCambiarOrden("prioridadIA")}>
            Ordenar por Prioridad IA
          </button>
        </div>
        <span className="tenue">{hallazgos.length} hallazgo(s)</span>
      </div>

      <div className="tabla-envoltura">
        <table className="tabla">
          <thead>
            <tr>
              <th>Hallazgo</th>
              <th>Severidad</th>
              <th>OWASP Top 10:2025</th>
              <th>URL afectada</th>
              <th>Prioridad IA</th>
            </tr>
          </thead>
          <tbody>
            {hallazgos.map((h) => (
              <Fragment key={h.id}>
                <tr className="fila-clic" onClick={() => setAbierto(abierto === h.id ? null : h.id)}>
                  <td>
                    <span className="flecha">{abierto === h.id ? "▾" : "▸"}</span> {h.nombre}
                    {h.ocurrencias > 1 && <span className="tenue"> ×{h.ocurrencias}</span>}
                  </td>
                  <td>
                    <InsigniaSeveridad severidad={h.severidad} />
                  </td>
                  <td>{h.categoriaOwasp}</td>
                  <td className="url" title={h.urlAfectada}>
                    {h.metodoHttp && <span className="metodo">{h.metodoHttp}</span>} {h.urlAfectada}
                  </td>
                  <td>
                    <InsigniaPrioridadIA ia={h.ia} />
                  </td>
                </tr>
                {abierto === h.id && (
                  <tr className="detalle">
                    <td colSpan={5}>
                      <div className="detalle-rejilla">
                        <div>
                          <h4>Descripción</h4>
                          <p>{h.descripcion}</p>
                          <h4>Recomendación</h4>
                          <p className="pre">{h.recomendacion}</p>
                        </div>
                        <div>
                          <h4>Análisis IA</h4>
                          {h.ia.estado === "Analizado" ? (
                            <p>
                              {h.ia.justificacion}
                              <br />
                              <span className="tenue">
                                Probabilidad de falso positivo: {Math.round((h.ia.probabilidadFalsoPositivo ?? 0) * 100)} % ·
                                modelo {h.ia.versionModelo}
                              </span>
                            </p>
                          ) : (
                            <p className="tenue">Análisis IA pendiente: se reintentará automáticamente.</p>
                          )}
                          <h4>Datos técnicos</h4>
                          <p className="tenue">
                            {h.parametro && <>Parámetro: <code>{h.parametro}</code> · </>}
                            {h.cweId && <>CWE-{h.cweId} · </>}
                            Clasificación OWASP por: {h.origenCategoria}
                          </p>
                          {h.evidencia && (
                            <>
                              <h4>Evidencia</h4>
                              <code className="evidencia">{h.evidencia}</code>
                            </>
                          )}
                        </div>
                      </div>
                    </td>
                  </tr>
                )}
              </Fragment>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
}
