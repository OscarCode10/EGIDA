import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { Navigate, useLocation, useNavigate } from "react-router";
import { useQueryClient } from "@tanstack/react-query";
import { api, borrarSesion, guardarSesion, obtenerToken, registrarAlNoAutorizado, usuarioGuardado } from "../api/cliente";
import type { RespuestaLogin, Usuario } from "../api/tipos";

interface Sesion {
  usuario: Usuario | null;
  esAdmin: boolean;
  iniciarSesion: (correo: string, contrasena: string) => Promise<Usuario>;
  cerrarSesion: () => void;
}

const Contexto = createContext<Sesion | null>(null);

function usuarioInicial(): Usuario | null {
  const json = usuarioGuardado();
  if (!json || !obtenerToken()) return null;
  try {
    return JSON.parse(json) as Usuario;
  } catch {
    return null;
  }
}

export function ProveedorSesion({ children }: { children: ReactNode }) {
  const [usuario, setUsuario] = useState<Usuario | null>(usuarioInicial);
  const cliente = useQueryClient();
  const navegar = useNavigate();
  const ubicacion = useLocation();

  const cerrarSesion = useCallback(() => {
    borrarSesion();
    cliente.clear();
    setUsuario(null);
    navegar("/login");
  }, [cliente, navegar]);

  useEffect(() => {
    registrarAlNoAutorizado(() => {
      cliente.clear();
      setUsuario(null);
      const volver = encodeURIComponent(ubicacion.pathname + ubicacion.search);
      navegar(`/login?volver=${volver}`);
    });
  }, [cliente, navegar, ubicacion]);

  const iniciarSesion = useCallback(async (correo: string, contrasena: string) => {
    const r = await api<RespuestaLogin>("/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ correo, contrasena }),
    });
    guardarSesion(r.token, JSON.stringify(r.usuario));
    setUsuario(r.usuario);
    return r.usuario;
  }, []);

  const valor = useMemo(
    () => ({ usuario, esAdmin: usuario?.rol === "Administrador", iniciarSesion, cerrarSesion }),
    [usuario, iniciarSesion, cerrarSesion],
  );
  return <Contexto.Provider value={valor}>{children}</Contexto.Provider>;
}

export function useSesion() {
  const s = useContext(Contexto);
  if (!s) throw new Error("useSesion fuera de ProveedorSesion");
  return s;
}

/** HU1-4: sin sesión, cualquier ruta redirige al inicio de sesión. */
export function RutaProtegida({ children }: { children: ReactNode }) {
  const { usuario } = useSesion();
  const ubicacion = useLocation();
  if (!usuario) {
    const volver = encodeURIComponent(ubicacion.pathname + ubicacion.search);
    return <Navigate to={`/login?volver=${volver}`} replace />;
  }
  return <>{children}</>;
}
