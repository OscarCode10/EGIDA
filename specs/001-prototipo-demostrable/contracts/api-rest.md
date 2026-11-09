# Contrato: API REST de ÉGIDA (`Egida.Api`)

Base: `/api`. Formato JSON (camelCase). Errores en **ProblemDetails** (RFC 9457), con `title` en español y una extensión `codigo` legible por máquina. Todas las rutas requieren `Authorization: Bearer <jwt>` salvo las marcadas como **pública**.

Convención de autorización: **C** = Cliente (solo sus recursos), **A** = Administrador (todos). Un recurso que existe pero no pertenece al Cliente responde **404**, para no revelar que existe. El intento queda auditado como `AccesoDenegado`.

## Autenticación

### `POST /api/auth/login` — pública
Solicitud:
```json
{ "correo": "cliente@egida.test", "contrasena": "********" }
```
- **200**:
  ```json
  { "token": "<jwt>", "expiraEn": "2026-11-08T15:30:00Z",
    "usuario": { "id": "…", "nombre": "Cliente Demo", "correo": "cliente@egida.test", "rol": "Cliente" } }
  ```
- **401** `codigo: "credenciales_invalidas"`: mensaje genérico, también si la cuenta está bloqueada (FR-005).
- **400** `codigo: "validacion"`: faltan campos.

### `GET /api/auth/yo` — C, A
- **200**: el objeto `usuario` de arriba.

## Objetivos permitidos

### `GET /api/objetivos-permitidos` — C, A
Sirve para que la UI sugiera la URL válida al registrar un activo.
- **200**: `[{ "nombre": "OWASP Juice Shop (pruebas)", "url": "http://juice-shop:3000" }]`

## Activos

### `GET /api/activos` — C, A
- **200**:
  ```json
  [{ "id": "…", "nombre": "Juice Shop", "url": "http://juice-shop:3000",
     "propietario": { "id": "…", "nombre": "Cliente Demo" },
     "creadoEn": "…", "ultimoEscaneo": { "id": "…", "estado": "Terminado", "finalizadoEn": "…" } }]
  ```
  `ultimoEscaneo` puede ser `null`. C recibe solo los suyos y A todos.

### `POST /api/activos` — C
Solicitud: `{ "nombre": "Juice Shop", "url": "http://juice-shop:3000" }`
- **201** + `Location: /api/activos/{id}`: el activo creado.
- **400** `codigo: "validacion"`: nombre vacío o URL inválida (detalle por campo en `errors`).
- **400** `codigo: "objetivo_no_autorizado"`: "Solo se permiten objetivos del entorno de pruebas autorizado."
- **409** `codigo: "activo_duplicado"`.
- **403**: si lo intenta un Administrador.

### `GET /api/activos/{id}` — C (propio), A
- **200** | **404**

## Escaneos

### `POST /api/activos/{id}/escaneos` — C (propio), A
Sin cuerpo. Responde sin esperar al escaneo (FR-012).
- **202** + `Location: /api/escaneos/{escaneoId}`:
  ```json
  { "id": "…", "activoId": "…", "estado": "Pendiente", "solicitadoEn": "…", "solicitadoPor": { "id": "…", "rol": "Administrador" } }
  ```
- **404**: el activo no existe o no es del Cliente.
- **409** `codigo: "escaneo_en_curso"` (FR-015).
- **400** `codigo: "objetivo_no_autorizado"`: el objetivo salió de la lista blanca después de registrarse.

### `GET /api/escaneos?activoId={id}&limite={n}` — C, A
- **200**: lista ordenada por `solicitadoEn` desc (límite por defecto 20, máximo 100). Cada elemento trae `id`, `activo {id, nombre, url, propietario}`, `estado`, `solicitadoEn`, `iniciadoEn`, `finalizadoEn`, `motivoFallo`, `totalHallazgos` y `conteoPorSeveridad {alta, media, baja, informativa}`.

### `GET /api/escaneos/{id}` — C (propio), A
- **200**: el mismo objeto, más `alerta { id, estadoEnvio } | null`. Se audita como `ResultadoEscaneoConsultado`.
- **404**

### `GET /api/escaneos/{id}/hallazgos?severidad={Alta|Media|Baja|Informativa}&orden={severidad|prioridadIA}` — C (propio), A
- **200**:
  ```json
  [{ "id": "…", "nombre": "SQL Injection - SQLite", "descripcion": "…",
     "severidad": "Alta", "categoriaOwasp": "A05:2025 - Injection", "origenCategoria": "EtiquetaZap2025",
     "urlAfectada": "http://juice-shop:3000/rest/products/search?q=…", "recomendacion": "…",
     "metodoHttp": "GET", "parametro": "q", "cweId": 89, "ocurrencias": 1,
     "ia": { "estado": "Analizado", "prioridad": "Alta", "probabilidadFalsoPositivo": 0.08,
             "esPosibleFalsoPositivo": false, "justificacion": "…", "versionModelo": "rf-2026.11.1" } }]
  ```
  Si `ia.estado = "Pendiente"`, los demás campos de `ia` son `null`.
  - `orden=severidad` (por defecto): severidad desc y luego Prioridad IA desc.
  - `orden=prioridadIA`: Prioridad IA desc (las pendientes al final) y luego severidad desc.

  Nota: las categorías OWASP de los ejemplos son ilustrativas; los valores reales salen de las etiquetas de ZAP y de la tabla de mapeo.

## Alertas

### `GET /api/alertas?limite={n}` — C (propias), A
- **200**: `[{ "id", "escaneoId", "activo": {"id","nombre"}, "creadaEn", "totalAltas", "totalPosiblesFp", "resumen", "estadoEnvio", "destinatario": {"id","nombre"} }]`

## Dashboard

### `GET /api/dashboard` — C, A
Alcance por rol (FR-031).
- **200**:
  ```json
  { "totalActivos": 1,
    "escaneosRecientes": [ /* 5, mismo formato que /api/escaneos */ ],
    "hallazgosPorSeveridad": { "alta": 3, "media": 12, "baja": 20, "informativa": 8 },
    "alertasRecientes": [ /* 5, mismo formato que /api/alertas */ ] }
  ```

## Salud

### `GET /salud` — pública
- **200** `{ "estado": "ok" }`. Se usa en el `healthcheck` de Docker Compose.

## Seguridad transversal

- Fallback policy: autenticado. Cada endpoint declara `[Authorize(Roles = …)]` o `[AllowAnonymous]` explícitamente.
- Límite de tasa en `/api/auth/login`: 10 solicitudes/min por IP (`Microsoft.AspNetCore.RateLimiting`).
- Cabeceras de seguridad (CSP, `X-Content-Type-Options`, `Referrer-Policy`) aplicadas por nginx en `web`.
- Swagger/OpenAPI solo en `Development`.
