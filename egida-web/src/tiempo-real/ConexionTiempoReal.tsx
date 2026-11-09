import { useEffect, useState } from "react";
import { HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { obtenerToken } from "../api/cliente";
import { useSesion } from "../autenticacion/ContextoSesion";

const EVENTOS = ["EscaneoActualizado", "HallazgosAnalizados", "AlertaCreada", "AlertaEnvioActualizado"];

/**
 * Conexión SignalR (contracts/signalr-notificaciones.md). Los eventos son avisos de invalidación:
 * se vuelven a pedir los datos por REST. Al reconectar se invalida todo.
 */
export function useTiempoReal() {
  const { usuario } = useSesion();
  const cliente = useQueryClient();
  const [conectado, setConectado] = useState(false);

  useEffect(() => {
    if (!usuario) return;
    const conexion = new HubConnectionBuilder()
      .withUrl("/hubs/notificaciones", { accessTokenFactory: () => obtenerToken() ?? "" })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    for (const evento of EVENTOS) {
      conexion.on(evento, () => cliente.invalidateQueries());
    }
    conexion.onreconnecting(() => setConectado(false));
    conexion.onreconnected(() => {
      setConectado(true);
      cliente.invalidateQueries();
    });
    conexion.onclose(() => setConectado(false));

    conexion
      .start()
      .then(() => setConectado(conexion.state === HubConnectionState.Connected))
      .catch(() => setConectado(false));

    return () => {
      conexion.stop();
    };
  }, [usuario, cliente]);

  return conectado;
}
