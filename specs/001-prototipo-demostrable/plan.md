# Plan de implementación: Prototipo demostrable de ÉGIDA

**Rama**: `001-prototipo-demostrable` | **Fecha**: 2026-11-08 | **Especificación**: [spec.md](./spec.md)

**Entrada**: especificación de `/specs/001-prototipo-demostrable/spec.md` (con clarificaciones del 2026-11-08)

## Resumen

Prototipo de punta a punta, en menos de 5 minutos:

1. Un cliente inicia sesión y registra un activo (solo objetivos de una lista blanca del entorno aislado).
2. Lanza un escaneo que el Worker ejecuta con OWASP ZAP (perfil "demo": baseline + escaneo activo acotado).
3. Los resultados se normalizan a 4 severidades y a OWASP Top 10:2025.
4. Se analizan con un microservicio de IA (RandomForest) que asigna Prioridad IA y probabilidad de falso positivo.
5. Si hay hallazgos Altos, se envía un correo (Mailpit) y el dashboard se actualiza en tiempo real (SignalR alimentado por `LISTEN/NOTIFY` de PostgreSQL).

Todo se levanta con `docker compose up`.

**Desviación respecto a la entrada del usuario**: la entrada pedía "modo baseline". El baseline de ZAP es solo pasivo y no produce hallazgos Altos en Juice Shop, así que no se podría demostrar el correo de alerta. Se usa un perfil "demo" con escaneo activo acotado (≤ 2 min), permitido solo sobre la lista blanca. Ver [research.md](./research.md) R1; FR-018 se ajustó en la especificación.

## Contexto técnico

**Lenguaje/Versión**: C# / .NET 10 (SDK 10.0.403); Python 3.12 (contenedor de IA); TypeScript 5 + React 19 (frontend)

**Dependencias principales**:
- *Backend*: ASP.NET Core 10, EF Core 10 + Npgsql, SignalR, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.Extensions.Identity.Core` (solo `PasswordHasher`), `Microsoft.Extensions.Http.Resilience`, MailKit.
- *IA*: FastAPI, Uvicorn, scikit-learn, pandas, joblib.
- *Frontend*: Vite, react-router, TanStack Query, `@microsoft/signalr`.
- *Contenedores*: `postgres:17-alpine`, `zaproxy/zap-stable`, `bkimminich/juice-shop`, `axllent/mailpit`, nginx.

**Almacenamiento**: PostgreSQL 17 (una base `egida`; la IA no tiene base propia)

**Pruebas**: xUnit (+ Testcontainers.PostgreSql y `WebApplicationFactory` para integración); pytest; build del frontend en CI

**Plataforma objetivo**: contenedores Linux con Docker Compose (Windows 11 + Docker Desktop/WSL 2 en desarrollo); la nube queda fuera de alcance

**Tipo de proyecto**: aplicación web (API + Worker + microservicio de IA + SPA)

**Metas de rendimiento**: escaneo de Juice Shop < 3 min (SC-002); finalización visible en < 5 s (SC-005); correo en < 1 min (SC-004); UI responde en < 2 s durante el escaneo (SC-009)

**Restricciones**: el sistema completo funciona con ≤ 6 GB de RAM en Docker (ZAP ≤ 1.5 GB); solo se escanean objetivos de la lista blanca; secretos solo por `.env`; sin datos personales reales

**Escala/Alcance**: 2 usuarios semilla, ~1–5 activos, decenas de escaneos, cientos de hallazgos; ~8 pantallas en la SPA

Todas las incógnitas quedan resueltas en [research.md](./research.md); no hay "NEEDS CLARIFICATION" pendientes.

## Verificación constitucional (Constitution Check)

*Puerta: antes de la Fase 0 y otra vez después del diseño de la Fase 1.*

| # | Principio | Cómo lo cumple el diseño | Estado |
|---|---|---|---|
| I | Seguridad desde el diseño | JWT con roles; fallback policy autenticada y `[Authorize(Roles)]` explícito; bloqueo tras 5 fallos; rate limit en login; bitácora `registros_auditoria`; HTTP plano solo en local (principio permite dev local) | ✅ |
| II | Ley 1581 | Usuarios `@egida.test`, dataset de IA sintético generado por script, Juice Shop con datos ficticios | ✅ |
| III | Clasificación obligatoria | 4 severidades desde `riskcode`; OWASP 2025 por etiqueta → tabla 2021→2025 → CWE → categoría por defecto; el constructor de `Hallazgo` rechaza datos incompletos | ✅ |
| IV | Pruebas obligatorias | xUnit (dominio, aplicación, integración) + pytest; corren en CI | ✅ |
| V | Capas y degradación de IA | Domain ← Application ← Infrastructure/Api/Worker; IA solo por REST; `EstadoIA = Pendiente` + reintentador | ✅ |
| VI | API inmediata / Worker diferido | La API solo encola (202); ZAP, IA y correo corren en el Worker; tiempo real por SignalR | ✅ |
| VII | Idioma y convenciones | Identificadores de dominio, mensajes y documentos en español; Conventional Commits | ✅ |
| VIII | Todo en contenedores | Un único `docker-compose.yml` con los 8 servicios | ✅ |
| IX | Despliegue portable | SMTP estándar, cadenas de conexión, imágenes OCI, `LISTEN/NOTIFY` estándar de PostgreSQL; sin SDKs de nube | ✅ |
| X | CI/CD | `ci.yml`: build + pruebas .NET/Python + build web + CodeQL + revisión de dependencias | ✅ |
| XI | Secretos | `.env` ignorado; `.env.example` ficticio; secretos de CI en GitHub Secrets | ✅ |
| XII | Escaneo ético | Lista blanca por origen exacto, verificada al registrar y al ejecutar; contexto de alcance en ZAP; Juice Shop en red `internal` sin salida | ✅ |
| XIII | Prototipo y simplicidad | Sin Hangfire, Redis ni Identity completo; lo excluido queda en "trabajo futuro" | ✅ |

**Resultado (antes de la Fase 0)**: PASA. **Resultado (después de la Fase 1)**: PASA. La complejidad añadida (escaneo activo acotado, `LISTEN/NOTIFY`) está justificada abajo.

## Estructura del proyecto

### Documentación (esta funcionalidad)

```text
specs/001-prototipo-demostrable/
├── spec.md
├── plan.md                       # este archivo
├── research.md                   # Fase 0
├── data-model.md                 # Fase 1
├── quickstart.md                 # Fase 1
├── contracts/
│   ├── api-rest.md
│   ├── ia-analizar.md
│   └── signalr-notificaciones.md
├── checklists/requirements.md
└── tasks.md                      # Fase 2 (/speckit-tasks; no lo crea este comando)
```

### Código fuente (raíz del repositorio)

```text
Egida.slnx
Directory.Build.props             # net10.0, Nullable, TreatWarningsAsErrors
docker-compose.yml
.env.example
.github/workflows/ci.yml

