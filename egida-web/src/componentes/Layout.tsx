import { NavLink, Outlet } from "react-router";
import { useSesion } from "../autenticacion/ContextoSesion";
import { useTiempoReal } from "../tiempo-real/ConexionTiempoReal";

export function Layout() {
  const { usuario, esAdmin, cerrarSesion } = useSesion();
  const conectado = useTiempoReal();

  return (
    <div className="app">
      <header className="barra">
        <div className="marca">
          <svg viewBox="0 0 32 32" width="26" height="26" aria-hidden>
            <path d="M16 2 4 7v8c0 7.5 5.1 13.4 12 15 6.9-1.6 12-7.5 12-15V7z" fill="currentColor" />
            <path d="m11 16 3.5 3.5L21 13" stroke="#fff" strokeWidth="2.5" fill="none" strokeLinecap="round" />
          </svg>
          <span>ÉGIDA</span>
        </div>
        <nav>
          <NavLink to="/" end>Panel</NavLink>
          <NavLink to="/activos">Activos</NavLink>
          <NavLink to="/escaneos">Escaneos</NavLink>
          <NavLink to="/alertas">Alertas</NavLink>
          <NavLink to="/metricas">Métricas</NavLink>
          <NavLink to="/reportes">Reportes</NavLink>
          <NavLink to="/auditoria">Auditoría</NavLink>
          {esAdmin && <NavLink to="/usuarios">Usuarios</NavLink>}
          <NavLink to="/configuracion">Configuración</NavLink>
        </nav>
        <div className="usuario">
          <span className={`punto ${conectado ? "ok" : ""}`} title={conectado ? "Tiempo real conectado" : "Tiempo real desconectado"} />
          <span>
            {usuario?.nombre} <span className={`rol ${esAdmin ? "rol-admin" : ""}`}>{usuario?.rol}</span>
          </span>
          <button className="boton-sec" onClick={cerrarSesion}>Cerrar sesión</button>
        </div>
      </header>
      <main className="contenido">
        <Outlet />
      </main>
    </div>
  );
}
