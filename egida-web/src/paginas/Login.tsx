import { useState, type FormEvent } from "react";
import { Navigate, useNavigate, useSearchParams } from "react-router";
import { ErrorApi } from "../api/cliente";
import { useSesion } from "../autenticacion/ContextoSesion";

export function Login() {
  const { usuario, iniciarSesion, cerrarSesion } = useSesion();
  const [rol, setRol] = useState<"Administrador" | "Cliente">("Cliente");
  const [parametros] = useSearchParams();
  const navegar = useNavigate();
  const [correo, setCorreo] = useState("");
  const [contrasena, setContrasena] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  const volver = parametros.get("volver");
  const destino = volver && volver.startsWith("/") && !volver.startsWith("//") ? volver : "/";
  if (usuario) return <Navigate to={destino} replace />;

  async function enviar(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setEnviando(true);
    try {
      const u = await iniciarSesion(correo, contrasena);
      if (u.rol !== rol) {
        cerrarSesion();
        setError(`Esta cuenta no tiene el rol ${rol}. Elige "${u.rol}" para ingresar.`);
        return;
      }
      navegar(destino, { replace: true });
    } catch (err) {
      setError(err instanceof ErrorApi && err.estado === 429
        ? "Demasiados intentos. Espera un minuto."
        : err instanceof ErrorApi ? err.message : "No se pudo conectar con el servidor.");
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="login-fondo">
      <form className="login" onSubmit={enviar}>
        <div className="marca grande">
          <svg viewBox="0 0 32 32" width="40" height="40" aria-hidden>
            <path d="M16 2 4 7v8c0 7.5 5.1 13.4 12 15 6.9-1.6 12-7.5 12-15V7z" fill="currentColor" />
            <path d="m11 16 3.5 3.5L21 13" stroke="#fff" strokeWidth="2.5" fill="none" strokeLinecap="round" />
          </svg>
          <span>ÉGIDA</span>
        </div>
        <p className="tenue centro">Escaneo, Gestión Inteligente y Detección de Amenazas</p>
        <div className="selector-rol" role="radiogroup" aria-label="Ingresar como">
          {(["Cliente", "Administrador"] as const).map((r) => (
            <button
              key={r}
              type="button"
              role="radio"
              aria-checked={rol === r}
              className={rol === r ? "activo" : ""}
              onClick={() => { setRol(r); setError(null); }}
            >
              {r}
            </button>
          ))}
        </div>
        <label>
          Correo
          <input type="email" autoComplete="username" value={correo} onChange={(e) => setCorreo(e.target.value)} required autoFocus />
        </label>
        <label>
          Contraseña
          <input type="password" autoComplete="current-password" value={contrasena} onChange={(e) => setContrasena(e.target.value)} required />
        </label>
        {error && <div className="aviso error" role="alert">{error}</div>}
        <button className="boton" disabled={enviando}>{enviando ? "Ingresando…" : `Ingresar como ${rol}`}</button>
        <p className="tenue pequeno centro">Prototipo académico · datos sintéticos</p>
      </form>
    </div>
  );
}