src/
├── Egida.Domain/
│   ├── Activos/                  # Activo
│   ├── Escaneos/                 # Escaneo, EstadoEscaneo, Hallazgo, Severidad, PrioridadIA
│   ├── Alertas/                  # Alerta, EstadoEnvio
│   ├── Usuarios/                 # Usuario, Rol
│   ├── Auditoria/                # RegistroAuditoria
│   └── Clasificacion/            # MapeadorOwasp (reglas puras), tablas CWE→OWASP y 2021→2025
├── Egida.Application/
│   ├── Abstracciones/            # IColaEscaneos, IEscaner, IAnalizadorIA, IEnviadorCorreo,
│   │                             # IPublicadorEventos, IListaBlancaObjetivos, IRepositorio…, IReloj
│   ├── Autenticacion/            # IniciarSesion
│   ├── Activos/                  # RegistrarActivo, ListarActivos
│   ├── Escaneos/                 # SolicitarEscaneo, EjecutarEscaneo, ConsultarHallazgos
│   ├── AnalisisIA/               # AnalizarHallazgos, ReintentarPendientes
│   ├── Alertas/                  # GenerarAlerta, EnviarAlerta
│   └── Dashboard/                # ObtenerResumen
├── Egida.Infrastructure/
│   ├── Persistencia/             # EgidaDbContext, configuraciones, Migraciones/, SembradorUsuarios
│   ├── Cola/                     # ColaEscaneosPostgres (FOR UPDATE SKIP LOCKED)
│   ├── Zap/                      # ClienteZap, NormalizadorAlertasZap
│   ├── IA/                       # ClienteIA (resiliencia + validación de contrato)
│   ├── Correo/                   # EnviadorCorreoMailKit, plantillas/
│   ├── Eventos/                  # PublicadorEventosPostgres (NOTIFY)
│   └── Seguridad/                # GeneradorJwt, ListaBlancaObjetivosConfig
├── Egida.Api/
│   ├── Endpoints/                # Minimal APIs agrupadas: Auth, Activos, Escaneos, Alertas, Dashboard
│   ├── Hubs/                     # HubNotificaciones
│   ├── Servicios/                # ReenviadorEventos (LISTEN → SignalR)
│   └── Program.cs                # JWT, fallback policy, rate limit, ProblemDetails, migraciones + semilla
└── Egida.Worker/
    ├── ProcesadorEscaneos.cs     # BackgroundService de la cola
    ├── ReintentadorAnalisisIA.cs
    ├── EnviadorAlertas.cs
    └── Program.cs

tests/
├── Egida.Domain.Tests/           # estados, mapeo OWASP, reglas de Hallazgo y Alerta
├── Egida.Application.Tests/      # casos de uso con dobles
└── Egida.Api.Tests/              # integración: login, bloqueo, aislamiento SC-008, 409, lista blanca

