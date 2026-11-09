// Cliente HTTP: agrega el JWT, interpreta ProblemDetails y redirige al login ante 401.

const CLAVE_TOKEN = "egida.token";
const CLAVE_USUARIO = "egida.usuario";

let tokenEnMemoria: string | null = leer(CLAVE_TOKEN);

function leer(clave: string): string | null {
  try {
    return sessionStorage.getItem(clave);
  } catch {
    return null;
  }
}

export function guardarSesion(token: string, usuarioJson: string) {
  tokenEnMemoria = token;
  try {
    sessionStorage.setItem(CLAVE_TOKEN, token);
    sessionStorage.setItem(CLAVE_USUARIO, usuarioJson);
  } catch {
    /* sin almacenamiento: queda solo en memoria */
  }
}

export function borrarSesion() {
  tokenEnMemoria = null;
  try {
    sessionStorage.removeItem(CLAVE_TOKEN);
    sessionStorage.removeItem(CLAVE_USUARIO);
  } catch {
    /* nada */
  }
}

export const obtenerToken = () => tokenEnMemoria;
export const usuarioGuardado = () => leer(CLAVE_USUARIO);

export class ErrorApi extends Error {
  constructor(
    public estado: number,
    public codigo: string,
    mensaje: string,
    public errores?: Record<string, string[]>,
  ) {
    super(mensaje);
  }
}

let alNoAutorizado: (() => void) | null = null;
export const registrarAlNoAutorizado = (f: () => void) => (alNoAutorizado = f);

export async function api<T>(ruta: string, opciones: RequestInit = {}): Promise<T> {
  const cabeceras = new Headers(opciones.headers);
  if (opciones.body) cabeceras.set("Content-Type", "application/json");
  if (tokenEnMemoria) cabeceras.set("Authorization", `Bearer ${tokenEnMemoria}`);

  const r = await fetch(ruta, { ...opciones, headers: cabeceras });
  if (r.status === 401 && !ruta.endsWith("/auth/login")) {
    borrarSesion();
    alNoAutorizado?.();
  }
  if (!r.ok) {
    let cuerpo: { title?: string; codigo?: string; errors?: Record<string, string[]> } = {};
    try {
      cuerpo = await r.json();
    } catch {
      /* sin cuerpo */
    }
    throw new ErrorApi(r.status, cuerpo.codigo ?? "error", cuerpo.title ?? `Error ${r.status}`, cuerpo.errors);
  }
  if (r.status === 204) return undefined as T;
  return (await r.json()) as T;
}
