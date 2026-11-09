# Contrato: hub SignalR de notificaciones

- **Ruta**: `/hubs/notificaciones`
- **Autenticación**: JWT por query `access_token` (solo en `/hubs/*`). Si no hay token válido, la conexión se rechaza.
- **Grupos**: al conectarse, el servidor agrega la conexión a `usuario-{id}`. Si el rol es Administrador, también a `administradores`. El cliente **no** puede unirse a grupos por su cuenta (el hub no expone métodos de cliente → servidor).

## Eventos servidor → cliente

Cada evento se envía al grupo `usuario-{propietarioDelActivo}` y al grupo `administradores`.

| Evento | Carga | Cuándo |
|---|---|---|
| `EscaneoActualizado` | `{ escaneoId, activoId, estado, motivoFallo, totalHallazgos }` | Cada transición de estado (FR-017) |
| `HallazgosAnalizados` | `{ escaneoId, analizados, pendientes }` | Al terminar el análisis IA o un reintento (FR-025) |
| `AlertaCreada` | `{ alertaId, escaneoId, totalAltas }` | Al crear una alerta (FR-027) |
| `AlertaEnvioActualizado` | `{ alertaId, estadoEnvio }` | Al enviarse o fallar definitivamente el correo |

Los eventos son **avisos de invalidación**: el frontend vuelve a pedir los datos por REST (TanStack Query `invalidateQueries`). Así no se duplica la lógica de autorización en el hub y el dashboard se actualiza solo (FR-032).

## Origen de los eventos

El Worker emite `NOTIFY egida_eventos, '{"tipo":"EscaneoActualizado","propietarioId":"…","datos":{…}}'`. El `ReenviadorEventos` de la API escucha con `LISTEN egida_eventos` y reenvía (ver [research.md](../research.md) R5). Si la conexión LISTEN se cae, se reconecta con backoff. Entretanto, el frontend sigue funcionando al recargar.

Reconexión del cliente: `withAutomaticReconnect()`. Al reconectar, el cliente invalida todas sus consultas.