egida-ia/
├── pyproject.toml / uv.lock
├── Dockerfile                    # python:3.12-slim; entrena el modelo en el build
├── app/                          # main.py (FastAPI), esquemas.py, modelo.py, justificacion.py
├── datos/hallazgos_sinteticos.csv
├── scripts/                      # generar_dataset.py, entrenar.py
└── tests/                        # test_analizar.py, test_salud.py

egida-web/
├── Dockerfile                    # build Vite → nginx (proxy /api y /hubs)
├── nginx.conf
└── src/
    ├── api/                      # cliente HTTP + tipos de los contratos
    ├── autenticacion/            # contexto de sesión, rutas protegidas
    ├── tiempo-real/              # conexión SignalR → invalidación de consultas
    ├── paginas/                  # Login, Dashboard, Activos, DetalleEscaneo, Alertas
    └── componentes/              # InsigniaSeveridad, InsigniaPrioridadIA, EstadoEscaneo, TablaHallazgos
```

**Decisión de estructura**: monorepo con `src/` para los 5 proyectos .NET de Clean Architecture, `tests/` para xUnit, y `egida-ia/` y `egida-web/` como servicios independientes en la raíz. `Egida.Api` y `Egida.Worker` son dos ejecutables (dos contenedores) que comparten `Application` e `Infrastructure`.

### Servicios de Docker Compose

| Servicio | Imagen | Redes | Puerto host | Notas |
|---|---|---|---|---|
| `postgres` | postgres:17-alpine | red-app | — | volumen `datos-postgres`; healthcheck `pg_isready` |
| `api` | build `src/Egida.Api` | red-app | 5080 | migraciones + semilla al arrancar |
| `worker` | build `src/Egida.Worker` | red-app | — | depende de postgres, zap y egida-ia |
| `egida-ia` | build `egida-ia` | red-app | — | healthcheck `/salud` |
| `zap` | zaproxy/zap-stable | red-app, red-objetivos | — | daemon, `-Xmx1g`, `mem_limit: 1536m`, clave API |
| `juice-shop` | bkimminich/juice-shop | red-objetivos (internal) | — | objetivo aislado |
| `mailpit` | axllent/mailpit | red-app | 8025 | bandeja web de la demo |
| `web` | build `egida-web` | red-app | 5173 | nginx + proxy a la API |

## Fases de implementación (guía para `/speckit-tasks` y `/speckit-implement`)

Al cerrar cada fase: pruebas en verde, commit, push. No se avanza si algo falla.

| Fase | Contenido | Criterio de salida |
|---|---|---|
| **A** | `docker-compose.yml` (postgres, zap, juice-shop, mailpit), `.env.example`, `Egida.slnx` con los 5 proyectos + 3 de pruebas vacíos, `ci.yml` básico | `dotnet build` OK; `docker compose up` deja sanos los 4 servicios de infraestructura; CI en verde |
| **B** | `Usuario`, `RegistroAuditoria`, DbContext + migración inicial, semilla, login JWT, bloqueo, `/api/auth/*`, fallback policy | Pruebas de integración de login, bloqueo y 401 en verde |
| **C** | `Activo`, `Escaneo`, `Hallazgo`; lista blanca; cola; `ClienteZap`; normalizador + mapeo OWASP; endpoints de activos y escaneos | **Validación R1**: escaneo real de Juice Shop < 3 min con ≥ 1 hallazgo Alto; pruebas de mapeo, 409 y lista blanca en verde |
| **D** | `egida-ia` completo (dataset, entrenamiento, `/analizar`, `/salud`, pytest) + `ClienteIA` + reintentador | Hallazgos con Prioridad IA; escenario de IA caída del quickstart OK |
| **E** | `Alerta` + `EnviadorAlertas` + plantilla; `PublicadorEventos`/`ReenviadorEventos` + hub; frontend completo | Quickstart completo (escenarios 1–15) en < 5 min |

## Seguimiento de complejidad

No hay violaciones de la constitución. Se registra la complejidad añadida por el principio XIII:

| Elemento | Por qué se necesita | Alternativa más simple rechazada porque |
|---|---|---|
| Escaneo activo acotado (además del baseline) | Sin hallazgos Altos no se demuestra la alerta por correo (HU5) | El baseline puro es solo pasivo y no genera Altos en Juice Shop |
| `LISTEN/NOTIFY` entre Worker y API | SignalR vive en la API y los eventos nacen en otro proceso | Polling desde la UI no cumple "tiempo real"; Redis añade RAM y otro servicio |
| URLs semilla por objetivo | Juice Shop es una SPA y el spider tradicional no descubre sus endpoints | El spider AJAX necesita un navegador headless (+~1 GB de RAM) |
| Dos modelos RandomForest | La especificación pide dos salidas distintas (Prioridad IA y probabilidad de FP) | Un único modelo multisalida complica la explicación de la justificación |
