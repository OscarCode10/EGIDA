# Investigación (Fase 0): Prototipo demostrable de ÉGIDA

**Funcionalidad**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Fecha**: 2026-11-08

Cada decisión sigue el formato: Decisión / Justificación / Alternativas consideradas.

---

## R1. Perfil de escaneo: "baseline" no basta para la demo

- **Decisión**: usar un perfil de escaneo **"demo"** con cuatro pasos:
  1. Visitar las URLs semilla del objetivo.
  2. Spider tradicional (máx. 1 min).
  3. Esperar a que termine el escaneo pasivo.
  4. Escaneo **activo acotado** a una política reducida (inyección SQL, XSS reflejado/persistente, path traversal, divulgación de archivos), con un máximo de 2 min.

  El escaneo activo solo se permite porque el objetivo está en la lista blanca del entorno aislado (principio XII). Se ajustó FR-018 de la especificación.
- **Justificación**: el escaneo baseline de ZAP es **solo pasivo**; sus resultados son advertencias (cabeceras CSP, anti-CSRF, etc.) de severidad Media o Baja. Contra Juice Shop casi no produce hallazgos de riesgo Alto, así que la historia 5 (correo por severidad Alta) no se podría demostrar. Juice Shop además es una SPA (Angular) y el spider tradicional descubre pocas rutas. Por eso cada objetivo de la lista blanca puede declarar **URLs semilla** (p. ej. `/rest/products/search?q=manzana`), que el Worker visita antes del spider.
- **Alternativas consideradas**:
  - *Baseline puro*: no genera hallazgos Altos; se rechaza.
  - *Escaneo completo (full scan)*: tarda decenas de minutos y usa mucha RAM; incumple SC-002 (< 3 min).
  - *Spider AJAX (navegador headless)*: descubre más rutas en SPAs, pero suma ~1 GB de RAM y tiempo; queda como mejora futura.
  - *Bajar el umbral de alerta a Media*: cambia el requisito de la tesis; se rechaza.
- **Validación temprana obligatoria (Fase C)**: escanear Juice Shop con el perfil demo y confirmar que hay **al menos un hallazgo Alto** en menos de 3 min. Si no lo hay, ajustar URLs semilla y política antes de seguir.

## R2. Control de ZAP

- **Decisión**: imagen `zaproxy/zap-stable` en modo daemon. La API REST se protege con clave (`ZAP_API_KEY`) y solo es accesible en la red interna de Docker. El JVM se limita con `-Xmx1g` y el contenedor con `mem_limit: 1536m`. Cliente HTTP tipado en `Egida.Infrastructure` (`ClienteZap`).

  Por escaneo, el Worker:
  1. Crea un **contexto** de ZAP cuyo patrón de inclusión es solo el origen autorizado; spider y escaneo activo corren "solo en alcance".
  2. Desactiva el seguimiento de redirecciones fuera del contexto.
  3. Consulta las alertas filtradas por `baseurl`.
  4. Elimina el contexto al terminar.
- **Justificación**: la API REST es estable y no requiere SDK. El contexto de ZAP agrega una segunda barrera de alcance, además de la lista blanca de ÉGIDA (FR-013).
- **Alternativas**: scripts `zap-baseline.py` / Automation Framework en contenedores efímeros (un contenedor por escaneo). Son más aislados, pero arrancar la JVM cada vez suma ~30 s y complica el seguimiento de estado; se dejan como opción para la nube (ZAP bajo demanda).
- **Concurrencia**: un solo escaneo a la vez por instancia de ZAP. El Worker procesa la cola en serie, lo cual es suficiente para la demo.

## R3. Normalización: severidad y OWASP Top 10

