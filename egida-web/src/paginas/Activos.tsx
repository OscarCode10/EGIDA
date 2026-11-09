import { useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate } from "react-router";
import { api, ErrorApi } from "../api/cliente";
import type { Activo, ObjetivoPermitido } from "../api/tipos";
import { useSesion } from "../autenticacion/ContextoSesion";
import { EstadoEscaneo, fecha } from "../componentes/Insignias";

export function Activos() {
  const { esAdmin } = useSesion();
  const cliente = useQueryClient();
  const navegar = useNavigate();
  const activos = useQuery({ queryKey: ["activos"], queryFn: () => api<Activo[]>("/api/activos") });
  const objetivos = useQuery({ queryKey: ["objetivos"], queryFn: () => api<ObjetivoPermitido[]>("/api/objetivos-permitidos") });

  const [nombre, setNombre] = useState("");
  const [url, setUrl] = useState("");
  const [errores, setErrores] = useState<Record<string, string[]>>({});
  const [mensaje, setMensaje] = useState<{ tipo: "error" | "ok"; texto: string } | null>(null);
  const [errorEscaneo, setErrorEscaneo] = useState<Record<string, string>>({});

  const registrar = useMutation({
    mutationFn: () => api<Activo>("/api/activos", { method: "POST", body: JSON.stringify({ nombre, url }) }),
    onSuccess: (a) => {
      setNombre("");
      setUrl("");
      setErrores({});
      setMensaje({ tipo: "ok", texto: `Activo "${a.nombre}" registrado.` });
      cliente.invalidateQueries();
    },
    onError: (e) => {
      if (e instanceof ErrorApi) {
        setErrores(e.errores ?? {});
        setMensaje({ tipo: "error", texto: e.message });
      } else setMensaje({ tipo: "error", texto: "No se pudo registrar el activo." });
    },
  });

  const escanear = useMutation({
    mutationFn: (id: string) => api<{ id: string }>(`/api/activos/${id}/escaneos`, { method: "POST" }),
    onSuccess: (e, id) => {
      setErrorEscaneo((x) => ({ ...x, [id]: "" }));
      cliente.invalidateQueries();
      navegar(`/escaneos/${e.id}`);
    },
    onError: (e, id) => setErrorEscaneo((x) => ({ ...x, [id]: e instanceof ErrorApi ? e.message : "Error al lanzar el escaneo." })),
  });

  function enviar(e: FormEvent) {
    e.preventDefault();
    setMensaje(null);
    registrar.mutate();
  }

  return (
    <>
      <h1>{esAdmin ? "Activos de todos los clientes" : "Mis activos"}</h1>

      {!esAdmin && (
        <section className="tarjeta">
          <h2>Registrar un activo</h2>
          <form className="formulario-linea" onSubmit={enviar} noValidate>
            <label>
              Nombre
              <input value={nombre} onChange={(e) => setNombre(e.target.value)} placeholder="Juice Shop" maxLength={100} />
              {errores.nombre && <small className="error-campo">{errores.nombre[0]}</small>}
            </label>
            <label className="ancho">
              URL
              <input value={url} onChange={(e) => setUrl(e.target.value)} placeholder="http://juice-shop:3000" list="objetivos" />
              <datalist id="objetivos">
                {objetivos.data?.map((o) => <option key={o.url} value={o.url}>{o.nombre}</option>)}
              </datalist>
              {errores.url && <small className="error-campo">{errores.url[0]}</small>}
            </label>
            <button className="boton" disabled={registrar.isPending}>Registrar</button>
          </form>
          {objetivos.data && objetivos.data.length > 0 && (
            <p className="tenue pequeno">
              Objetivos autorizados del entorno de pruebas:{" "}
              {objetivos.data.map((o) => (
                <button key={o.url} type="button" className="enlace" onClick={() => { setUrl(o.url); if (!nombre) setNombre("Juice Shop"); }}>
                  {o.url}
                </button>
              ))}
            </p>
          )}
          {mensaje && <div className={`aviso ${mensaje.tipo === "ok" ? "ok" : "error"}`}>{mensaje.texto}</div>}
        </section>
      )}

      <section className="tarjeta">
        {activos.isLoading ? (
          <p className="tenue">Cargando…</p>
        ) : !activos.data?.length ? (
          <p className="tenue">No hay activos registrados.</p>
        ) : (
          <div className="tabla-envoltura">
            <table className="tabla">
              <thead>
                <tr>
                  <th>Nombre</th>
                  <th>URL</th>
                  {esAdmin && <th>Propietario</th>}
                  <th>Último escaneo</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {activos.data.map((a) => {
                  const enMarcha = a.ultimoEscaneo && ["Pendiente", "EnCurso"].includes(a.ultimoEscaneo.estado);
                  return (
                    <tr key={a.id}>
                      <td><strong>{a.nombre}</strong></td>
                      <td className="url">{a.url}</td>
                      {esAdmin && <td>{a.propietario.nombre}</td>}
                      <td>
                        {a.ultimoEscaneo ? (
                          <Link to={`/escaneos/${a.ultimoEscaneo.id}`}>
                            <EstadoEscaneo estado={a.ultimoEscaneo.estado} />{" "}
                            <span className="tenue">{fecha(a.ultimoEscaneo.finalizadoEn)}</span>
                          </Link>
                        ) : (
                          <span className="tenue">Nunca escaneado</span>
                        )}
                      </td>
                      <td className="acciones">
                        <button className="boton" disabled={escanear.isPending} onClick={() => escanear.mutate(a.id)}
                          title={enMarcha ? "Ya hay un escaneo en marcha" : "Lanzar escaneo"}>
                          Lanzar escaneo
                        </button>
                        {errorEscaneo[a.id] && <div className="error-campo">{errorEscaneo[a.id]}</div>}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </>
  );
}