- **Decisión**:
  - Severidad desde `riskcode` de ZAP: 3 → Alta, 2 → Media, 1 → Baja, 0 → Informativa.
  - Categoría OWASP desde las **etiquetas `OWASP_2025_Axx`** de la alerta (ZAP ya etiqueta sus reglas con OWASP Top 10:2025).
  - Si la alerta no trae etiqueta 2025, se buscan en orden: (1) etiqueta `OWASP_2021_Axx` traducida con una tabla 2021 → 2025, (2) tabla CWE → OWASP 2025 incluida en el repo, (3) categoría por defecto **A02:2025 – Security Misconfiguration**, marcada como "mapeo por regla".
  - Las alertas con confianza ZAP = 0 ("False Positive" marcado por el propio ZAP) se descartan.
  - Las alertas repetidas (mismo plugin + URL + parámetro) se agrupan en un solo hallazgo.
- **Justificación**: cumple el principio III (ningún hallazgo sin severidad ni OWASP) con una regla documentada y comprobable con pruebas unitarias. La versión 2025 es la vigente para la tesis.
- **Alternativas**: usar OWASP 2021 (más antigua; se rechaza) o mapear solo por CWE (algunas alertas pasivas no traen CWE).
- **Verificación pendiente**: confirmar en la Fase C que la versión de `zap-stable` descargada trae las etiquetas 2025. Si no, se activa la tabla 2021 → 2025.

## R4. Cola de escaneos sin Hangfire

- **Decisión**: el `BackgroundService` del Worker (`ProcesadorEscaneos`) consulta cada 3 s la tabla `escaneos`. Toma un escaneo con `SELECT … WHERE estado = 'Pendiente' ORDER BY solicitado_en LIMIT 1 FOR UPDATE SKIP LOCKED` y lo pasa a `EnCurso` en la misma transacción. Detrás de la interfaz `IColaEscaneos` (en `Application`) para poder migrar a Hangfire sin tocar los casos de uso.

  Un **índice único parcial** sobre `escaneos(activo_id) WHERE estado IN ('Pendiente','EnCurso')` garantiza FR-015 incluso con solicitudes simultáneas. Al arrancar, el Worker marca como `Fallido` (motivo "interrumpido") los escaneos que quedaron `EnCurso`.
- **Justificación**: no añade infraestructura (sin RabbitMQ/Redis), es transaccional y soporta varias instancias.
- **Alternativas**: Hangfire (pospuesto por decisión del usuario); `Channel<T>` en memoria (se pierde al reiniciar y no cruza procesos API ↔ Worker).

## R5. Tiempo real desde el Worker hasta el navegador

- **Decisión**: **PostgreSQL `LISTEN/NOTIFY`**. El Worker emite `NOTIFY egida_eventos, '<json>'` en cada cambio relevante (estado de escaneo, hallazgos analizados, alerta creada). Un `BackgroundService` dentro de la API (`ReenviadorEventos`) escucha con Npgsql y reenvía por SignalR a los grupos `usuario-{propietarioId}` y `administradores`.
- **Justificación**: SignalR vive en la API y el Worker es otro proceso. `LISTEN/NOTIFY` usa la base que ya existe, sin backplane Redis. La latencia es de milisegundos, de sobra para SC-005 (< 5 s).
- **Alternativas**: backplane Redis (más RAM, excluido); endpoint interno HTTP de la API que el Worker invoca (acopla procesos y exige autenticación entre servicios); polling desde el frontend (no es "tiempo real" y carga la API).

## R6. Autenticación, roles y bloqueo

- **Decisión**:
  - Entidad propia `Usuario`, con hash vía `PasswordHasher<T>` de `Microsoft.Extensions.Identity.Core` (PBKDF2), sin el esquema completo de ASP.NET Identity.
  - JWT Bearer firmado con HS256 (clave ≥ 32 bytes por variable de entorno), vigencia de **30 min**, claims `sub`, `email`, `role`.
  - Política de autorización por defecto `RequireAuthenticatedUser` (fallback policy) y `[Authorize(Roles=…)]` explícito en cada endpoint (denegar por defecto).
  - Bloqueo: 5 fallos consecutivos → `bloqueado_hasta = ahora + 15 min`.
  - SignalR recibe el token por `access_token` en la query, solo para la ruta `/hubs/*`.
  - El frontend guarda el token **en memoria y `sessionStorage`**; el logout lo borra.
  - Los recursos de otro cliente responden **404** para no revelar su existencia.
- **Justificación**: es lo mínimo que cumple el principio I y FR-001…FR-007, sin tablas ni páginas de Identity que no se usan (no hay registro, recuperación ni MFA).
- **Alternativas**: ASP.NET Core Identity completo (sobra para dos usuarios); cookies de sesión (más simple con SignalR, pero exige CSRF; JWT es el requisito del documento de la tesis); refresh tokens (fuera de alcance; la sesión expira a los 30 min).

## R7. Usuarios semilla y secretos

- **Decisión**: la API, al arrancar, aplica las migraciones de EF Core y crea si no existen `admin@egida.test` (Administrador) y `cliente@egida.test` (Cliente). Las contraseñas vienen de `SEMILLA_ADMIN_CONTRASENA` y `SEMILLA_CLIENTE_CONTRASENA`. El dominio `.test` está reservado (RFC 2606), así que los correos son sintéticos (principio II). `.env.example` se versiona con valores ficticios y `.env` está en `.gitignore` (principio XI).
- **Alternativas**: contraseñas fijas en el código (viola XI); script SQL aparte (otro paso manual).

## R8. Módulo de IA (egida-ia)

- **Decisión**:
  - **Python 3.12** en el contenedor (`python:3.12-slim`); dependencias con **uv** (`pyproject.toml` + `uv.lock`); FastAPI + Uvicorn + scikit-learn + pandas + joblib.
  - Dos modelos `RandomForestClassifier`: (a) **probabilidad de falso positivo** (binario, `predict_proba`) y (b) **Prioridad IA** (Alta/Media/Baja).
  - Variables de entrada: riesgo ZAP, confianza ZAP, plugin, CWE, categoría OWASP, método HTTP, si tiene parámetro, si tiene evidencia, longitud de la evidencia, tipo de contenido y si la URL es estática (js/css/img).
  - Dataset **sintético** `egida-ia/datos/hallazgos_sinteticos.csv` (~400 filas), generado por `scripts/generar_dataset.py` con reglas expertas más ruido controlado y semilla fija (reproducible). El dataset y el script se versionan.
  - El modelo se entrena en el `docker build` (paso `entrenar.py`) y se guarda con `joblib`. Al arrancar se carga; si falta, se entrena.
  - La **justificación** se arma con plantillas en español a partir de las variables que más pesan en el árbol (p. ej., "Confianza del escáner baja y evidencia vacía: probable falso positivo").
  - Endpoints `POST /analizar` y `GET /salud`. Respuesta con `version_modelo`.
- **Justificación**: cumple el stack constitucional. RandomForest es explicable y funciona con datasets pequeños. Usar 3.12 evita problemas de ruedas binarias de scikit-learn con Python 3.14 (el local puede usar 3.14 vía uv, pero el contenedor fija 3.12).
- **Alternativas**: dataset público CIC-IDS2017 (es tráfico de red, no hallazgos DAST; no encaja con la entrada); modelo LLM (fuera del stack y no reproducible); reglas sin ML (no cumple el objetivo de IA de la tesis).
- **Punto abierto con el director**: el dataset definitivo y la métrica de reducción de falsos positivos (> 30 %) se definen después; este dataset solo sirve para el prototipo.

## R9. Resiliencia ante caída de la IA

- **Decisión**: `HttpClient` tipado `ClienteIA` con `Microsoft.Extensions.Http.Resilience`: timeout de 10 s, 2 reintentos con backoff y circuit breaker. Si falla o la respuesta no valida (prioridad desconocida, probabilidad fuera de [0, 1], ids que no coinciden), los hallazgos quedan `EstadoIA = Pendiente` y el escaneo se marca `Terminado` igual.

  Un servicio periódico del Worker (`ReintentadorAnalisisIA`, cada 60 s) reanaliza los pendientes cuando `/salud` responde, y emite `HallazgosAnalizados`.
- **Justificación**: principio V (degradación elegante) y FR-024/FR-025.

## R10. Correo de alerta

- **Decisión**: el Worker crea la `Alerta` (una por escaneo, índice único `escaneo_id`) dentro de la misma transacción que marca el escaneo como `Terminado`. Luego `EnviadorAlertas` (BackgroundService) la envía con **MailKit** por SMTP a **Mailpit** (`axllent/mailpit`, SMTP 1025, UI 8025). Hasta 3 intentos (backoff de 10 s, 30 s, 90 s) y estado `Pendiente → Enviado | Fallido`. Plantilla HTML + texto en español, con enlace `{EGIDA_URL_PUBLICA}/escaneos/{id}`.
- **Justificación**: patrón *outbox*. El correo nunca se pierde ni bloquea el escaneo (FR-029), y SMTP estándar mantiene la portabilidad (principio IX).
- **Alternativas**: enviar en línea al terminar el escaneo (si falla el SMTP se pierde la alerta); SendGrid/Azure Communication Services (SDK propietario; viola IX).

## R11. Frontend (egida-web)

- **Decisión**: React + Vite + TypeScript, `react-router`, **TanStack Query** (caché e invalidación al llegar eventos SignalR), `@microsoft/signalr` y CSS simple (sin librería de componentes pesada). En Docker: build multietapa → **nginx** que sirve los estáticos y hace de proxy de `/api` y `/hubs` hacia la API (mismo origen, sin CORS).
- **Alternativas**: servidor de desarrollo de Vite en contenedor (recarga en caliente, pero no representa la publicación); Next.js (fuera del stack).

## R12. Red Docker aislada

- **Decisión**: dos redes:
  - `red-objetivos` (`internal: true`, sin salida a Internet): `juice-shop` y `zap`.
  - `red-app`: `postgres`, `api`, `worker`, `egida-ia`, `mailpit`, `web` y `zap`.

  Juice Shop no publica puertos al host. Solo se publican `web` (5173), `api` (5080, para Swagger en desarrollo) y `mailpit` (8025). La lista blanca, por variable de entorno, queda como `EGIDA__Escaneo__Objetivos__0__Url=http://juice-shop:3000` más sus URLs semilla.
- **Justificación**: cumple el principio XII y la restricción de la especificación (solo el entorno aislado). Aunque la API de ÉGIDA fallara en validar, ZAP no alcanza objetivos externos a través de `red-objetivos`. ZAP sí está en `red-app` para recibir órdenes; el contexto de alcance de R2 cubre ese lado.

## R13. Pruebas y CI

- **Decisión**:
  - **xUnit** con tres proyectos:
    - `Egida.Domain.Tests`: reglas de transición de estado, normalización y mapeo OWASP.
    - `Egida.Application.Tests`: casos de uso con dobles de prueba.
    - `Egida.Api.Tests`: integración con `WebApplicationFactory` + **Testcontainers.PostgreSql** (login, bloqueo, aislamiento entre clientes SC-008, 409 por escaneo duplicado, rechazo de URL no autorizada).
  - **pytest** + `httpx`/`TestClient` para `/analizar` (camino feliz, validación 422, determinismo del modelo).
  - **GitHub Actions** (`.github/workflows/ci.yml`): `dotnet build` + `dotnet test`, `uv run pytest`, `npm run build` del frontend, **CodeQL** (C#, Python, JS/TS) y `dependency-review-action` + `dotnet list package --vulnerable` + `pip-audit`.
- **Justificación**: principios IV y X.

## R14. Versiones

- .NET 10 (SDK 10.0.403 instalado) y solución en formato **`Egida.slnx`** (el predeterminado de `dotnet new sln` en .NET 10).
- EF Core 10 + `Npgsql.EntityFrameworkCore.PostgreSQL` 10.x; PostgreSQL 17 (`postgres:17-alpine`).
- Node 20 sirve para Vite actual, pero se recomienda pasar a Node 22/24 LTS antes de la Fase E.
- Las versiones exactas de paquetes NuGet/npm/PyPI se fijan en la implementación (lockfiles versionados).
