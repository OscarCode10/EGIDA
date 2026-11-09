---

description: "Lista de tareas del prototipo demostrable de ÉGIDA"
---

# Tareas: Prototipo demostrable de ÉGIDA

**Entrada**: documentos de diseño en `/specs/001-prototipo-demostrable/`

**Prerrequisitos**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Pruebas**: SÍ se incluyen. La constitución (principio IV, no negociable) exige pruebas automatizadas xUnit/pytest del camino feliz y de los errores esperados de cada funcionalidad. En cada historia, las pruebas se escriben primero y deben fallar antes de implementar.

**Organización**: por historia de usuario (HU1–HU7 de la especificación = US1–US7 aquí), para que cada una se pueda implementar y probar por separado. La columna "Fase del plan" indica a qué fase A–E de [plan.md](./plan.md) corresponde cada bloque.

## Formato: `[ID] [P?] [Historia] Descripción`

- **[P]**: se puede hacer en paralelo (archivos distintos, sin dependencias pendientes).
- **[USn]**: historia de usuario a la que pertenece la tarea.
- Todas las rutas son relativas a la raíz del repositorio.

## Convenciones de rutas

- Backend .NET: `src/Egida.<Capa>/…`, pruebas en `tests/Egida.<Capa>.Tests/…`
- IA: `egida-ia/…`
- Frontend: `egida-web/src/…`

## Correspondencia con las fases del plan

| Fase del plan | Fases de este archivo |
|---|---|
| A | Fase 1 (Setup) |
| B | Fase 2 (Fundacional) + Fase 3 (US1) |
| C | Fase 4 (US2) + Fase 5 (US3) |
| D | Fase 6 (US4) |
| E | Fase 7 (US5) + Fase 8 (US6) + Fase 9 (US7) + Fase 10 (Pulido) |

---

## Fase 1: Setup (infraestructura compartida) — Fase A del plan

**Propósito**: estructura del repositorio, contenedores de infraestructura y CI básico.

- [ ] T001 Crear `.gitignore` en la raíz (bin/, obj/, `.env`, node_modules/, dist/, `.venv/`, `__pycache__/`, `egida-ia/modelos/*.joblib`, `TestResults/`)
- [ ] T002 Crear `Directory.Build.props` en la raíz con `TargetFramework net10.0`, `Nullable enable`, `ImplicitUsings enable`, `TreatWarningsAsErrors true`, `LangVersion latest`
- [ ] T003 Crear la solución `Egida.slnx` y los proyectos `src/Egida.Domain` (classlib), `src/Egida.Application` (classlib), `src/Egida.Infrastructure` (classlib), `src/Egida.Api` (web vacío), `src/Egida.Worker` (worker) con referencias: Application→Domain; Infrastructure→Application; Api→Application+Infrastructure; Worker→Application+Infrastructure
- [ ] T004 Crear los proyectos xUnit `tests/Egida.Domain.Tests` (→Domain), `tests/Egida.Application.Tests` (→Application), `tests/Egida.Api.Tests` (→Api, con `Microsoft.AspNetCore.Mvc.Testing` y `Testcontainers.PostgreSql`) y agregarlos a `Egida.slnx`
- [ ] T005 Agregar paquetes NuGet: `Npgsql.EntityFrameworkCore.PostgreSQL` 10.x, `EFCore.NamingConventions`, `Microsoft.Extensions.Identity.Core`, `Microsoft.Extensions.Http.Resilience`, `MailKit` en `src/Egida.Infrastructure/Egida.Infrastructure.csproj`; `Microsoft.AspNetCore.Authentication.JwtBearer` y `Microsoft.AspNetCore.OpenApi` en `src/Egida.Api/Egida.Api.csproj`; `Microsoft.EntityFrameworkCore.Design` en `src/Egida.Api/Egida.Api.csproj`
- [ ] T006 [P] Crear `.env.example` con valores ficticios: `POSTGRES_DB=egida`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `JWT_CLAVE` (≥ 32 bytes), `ZAP_API_KEY`, `SEMILLA_ADMIN_CONTRASENA`, `SEMILLA_CLIENTE_CONTRASENA`, `EGIDA_URL_PUBLICA=http://localhost:5173`
- [ ] T007 Crear `docker-compose.yml` con los servicios de infraestructura: `postgres` (postgres:17-alpine, volumen `datos-postgres`, healthcheck `pg_isready`, red-app), `zap` (zaproxy/zap-stable en daemon, `-Xmx1g`, `mem_limit: 1536m`, `api.key=${ZAP_API_KEY}`, `api.addrs.addr.regex=true`, redes red-app y red-objetivos, healthcheck a `/JSON/core/view/version/`), `juice-shop` (bkimminich/juice-shop, solo red-objetivos, sin puertos), `mailpit` (axllent/mailpit, red-app, puerto 8025); red `red-objetivos` con `internal: true` (R12)
- [ ] T008 [P] Crear `.github/workflows/ci.yml` con job .NET: `actions/setup-dotnet` 10.x, `dotnet restore`, `dotnet build Egida.slnx -c Release`, `dotnet test Egida.slnx -c Release` (los jobs de Python y web se agregan en T093 y T108)
- [ ] T009 [P] Crear `README.md` en la raíz con descripción breve de ÉGIDA y enlace a `specs/001-prototipo-demostrable/quickstart.md`
- [ ] T010 Verificar: `dotnet build Egida.slnx` sin errores y `docker compose up -d postgres zap juice-shop mailpit` deja los 4 servicios sanos (`docker compose ps`)

**Checkpoint**: solución compila, infraestructura arriba, CI en verde.

---

## Fase 2: Fundacional (prerrequisitos bloqueantes) — Fase B del plan

**Propósito**: núcleo que TODAS las historias necesitan: persistencia, configuración, pipeline de la API, base del Worker y esqueleto del frontend.

**⚠️ CRÍTICO**: ninguna historia empieza hasta terminar esta fase.

- [ ] T011 [P] Crear enum `Rol { Administrador, Cliente }` en `src/Egida.Domain/Usuarios/Rol.cs`
- [ ] T012 [P] Crear `IReloj` (`DateTimeOffset Ahora`) en `src/Egida.Application/Abstracciones/IReloj.cs` e implementación `RelojSistema` en `src/Egida.Infrastructure/Comun/RelojSistema.cs`
- [ ] T013 [P] Crear la clase base de errores de dominio `ExcepcionDominio` (con `Codigo` legible por máquina) en `src/Egida.Domain/Comun/ExcepcionDominio.cs`
- [ ] T014 [P] Crear `Resultado<T>` / `Error` (código, mensaje en español, errores por campo) en `src/Egida.Application/Comun/Resultado.cs`
- [ ] T015 Crear `EgidaDbContext` en `src/Egida.Infrastructure/Persistencia/EgidaDbContext.cs` con `UseSnakeCaseNamingConvention()`, enums guardados como texto, `DateTimeOffset` en UTC y aplicación de configuraciones por ensamblado (`ApplyConfigurationsFromAssembly`)
- [ ] T016 Crear `DependencyInjection.AgregarInfraestructura(IServiceCollection, IConfiguration)` en `src/Egida.Infrastructure/DependencyInjection.cs` (DbContext con cadena `ConnectionStrings:Egida`, `IReloj`)
- [ ] T017 Crear `Program.cs` base de `src/Egida.Api/Program.cs`: ProblemDetails (RFC 9457, `title` en español y extensión `codigo`), manejador global de excepciones que traduce `ExcepcionDominio`, fallback policy `RequireAuthenticatedUser`, `GET /salud` con `[AllowAnonymous]` → `{ "estado": "ok" }`, OpenAPI solo en `Development`, y `public partial class Program {}` para pruebas
- [ ] T018 Crear `src/Egida.Api/Endpoints/ExtensionesResultado.cs` que convierte `Resultado<T>` en respuestas HTTP (400 `validacion` con `errors`, 404, 409, 403) usando ProblemDetails
- [ ] T019 Crear `Program.cs` base del Worker en `src/Egida.Worker/Program.cs` (Host genérico + `AgregarInfraestructura`, logging en consola)
- [ ] T020 [P] Crear `Dockerfile` multietapa (sdk:10.0 → aspnet:10.0, usuario no root) en `src/Egida.Api/Dockerfile` y `src/Egida.Worker/Dockerfile` (contexto = raíz del repo)
- [ ] T021 Agregar servicios `api` (puerto 5080, depende de postgres sano, variables `ConnectionStrings__Egida`, `Jwt__Clave`, `Semilla__*`) y `worker` (depende de postgres, zap) a `docker-compose.yml`
- [ ] T022 Crear `tests/Egida.Api.Tests/Infraestructura/FabricaApiEgida.cs`: `WebApplicationFactory<Program>` + `PostgreSqlContainer` de Testcontainers (`IAsyncLifetime`), con configuración de prueba (clave JWT, contraseñas semilla) y helper `ObtenerTokenAsync(correo, contrasena)`
- [ ] T023 [P] Prueba de humo `GET /salud` → 200 sin token, y `GET /api/cualquier-cosa` → 401 sin token en `tests/Egida.Api.Tests/SaludTests.cs`
- [ ] T024 [P] Inicializar `egida-web` con Vite + React 19 + TypeScript (`npm create vite@latest egida-web -- --template react-ts`), agregar `react-router`, `@tanstack/react-query`, `@microsoft/signalr`; configurar proxy de desarrollo `/api` y `/hubs` → `http://localhost:5080` en `egida-web/vite.config.ts`
- [ ] T025 [P] Crear cliente HTTP base `egida-web/src/api/cliente.ts` (fetch con `Authorization: Bearer`, parseo de ProblemDetails a error tipado, redirección a `/login` ante 401) y `egida-web/src/api/tipos.ts` con los tipos de [contracts/api-rest.md](./contracts/api-rest.md)
- [ ] T026 [P] Crear estilos base y layout (`egida-web/src/estilos.css`, `egida-web/src/componentes/Layout.tsx` con barra de navegación: Dashboard, Activos, Alertas, Cerrar sesión)

**Checkpoint**: API y Worker arrancan; pruebas de humo en verde; frontend compila (`npm run build`).

---

## Fase 3: Historia de usuario 1 — Inicio de sesión con roles (Prioridad: P1) 🎯 MVP — Fase B del plan

**Objetivo**: los dos usuarios semilla entran con correo y contraseña, reciben un JWT con su rol y nada es accesible sin sesión.

**Prueba independiente**: iniciar sesión con `admin@egida.test` y `cliente@egida.test`; contraseña errónea → mensaje genérico; 5 fallos → bloqueo de 15 min; sin token → 401 / redirección a `/login`.

### Pruebas de la US1 ⚠️ (escribir primero, deben fallar)

- [ ] T027 [P] [US1] Pruebas unitarias de `Usuario` en `tests/Egida.Domain.Tests/Usuarios/UsuarioTests.cs`: correo se guarda en minúsculas; un fallo incrementa `IntentosFallidos`; al 5.º fallo `BloqueadoHasta = ahora + 15 min` e `IntentosFallidos = 0`; login exitoso reinicia `IntentosFallidos`; `EstaBloqueado(ahora)` es verdadero mientras `BloqueadoHasta > ahora`
- [ ] T028 [P] [US1] Pruebas del caso de uso `IniciarSesion` con dobles en `tests/Egida.Application.Tests/Autenticacion/IniciarSesionTests.cs`: éxito devuelve token y audita `LoginExitoso`; contraseña errónea → `credenciales_invalidas` y audita `LoginFallido`; correo inexistente → mismo error, `usuario_id` nulo y `correo_intentado` registrado; cuenta bloqueada → mismo error genérico y audita `CuentaBloqueada` al bloquearse
- [ ] T029 [P] [US1] Pruebas de integración en `tests/Egida.Api.Tests/Autenticacion/LoginTests.cs`: `POST /api/auth/login` 200 con `token`, `expiraEn` y `usuario` para ambos semilla; 401 `codigo: "credenciales_invalidas"`; 400 `validacion` si faltan campos; 5 fallos seguidos → el 6.º intento con contraseña correcta también responde 401; `GET /api/auth/yo` 200 con token y 401 sin token; token expirado → 401
- [ ] T030 [P] [US1] Prueba de integración de límite de tasa en `tests/Egida.Api.Tests/Autenticacion/LimiteTasaLoginTests.cs`: la 11.ª solicitud a `/api/auth/login` en un minuto desde la misma IP responde 429

### Implementación de la US1

- [ ] T031 [P] [US1] Crear entidad `Usuario` en `src/Egida.Domain/Usuarios/Usuario.cs`: `Id` (Guid v7), `Nombre` "texto(100), obligatorio", `Correo` "texto(254), obligatorio, guardado en minúsculas, único", `HashContrasena`, `Rol`, `IntentosFallidos` "≥ 0; se reinicia con un login exitoso", `BloqueadoHasta` (nullable), `CreadoEn`; métodos `RegistrarLoginFallido(ahora)`, `RegistrarLoginExitoso()`, `EstaBloqueado(ahora)` según la regla de bloqueo de data-model.md
- [ ] T032 [P] [US1] Crear entidad `RegistroAuditoria` (solo inserción) en `src/Egida.Domain/Auditoria/RegistroAuditoria.cs` con `Fecha`, `UsuarioId?`, `CorreoIntentado?` "texto(254)", `Accion` "texto(60)", `Recurso?` "texto(200)", `Resultado` "texto(20): Exito, Fallo, Denegado", `IpOrigen?` "texto(45)"; y constantes de acción en `src/Egida.Domain/Auditoria/AccionesAuditoria.cs` (`LoginExitoso`, `LoginFallido`, `CuentaBloqueada`, `ActivoRegistrado`, `EscaneoSolicitado`, `EscaneoSolicitadoPorAdmin`, `ResultadoEscaneoConsultado`, `AccesoDenegado`)
- [ ] T033 [P] [US1] Crear abstracciones en `src/Egida.Application/Abstracciones/`: `IRepositorioUsuarios.cs`, `IRegistroAuditoria.cs`, `IHasherContrasenas.cs`, `IGeneradorTokens.cs`, `IUsuarioActual.cs` (Id, Rol, EsAdministrador, IP)
- [ ] T034 [US1] Crear configuraciones EF `UsuarioConfiguracion` (índice único en `correo`) y `RegistroAuditoriaConfiguracion` en `src/Egida.Infrastructure/Persistencia/Configuraciones/`
- [ ] T035 [US1] Implementar `RepositorioUsuarios` y `RegistroAuditoriaEf` en `src/Egida.Infrastructure/Persistencia/`
- [ ] T036 [P] [US1] Implementar `HasherContrasenas` (envuelve `PasswordHasher<Usuario>`, PBKDF2) en `src/Egida.Infrastructure/Seguridad/HasherContrasenas.cs`
- [ ] T037 [P] [US1] Implementar `GeneradorJwt` (HS256, clave ≥ 32 bytes desde `Jwt:Clave`, vigencia 30 min, claims `sub`, `email`, `role`) en `src/Egida.Infrastructure/Seguridad/GeneradorJwt.cs`
- [ ] T038 [US1] Implementar caso de uso `IniciarSesion` en `src/Egida.Application/Autenticacion/IniciarSesion.cs` (mensaje siempre genérico "Correo o contraseña incorrectos"; auditoría de cada intento)
- [ ] T039 [US1] Implementar `SembradorUsuarios` en `src/Egida.Infrastructure/Persistencia/SembradorUsuarios.cs`: crea si no existen `admin@egida.test` (Administrador, "Administrador Demo") y `cliente@egida.test` (Cliente, "Cliente Demo") con contraseñas de `SEMILLA_ADMIN_CONTRASENA` / `SEMILLA_CLIENTE_CONTRASENA`; falla al arrancar si faltan
- [ ] T040 [US1] Crear la migración inicial `dotnet ef migrations add Inicial` en `src/Egida.Infrastructure/Persistencia/Migraciones/` (tablas `usuarios`, `registros_auditoria`)
- [ ] T041 [US1] En `src/Egida.Api/Program.cs`: autenticación JwtBearer (lectura de `access_token` en query solo para `/hubs/*`), autorización por roles, rate limiter `login` (10 solicitudes/min por IP), aplicar migraciones y ejecutar `SembradorUsuarios` al arrancar; implementar `UsuarioActualHttp` en `src/Egida.Api/Servicios/UsuarioActualHttp.cs`
- [ ] T042 [US1] Crear endpoints `POST /api/auth/login` (`[AllowAnonymous]`, rate limit) y `GET /api/auth/yo` (Roles C, A) en `src/Egida.Api/Endpoints/EndpointsAutenticacion.cs`
- [ ] T043 [P] [US1] Crear contexto de sesión en `egida-web/src/autenticacion/ContextoSesion.tsx` (token en memoria + `sessionStorage`, `iniciarSesion`, `cerrarSesion` que borra el token y la caché de TanStack Query) y `egida-web/src/autenticacion/RutaProtegida.tsx` (redirige a `/login?volver=<ruta>`)
- [ ] T044 [US1] Crear página `egida-web/src/paginas/Login.tsx` (correo, contraseña, mensaje de error genérico, redirección a `volver` tras éxito) y el enrutador en `egida-web/src/App.tsx` con todas las rutas protegidas salvo `/login`

**Checkpoint**: US1 funciona y se prueba por separado (T027–T030 en verde).

---

## Fase 4: Historia de usuario 2 — Registrar un activo y escanearlo (Prioridad: P1) — Fase C del plan

**Objetivo**: el cliente registra un activo de la lista blanca y lanza un escaneo que el Worker ejecuta con ZAP (perfil demo); el estado avanza solo en pantalla.

**Prueba independiente**: registrar `http://juice-shop:3000`, lanzar escaneo, ver Pendiente → En curso → Terminado sin recargar; URL externa rechazada; segundo escaneo simultáneo → 409.

### Pruebas de la US2 ⚠️

- [ ] T045 [P] [US2] Pruebas de `Activo` y normalización de URL en `tests/Egida.Domain.Tests/Activos/ActivoTests.cs`: nombre vacío o > 100 → error; URL no absoluta o esquema distinto de http/https → error; normalización (esquema y host en minúsculas, sin puerto por defecto, sin fragmento, sin barra final salvo la raíz)
- [ ] T046 [P] [US2] Pruebas de la máquina de estados de `Escaneo` en `tests/Egida.Domain.Tests/Escaneos/EscaneoTests.cs`: Pendiente→EnCurso fija `IniciadoEn`; EnCurso→Terminado fija `FinalizadoEn` y `TotalHallazgos`; Pendiente/EnCurso→Fallido exige motivo; Terminado y Fallido son finales (cualquier transición lanza excepción)
- [ ] T047 [P] [US2] Pruebas de `ListaBlancaObjetivosConfig` en `tests/Egida.Application.Tests/Activos/ListaBlancaTests.cs`: coincide por origen exacto (esquema+host+puerto); `http://juice-shop:3000/ruta` permitido; `https://juice-shop:3000`, `http://juice-shop:3001`, `http://juice-shop.evil.com:3000` rechazados
- [ ] T048 [P] [US2] Pruebas de casos de uso `RegistrarActivo` y `SolicitarEscaneo` en `tests/Egida.Application.Tests/Escaneos/SolicitarEscaneoTests.cs`: objetivo no autorizado → `objetivo_no_autorizado`; activo duplicado → `activo_duplicado`; escaneo con otro activo → `escaneo_en_curso`; auditoría `ActivoRegistrado` / `EscaneoSolicitado`
- [ ] T049 [P] [US2] Pruebas de `EjecutarEscaneo` con `IEscaner` falso en `tests/Egida.Application.Tests/Escaneos/EjecutarEscaneoTests.cs`: objetivo que salió de la lista blanca → Fallido "Objetivo ya no autorizado" sin llamar a ZAP; error del escáner → Fallido "Objetivo no disponible"; superar el tiempo máximo (15 min por defecto) → Fallido "Tiempo excedido"; éxito → Terminado con hallazgos; se publica `EscaneoActualizado` en cada transición
- [ ] T050 [P] [US2] Pruebas de integración en `tests/Egida.Api.Tests/Activos/ActivosTests.cs`: `POST /api/activos` 201 con `Location`; 400 `validacion` (nombre vacío, URL inválida); 400 `objetivo_no_autorizado` para `https://ejemplo.com`; 409 `activo_duplicado`; 403 si lo intenta el Administrador; `GET /api/activos` incluye `ultimoEscaneo` (null si no hay); `GET /api/objetivos-permitidos` 200
- [ ] T051 [P] [US2] Pruebas de integración en `tests/Egida.Api.Tests/Escaneos/SolicitarEscaneoTests.cs`: `POST /api/activos/{id}/escaneos` 202 con `Location` y estado `Pendiente`; segundo POST → 409 `escaneo_en_curso`; dos POST simultáneos → exactamente uno 202 (índice único parcial); `GET /api/escaneos?activoId=` ordenado desc con límite por defecto 20 y máximo 100
- [ ] T052 [P] [US2] Prueba de integración de la cola en `tests/Egida.Api.Tests/Escaneos/ColaEscaneosTests.cs`: `TomarSiguienteAsync` toma el más antiguo y lo pasa a EnCurso; dos consumidores concurrentes no toman el mismo (`FOR UPDATE SKIP LOCKED`); `MarcarInterrumpidosAsync` deja Fallido "Interrumpido" los EnCurso
- [ ] T053 [P] [US2] Pruebas de `ClienteZap` con `HttpMessageHandler` falso en `tests/Egida.Api.Tests/Infraestructura/ClienteZapTests.cs` (este proyecto ya referencia Infrastructure a través de Api): crea contexto con patrón de inclusión solo del origen autorizado, envía `apikey`, desactiva seguimiento de redirecciones fuera del contexto, elimina el contexto al final aunque haya error

### Implementación de la US2

- [ ] T054 [P] [US2] Crear entidad `Activo` en `src/Egida.Domain/Activos/Activo.cs`: `Nombre` "texto(100), obligatorio, 1–100 caracteres sin espacios sobrantes", `Url` "texto(2048); http/https absoluta; normalizada", `PropietarioId`, `CreadoEn`; y `UrlNormalizada` (value object con `Origen`) en `src/Egida.Domain/Activos/UrlNormalizada.cs`
- [ ] T055 [P] [US2] Crear `EstadoEscaneo { Pendiente, EnCurso, Terminado, Fallido }` y entidad `Escaneo` en `src/Egida.Domain/Escaneos/Escaneo.cs` con `ActivoId`, `SolicitadoPorId`, `SolicitadoEn`, `IniciadoEn?`, `FinalizadoEn?`, `MotivoFallo?` "texto(500); obligatorio si Fallido", `TotalHallazgos`; métodos `Iniciar`, `Terminar`, `Fallar(motivo)` según la máquina de estados de data-model.md; constantes de motivos en `src/Egida.Domain/Escaneos/MotivosFallo.cs` ("Tiempo excedido", "Interrumpido", "Objetivo no disponible", "Objetivo ya no autorizado")
- [ ] T056 [P] [US2] Crear abstracciones en `src/Egida.Application/Abstracciones/`: `IRepositorioActivos.cs`, `IRepositorioEscaneos.cs`, `IColaEscaneos.cs` (`TomarSiguienteAsync`, `MarcarInterrumpidosAsync`), `IEscaner.cs` (`EscanearAsync(ObjetivoPermitido, CancellationToken) → IReadOnlyList<AlertaEscaner>`), `IListaBlancaObjetivos.cs`, `IPublicadorEventos.cs` (`PublicarAsync(tipo, propietarioId, datos)`)
- [ ] T057 [P] [US2] Crear `ObjetivoPermitido` (Url, Nombre, UrlsSemilla) y `OpcionesEscaneo` (Objetivos, `TiempoMaximoMinutos = 15`, `MinutosSpider = 1`, `MinutosEscaneoActivo = 2`) en `src/Egida.Application/Escaneos/OpcionesEscaneo.cs`
- [ ] T058 [US2] Implementar `ListaBlancaObjetivosConfig` (comparación por origen exacto, desde `Egida:Escaneo:Objetivos`) en `src/Egida.Infrastructure/Seguridad/ListaBlancaObjetivosConfig.cs`
- [ ] T059 [US2] Crear configuraciones EF `ActivoConfiguracion` (único `propietario_id, url`) y `EscaneoConfiguracion` (índice único parcial `activo_id` WHERE `estado IN ('Pendiente','EnCurso')`; índice `estado, solicitado_en`) en `src/Egida.Infrastructure/Persistencia/Configuraciones/`
- [ ] T060 [US2] Implementar `RepositorioActivos` y `RepositorioEscaneos` (traducen la violación del índice único a `activo_duplicado` / `escaneo_en_curso`) en `src/Egida.Infrastructure/Persistencia/`
- [ ] T061 [US2] Implementar `ColaEscaneosPostgres` (`SELECT … WHERE estado = 'Pendiente' ORDER BY solicitado_en LIMIT 1 FOR UPDATE SKIP LOCKED` + paso a EnCurso en la misma transacción) en `src/Egida.Infrastructure/Cola/ColaEscaneosPostgres.cs`
- [ ] T062 [US2] Implementar `PublicadorEventosPostgres` (`NOTIFY egida_eventos, '{"tipo","propietarioId","datos"}'` vía `pg_notify`) en `src/Egida.Infrastructure/Eventos/PublicadorEventosPostgres.cs`
- [ ] T063 [US2] Implementar `ClienteZap` (HttpClient tipado, API REST JSON con `apikey`) en `src/Egida.Infrastructure/Zap/ClienteZap.cs` con el perfil demo de R1: crear contexto con regex del origen, visitar URLs semilla (`core/action/accessUrl`), spider en alcance (máx. 1 min), esperar fin del pasivo (`pscan/view/recordsToScan` = 0), escaneo activo con la política reducida (máx. 2 min, `ascan/action/stop` al vencer), leer `core/view/alerts?baseurl=`, eliminar contexto y limpiar sesión
- [ ] T064 [US2] Crear política de escaneo activo reducida (SQLi, XSS reflejado/persistente, path traversal, divulgación de archivos) en `src/Egida.Infrastructure/Zap/PoliticaDemo.cs` (se crea/actualiza vía `ascan/action/addScanPolicy` + `setEnabledPolicies` al primer uso)
- [ ] T065 [US2] Implementar casos de uso `RegistrarActivo`, `ListarActivos` y `ObtenerActivo` en `src/Egida.Application/Activos/` (Cliente ve solo los suyos; recurso ajeno → 404 + auditoría `AccesoDenegado`)
- [ ] T066 [US2] Implementar casos de uso `SolicitarEscaneo` y `ListarEscaneos` en `src/Egida.Application/Escaneos/` (verifica lista blanca, crea Pendiente, audita, publica `EscaneoActualizado`)
- [ ] T067 [US2] Implementar caso de uso `EjecutarEscaneo` en `src/Egida.Application/Escaneos/EjecutarEscaneo.cs` (re-verifica lista blanca, aplica tiempo máximo con `CancellationTokenSource`, llama a `IEscaner`, guarda resultado y publica eventos; la normalización a hallazgos se conecta en US3)
- [ ] T068 [US2] Implementar `ProcesadorEscaneos` (BackgroundService: al arrancar `MarcarInterrumpidosAsync`; luego cada 3 s `TomarSiguienteAsync` → `EjecutarEscaneo`, uno a la vez) en `src/Egida.Worker/ProcesadorEscaneos.cs` y registrar en `src/Egida.Worker/Program.cs`
- [ ] T069 [US2] Crear la migración `ActivosYEscaneos` en `src/Egida.Infrastructure/Persistencia/Migraciones/`
- [ ] T070 [US2] Crear endpoints `GET /api/objetivos-permitidos`, `GET/POST /api/activos`, `GET /api/activos/{id}` en `src/Egida.Api/Endpoints/EndpointsActivos.cs` y `POST /api/activos/{id}/escaneos`, `GET /api/escaneos`, `GET /api/escaneos/{id}` en `src/Egida.Api/Endpoints/EndpointsEscaneos.cs` según [contracts/api-rest.md](./contracts/api-rest.md)
- [ ] T071 [US2] Implementar `HubNotificaciones` (`/hubs/notificaciones`, `[Authorize]`, sin métodos cliente→servidor; al conectar agrega a `usuario-{id}` y, si es Administrador, a `administradores`) en `src/Egida.Api/Hubs/HubNotificaciones.cs`
- [ ] T072 [US2] Implementar `ReenviadorEventos` (BackgroundService con `LISTEN egida_eventos` en Npgsql, reconexión con backoff; reenvía a `usuario-{propietarioId}` y `administradores`) en `src/Egida.Api/Servicios/ReenviadorEventos.cs`; registrar SignalR y el hub en `src/Egida.Api/Program.cs`
- [ ] T073 [US2] Configurar en `docker-compose.yml` la lista blanca del Worker y la API: `EGIDA__Escaneo__Objetivos__0__Url=http://juice-shop:3000`, `…__Nombre`, `…__UrlsSemilla__0..2` (`/rest/products/search?q=manzana`, `/ftp`, `/api/Products`), `Zap__Url=http://zap:8080`, `Zap__ClaveApi=${ZAP_API_KEY}`
- [ ] T074 [P] [US2] Crear conexión SignalR en `egida-web/src/tiempo-real/ConexionTiempoReal.tsx` (`accessTokenFactory`, `withAutomaticReconnect()`; cada evento invalida las consultas relacionadas; al reconectar invalida todas)
- [ ] T075 [P] [US2] Crear componente `egida-web/src/componentes/EstadoEscaneo.tsx` (Pendiente, En curso, Terminado, Fallido + motivo)
- [ ] T076 [US2] Crear página `egida-web/src/paginas/Activos.tsx`: lista con estado y fecha del último escaneo; formulario de registro (sugiere la URL de `/api/objetivos-permitidos`, muestra errores por campo y "Solo se permiten objetivos del entorno de pruebas autorizado"); botón "Lanzar escaneo" que muestra "Ya hay un escaneo en marcha para este activo" ante 409
- [ ] T077 [US2] **Validación R1 (bloqueante)**: con `docker compose up`, escanear Juice Shop real y confirmar < 3 min y ≥ 1 alerta `riskcode = 3`; anotar el tiempo y las alertas Altas obtenidas en `specs/001-prototipo-demostrable/research.md` (R1, sección "Resultado de la validación"); si falla, ajustar URLs semilla / política antes de seguir

**Checkpoint**: US2 funciona y se prueba por separado; R1 validado.

---

## Fase 5: Historia de usuario 3 — Ver hallazgos clasificados (Prioridad: P1) — Fase C del plan

**Objetivo**: al terminar el escaneo, cada hallazgo tiene nombre, severidad, categoría OWASP Top 10:2025, URL afectada y recomendación.

**Prueba independiente**: abrir un escaneo terminado y verificar los cinco campos en todos los hallazgos; filtrar/ordenar por severidad; escaneo sin hallazgos → mensaje claro.

### Pruebas de la US3 ⚠️

- [ ] T078 [P] [US3] Pruebas de `MapeadorOwasp` en `tests/Egida.Domain.Tests/Clasificacion/MapeadorOwaspTests.cs`: etiqueta `OWASP_2025_A05` → `A05:2025 - Injection` (`EtiquetaZap2025`); solo `OWASP_2021_A03` → traducida con la tabla 2021→2025 (`EtiquetaZap2021`); sin etiquetas y CWE 79 → por tabla CWE (`TablaCwe`); sin nada → `A02:2025 - Security Misconfiguration` (`PorDefecto`); todas las salidas cumplen el formato `A0X:2025 - <Nombre>`
- [ ] T079 [P] [US3] Pruebas de `Hallazgo` en `tests/Egida.Domain.Tests/Escaneos/HallazgoTests.cs`: el constructor rechaza severidad o categoría ausente (principio III); rechaza `ConfianzaEscaner` fuera de 1–3; evidencia se trunca a 2000; la severidad no tiene setter público (FR-026); estado IA inicial `Pendiente`
- [ ] T080 [P] [US3] Pruebas de `NormalizadorAlertasZap` en `tests/Egida.Api.Tests/Infraestructura/NormalizadorAlertasZapTests.cs` con un JSON de alertas ZAP de ejemplo en `tests/Egida.Api.Tests/Infraestructura/Datos/alertas-juice-shop.json`: `riskcode` 3/2/1/0 → Alta/Media/Baja/Informativa; confianza 0 descartada; misma (plugin + URL + parámetro) agrupada con `Ocurrencias`; `solution` vacía → recomendación de la tabla por categoría
- [ ] T081 [P] [US3] Pruebas de integración en `tests/Egida.Api.Tests/Escaneos/HallazgosTests.cs`: `GET /api/escaneos/{id}/hallazgos` devuelve los campos del contrato; filtro `severidad=Alta`; `orden=severidad` (severidad desc, luego Prioridad IA desc); escaneo sin hallazgos → lista vacía; `GET /api/escaneos/{id}` audita `ResultadoEscaneoConsultado` e incluye `conteoPorSeveridad`

### Implementación de la US3

- [ ] T082 [P] [US3] Crear enums `Severidad { Informativa = 0, Baja = 1, Media = 2, Alta = 3 }`, `PrioridadIA { Baja = 1, Media = 2, Alta = 3 }`, `EstadoAnalisisIA { Pendiente, Analizado }` en `src/Egida.Domain/Escaneos/` y `OrigenCategoriaOwasp { EtiquetaZap2025, EtiquetaZap2021, TablaCwe, PorDefecto }` en `src/Egida.Domain/Clasificacion/OrigenCategoriaOwasp.cs`
- [ ] T083 [P] [US3] Crear tablas `src/Egida.Domain/Clasificacion/CategoriasOwasp2025.cs` (A01–A10 con nombre), `TablaOwasp2021a2025.cs`, `TablaCweOwasp2025.cs` (al menos CWE 22, 79, 89, 200, 352, 525, 693, 1021, 1275 y los que aparezcan en la validación R1) y `RecomendacionesPorCategoria.cs`; documentar la regla en comentarios XML
- [ ] T084 [US3] Implementar `MapeadorOwasp` (reglas puras en el orden de R3) en `src/Egida.Domain/Clasificacion/MapeadorOwasp.cs`
- [ ] T085 [US3] Crear entidad `Hallazgo` en `src/Egida.Domain/Escaneos/Hallazgo.cs` con los campos de data-model.md: `Nombre` "texto(300) obligatorio", `Descripcion` obligatoria, `Severidad` obligatoria e inmutable, `CategoriaOwasp` "texto(60), obligatorio, formato A0X:2025 - <Nombre>", `OrigenCategoria`, `UrlAfectada` "texto(2048) obligatoria", `Recomendacion` obligatoria, `MetodoHttp?` "texto(10)", `Parametro?` "texto(300)", `Evidencia?` "texto(2000) truncada", `CweId?`, `PluginId`, `ConfianzaEscaner` "1–3", `Ocurrencias` "≥ 1", campos IA (`EstadoIA` inicial Pendiente, `PrioridadIA?`, `ProbabilidadFalsoPositivo?` "decimal(5,4) en [0, 1]", `EsPosibleFalsoPositivo` calculado `>= 0.5`, `JustificacionIA?` "texto(500)", `VersionModeloIA?` "texto(40)", `AnalizadoEn?`, `IntentosIA`)
- [ ] T086 [US3] Crear `AlertaEscaner` (DTO crudo de ZAP: pluginId, alert, riskcode, confidence, url, method, param, evidence, cweid, solution, description, tags) en `src/Egida.Application/Escaneos/AlertaEscaner.cs` e implementar `NormalizadorAlertasZap` en `src/Egida.Infrastructure/Zap/NormalizadorAlertasZap.cs`
- [ ] T087 [US3] Crear `HallazgoConfiguracion` (índices `escaneo_id, severidad desc` y `estado_ia`; `es_posible_falso_positivo` como columna calculada o propiedad ignorada) en `src/Egida.Infrastructure/Persistencia/Configuraciones/` y la migración `Hallazgos`
- [ ] T088 [US3] Conectar la normalización en `EjecutarEscaneo` (alertas → hallazgos, `TotalHallazgos`, todo en la misma transacción que `Terminar`) en `src/Egida.Application/Escaneos/EjecutarEscaneo.cs`
- [ ] T089 [US3] Implementar caso de uso `ConsultarHallazgos` (filtro por severidad, órdenes `severidad` y `prioridadIA` según el contrato) en `src/Egida.Application/Escaneos/ConsultarHallazgos.cs` y endpoint `GET /api/escaneos/{id}/hallazgos` en `src/Egida.Api/Endpoints/EndpointsEscaneos.cs`; agregar `conteoPorSeveridad` a las respuestas de escaneo
- [ ] T090 [P] [US3] Crear componente `egida-web/src/componentes/InsigniaSeveridad.tsx` (cuatro colores) y `egida-web/src/componentes/TablaHallazgos.tsx` (nombre, severidad, OWASP, URL, recomendación desplegable; filtro y orden por severidad)
- [ ] T091 [US3] Crear página `egida-web/src/paginas/DetalleEscaneo.tsx` (ruta `/escaneos/:id`): datos del escaneo con `EstadoEscaneo`, tabla de hallazgos y mensaje "No se encontraron vulnerabilidades" si está vacía
- [ ] T092 [US3] Verificar con un escaneo real que el 100 % de hallazgos tiene severidad y categoría (SC-003) y confirmar en `research.md` R3 si `zap-stable` trae etiquetas `OWASP_2025_*` (si no, la tabla 2021→2025 queda activa)

**Checkpoint**: US1–US3 funcionan (flujo P1 completo = MVP demostrable).

---

## Fase 6: Historia de usuario 4 — Priorización por IA (Prioridad: P2) — Fase D del plan

**Objetivo**: cada hallazgo muestra Prioridad IA, posible falso positivo y justificación; si la IA cae, todo sigue con "análisis IA pendiente" y se reintenta solo.

**Prueba independiente**: con `egida-ia` arriba, hallazgos analizados; con `docker compose stop egida-ia`, escaneo Terminado con hallazgos Pendiente; al arrancarla, en ≤ 60 s pasan a Analizado.

### Pruebas de la US4 ⚠️

- [ ] T093 [US4] Agregar al `.github/workflows/ci.yml` el job Python: `astral-sh/setup-uv`, `uv sync` y `uv run pytest` en `egida-ia/`, más `uv run pip-audit`
- [ ] T094 [P] [US4] Pruebas pytest de `/analizar` en `egida-ia/tests/test_analizar.py`: camino feliz (un resultado por id, prioridad en Alta/Media/Baja, probabilidad en [0, 1] con 4 decimales, justificación en español de 1–500 caracteres); 422 por campo faltante, enum inválido, `confianza_escaner` fuera de 1–3, ids duplicados o lote > 500; determinismo (misma entrada → misma salida)
- [ ] T095 [P] [US4] Pruebas pytest de `/salud` en `egida-ia/tests/test_salud.py`: 200 con `version_modelo`; 503 si el modelo no está cargado
- [ ] T096 [P] [US4] Pruebas de `Hallazgo.RegistrarAnalisisIA` en `tests/Egida.Domain.Tests/Escaneos/HallazgoIATests.cs`: probabilidad 0.5 → posible FP; 0.4999 → no; probabilidad fuera de [0, 1] → error; la severidad no cambia
- [ ] T097 [P] [US4] Pruebas de `ClienteIA` con handler falso en `tests/Egida.Api.Tests/Infraestructura/ClienteIATests.cs`: respuesta válida → resultados; ids que faltan o sobran, prioridad desconocida o probabilidad fuera de rango → lote completo no analizado; timeout/500 → no analizado
- [ ] T098 [P] [US4] Pruebas de `AnalizarHallazgos` y `ReintentarPendientes` en `tests/Egida.Application.Tests/AnalisisIA/AnalizarHallazgosTests.cs`: IA caída → hallazgos Pendiente, `IntentosIA` +1 y escaneo igual Terminado; fallo a mitad (lotes) → los ya analizados se conservan; reintento exitoso publica `HallazgosAnalizados`
- [ ] T099 [P] [US4] Prueba de integración del orden `prioridadIA` en `tests/Egida.Api.Tests/Escaneos/OrdenPrioridadIATests.cs`: Alta, Media, Baja, luego pendientes; dentro de cada grupo por severidad desc

### Implementación de la US4

- [ ] T100 [P] [US4] Crear `egida-ia/pyproject.toml` (Python 3.12, fastapi, uvicorn, scikit-learn, pandas, joblib; dev: pytest, httpx, pip-audit) y generar `egida-ia/uv.lock`
- [ ] T101 [P] [US4] Crear `egida-ia/scripts/generar_dataset.py` (~400 filas sintéticas con reglas expertas + ruido, semilla fija; columnas: riesgo, confianza, plugin_id, cwe_id, categoria_owasp, metodo_http, tiene_parametro, tiene_evidencia, longitud_evidencia, tipo_contenido, url_estatica, es_falso_positivo, prioridad) y generar `egida-ia/datos/hallazgos_sinteticos.csv`
- [ ] T102 [US4] Crear `egida-ia/app/modelo.py` (extracción de variables de R8, dos `RandomForestClassifier` con `random_state` fijo, guardar/cargar con joblib en `egida-ia/modelos/`, `version_modelo = "rf-2026.11.1"`) y `egida-ia/scripts/entrenar.py`
- [ ] T103 [P] [US4] Crear `egida-ia/app/esquemas.py` (Pydantic según [contracts/ia-analizar.md](./contracts/ia-analizar.md): validaciones de la tabla, máx. 500 hallazgos, ids únicos, evidencia truncada a 2000)
- [ ] T104 [P] [US4] Crear `egida-ia/app/justificacion.py` (plantillas en español a partir de las variables de mayor peso; 1–500 caracteres)
- [ ] T105 [US4] Crear `egida-ia/app/main.py` (FastAPI: `POST /analizar`, `GET /salud`, 503 si no hay modelo; al arrancar carga el modelo y, si falta, entrena)
- [ ] T106 [US4] Crear `egida-ia/Dockerfile` (python:3.12-slim + uv, entrena en el build, usuario no root, `uvicorn app.main:app --port 8000`) y agregar el servicio `egida-ia` a `docker-compose.yml` (red-app, sin puertos al host, healthcheck `/salud`; `worker` depende de él)
- [ ] T107 [US4] Agregar `Hallazgo.RegistrarAnalisisIA(prioridad, probabilidad, justificacion, version, ahora)` y `RegistrarFalloIA()` en `src/Egida.Domain/Escaneos/Hallazgo.cs`
- [ ] T108 [P] [US4] Crear `IAnalizadorIA` en `src/Egida.Application/Abstracciones/IAnalizadorIA.cs` e implementar `ClienteIA` (HttpClient tipado con `AddResilienceHandler`: timeout 10 s, 2 reintentos con backoff, circuit breaker; validación de contrato; JSON snake_case) en `src/Egida.Infrastructure/IA/ClienteIA.cs`
- [ ] T109 [US4] Implementar casos de uso `AnalizarHallazgos` y `ReintentarPendientes` en `src/Egida.Application/AnalisisIA/` (publican `HallazgosAnalizados { escaneoId, analizados, pendientes }`)
- [ ] T110 [US4] Llamar a `AnalizarHallazgos` desde `EjecutarEscaneo` después de marcar Terminado (sin afectar el estado del escaneo) en `src/Egida.Application/Escaneos/EjecutarEscaneo.cs`
- [ ] T111 [US4] Implementar `ReintentadorAnalisisIA` (BackgroundService cada 60 s: si `/salud` responde, reanaliza pendientes) en `src/Egida.Worker/ReintentadorAnalisisIA.cs` y registrarlo en `src/Egida.Worker/Program.cs`
- [ ] T112 [US4] Incluir el objeto `ia` en la respuesta de hallazgos (campos null si Pendiente) y el orden `prioridadIA` en `src/Egida.Application/Escaneos/ConsultarHallazgos.cs`
- [ ] T113 [P] [US4] Crear `egida-web/src/componentes/InsigniaPrioridadIA.tsx` (estilo visual distinto al de severidad: contorno + icono, rotulado "Prioridad IA"; "análisis IA pendiente" si aplica; marca "posible falso positivo")
- [ ] T114 [US4] Agregar a `egida-web/src/componentes/TablaHallazgos.tsx` la columna "Prioridad IA", la marca de FP, la justificación y el orden por Prioridad IA

**Checkpoint**: US4 funciona; escenario de degradación de IA del quickstart OK.

---

## Fase 7: Historia de usuario 5 — Alerta por correo de severidad alta (Prioridad: P2) — Fase E del plan

**Objetivo**: un único correo al cliente dueño del activo cuando el escaneo termina con ≥ 1 hallazgo Alto, con reintentos y estado visible.

**Prueba independiente**: escanear Juice Shop y ver en Mailpit (`http://localhost:8025`) el correo con resumen, FP marcados y enlace funcional; escaneo sin Altos → sin correo.

### Pruebas de la US5 ⚠️

- [ ] T115 [P] [US5] Pruebas de `Alerta` en `tests/Egida.Domain.Tests/Alertas/AlertaTests.cs`: no se crea con 0 Altas; fallo intermedio deja Pendiente, incrementa `IntentosEnvio` y fija `ProximoIntentoEn` (10 s, 30 s, 90 s); 3.er fallo → Fallido; envío OK → Enviado con `EnviadaEn`
- [ ] T116 [P] [US5] Pruebas de `GenerarAlerta` en `tests/Egida.Application.Tests/Alertas/GenerarAlertaTests.cs`: escaneo con Altas → una alerta con destinatario = propietario del activo (también si lo lanzó el Administrador); todas las Altas como posible FP → alerta igual con `TotalPosiblesFp`; sin Altas → ninguna alerta
- [ ] T117 [P] [US5] Pruebas de la plantilla en `tests/Egida.Api.Tests/Infraestructura/PlantillaCorreoTests.cs`: asunto "ÉGIDA – Alerta: N hallazgos de severidad Alta en <activo>"; incluye fecha, conteo por severidad, lista de Altas con Prioridad IA, "posible falso positivo" o "análisis IA pendiente", y enlace `{EGIDA_URL_PUBLICA}/escaneos/{id}`
- [ ] T118 [P] [US5] Pruebas de integración en `tests/Egida.Api.Tests/Alertas/AlertasTests.cs`: `GET /api/alertas` del cliente devuelve solo las suyas con `estadoEnvio`; límite; índice único `escaneo_id` impide duplicados

### Implementación de la US5

- [ ] T119 [P] [US5] Crear `EstadoEnvio { Pendiente, Enviado, Fallido }` y entidad `Alerta` en `src/Egida.Domain/Alertas/Alerta.cs`: `EscaneoId` "único", `DestinatarioId` "siempre el propietario del activo", `CorreoDestino` "texto(254)", `CreadaEn`, `TotalAltas` "≥ 1", `TotalPosiblesFp`, `Resumen` "texto(1000)", `EstadoEnvio` inicial Pendiente, `IntentosEnvio` "0–3", `ProximoIntentoEn?`, `UltimoError?` "texto(500)", `EnviadaEn?`; métodos `RegistrarEnvioExitoso`, `RegistrarFalloEnvio`
- [ ] T120 [US5] Crear `AlertaConfiguracion` (único `escaneo_id`) en `src/Egida.Infrastructure/Persistencia/Configuraciones/` y la migración `Alertas`
- [ ] T121 [P] [US5] Crear `IEnviadorCorreo` y `IRepositorioAlertas` en `src/Egida.Application/Abstracciones/`
- [ ] T122 [US5] Implementar `GenerarAlerta` en `src/Egida.Application/Alertas/GenerarAlerta.cs` y llamarla desde `EjecutarEscaneo` en la misma transacción que marca Terminado (patrón outbox, R10); publica `AlertaCreada`
- [ ] T123 [US5] Crear plantillas HTML + texto en español en `src/Egida.Infrastructure/Correo/Plantillas/AlertaEscaneo.html` y `AlertaEscaneo.txt` (recursos incrustados) y `ConstructorCorreoAlerta` en `src/Egida.Application/Alertas/ConstructorCorreoAlerta.cs` (lee el estado IA actual de cada Alta al momento del envío)
- [ ] T124 [US5] Implementar `EnviadorCorreoMailKit` (SMTP `Correo:Host`/`Puerto`, remitente `alertas@egida.test`) en `src/Egida.Infrastructure/Correo/EnviadorCorreoMailKit.cs`
- [ ] T125 [US5] Implementar caso de uso `EnviarAlerta` en `src/Egida.Application/Alertas/EnviarAlerta.cs` y `EnviadorAlertas` (BackgroundService que procesa alertas Pendiente con `ProximoIntentoEn <= ahora`; publica `AlertaEnvioActualizado`) en `src/Egida.Worker/EnviadorAlertas.cs`
- [ ] T126 [US5] Agregar al `worker` en `docker-compose.yml`: `Correo__Host=mailpit`, `Correo__Puerto=1025`, `Egida__UrlPublica=${EGIDA_URL_PUBLICA}`
- [ ] T127 [US5] Implementar `ListarAlertas` en `src/Egida.Application/Alertas/ListarAlertas.cs`, endpoint `GET /api/alertas` en `src/Egida.Api/Endpoints/EndpointsAlertas.cs`, y `alerta { id, estadoEnvio }` en `GET /api/escaneos/{id}`
- [ ] T128 [US5] Crear página `egida-web/src/paginas/Alertas.tsx` (lista con activo, fecha, total Altas, posibles FP, estado de envío y enlace al escaneo)

**Checkpoint**: US5 funciona; correo visible en Mailpit en < 1 min (SC-004).

---

## Fase 8: Historia de usuario 6 — Dashboard en tiempo real (Prioridad: P3) — Fase E del plan

**Objetivo**: panel con total de activos, 5 escaneos recientes, hallazgos por severidad y 5 alertas recientes, actualizado solo.

**Prueba independiente**: dashboard abierto en una pestaña, escaneo lanzado en otra; los indicadores cambian solos en < 5 s al terminar.

### Pruebas de la US6 ⚠️

- [ ] T129 [P] [US6] Pruebas de `ObtenerResumen` en `tests/Egida.Application.Tests/Dashboard/ObtenerResumenTests.cs`: alcance Cliente (solo propios) vs Administrador (global); usuario sin datos → todo en 0 y listas vacías; máximo 5 escaneos y 5 alertas, ordenados desc
- [ ] T130 [P] [US6] Prueba de integración `GET /api/dashboard` en `tests/Egida.Api.Tests/Dashboard/DashboardTests.cs` (formato del contrato)
- [ ] T131 [P] [US6] Prueba de integración del hub en `tests/Egida.Api.Tests/TiempoReal/HubNotificacionesTests.cs`: conexión sin token rechazada; un `NOTIFY egida_eventos` llega como `EscaneoActualizado` al cliente dueño y al administrador, pero no a otro cliente

### Implementación de la US6

- [ ] T132 [US6] Implementar `ObtenerResumen` en `src/Egida.Application/Dashboard/ObtenerResumen.cs` y endpoint `GET /api/dashboard` en `src/Egida.Api/Endpoints/EndpointsDashboard.cs`
- [ ] T133 [US6] Crear página `egida-web/src/paginas/Dashboard.tsx` (tarjetas de indicadores, escaneos recientes con `EstadoEscaneo`, hallazgos por severidad, alertas recientes, invitación "Registra tu primer activo" si no hay activos); invalidación por `EscaneoActualizado`, `HallazgosAnalizados`, `AlertaCreada` y `AlertaEnvioActualizado` en `egida-web/src/tiempo-real/ConexionTiempoReal.tsx`

**Checkpoint**: US6 funciona; SC-005 verificado.

---

## Fase 9: Historia de usuario 7 — Vista global del administrador (Prioridad: P3) — Fase E del plan

**Objetivo**: el administrador ve todo con el propietario identificado y puede lanzar escaneos sobre cualquier activo; un cliente no accede a recursos ajenos.

**Prueba independiente**: como admin ver el activo del cliente y relanzar su escaneo (llega un segundo correo al cliente); pruebas automáticas con un segundo cliente para SC-008.

### Pruebas de la US7 ⚠️

- [ ] T134 [P] [US7] Pruebas de aislamiento (SC-008) en `tests/Egida.Api.Tests/Autorizacion/AislamientoClientesTests.cs`: crear un segundo cliente de prueba directamente en la BD; ese cliente recibe 404 en `GET /api/activos/{id}`, `POST /api/activos/{id}/escaneos`, `GET /api/escaneos/{id}`, `GET /api/escaneos/{id}/hallazgos` de recursos ajenos, no los ve en listas ni en `/api/alertas` ni en `/api/dashboard`, y cada intento queda auditado como `AccesoDenegado`
- [ ] T135 [P] [US7] Pruebas de administrador en `tests/Egida.Api.Tests/Autorizacion/AdministradorTests.cs`: lista activos y escaneos de todos con `propietario`; `POST /api/activos/{id}/escaneos` sobre activo de cliente → 202 y auditoría `EscaneoSolicitadoPorAdmin`; `POST /api/activos` → 403
- [ ] T136 [P] [US7] Prueba en `tests/Egida.Application.Tests/Alertas/AlertaEscaneoAdminTests.cs`: escaneo lanzado por el admin con Altas → alerta con destinatario = cliente dueño

### Implementación de la US7

- [ ] T137 [US7] Revisar los casos de uso de `src/Egida.Application/Activos/`, `Escaneos/`, `Alertas/` y `Dashboard/` para que el filtro por propietario use `IUsuarioActual.EsAdministrador` de forma uniforme (extraer `FiltroAlcance` en `src/Egida.Application/Comun/FiltroAlcance.cs`) y que `SolicitarEscaneo` audite `EscaneoSolicitadoPorAdmin` cuando corresponda
- [ ] T138 [US7] En el frontend, mostrar la columna "Propietario" en `egida-web/src/paginas/Activos.tsx` y en las listas de escaneos/alertas cuando el rol es Administrador, y ocultar el formulario de registro de activos para el Administrador

**Checkpoint**: todas las historias funcionan por separado.

---

## Fase 10: Pulido y temas transversales — Fase E del plan

- [ ] T139 [P] Crear `egida-web/Dockerfile` (node:22 build → nginx) y `egida-web/nginx.conf` (fallback SPA a `index.html`, proxy `/api` y `/hubs` a `api:8080` con soporte WebSocket, cabeceras CSP, `X-Content-Type-Options`, `Referrer-Policy`); agregar servicio `web` (puerto 5173) a `docker-compose.yml`
- [ ] T140 Agregar al `.github/workflows/ci.yml`: job web (`npm ci`, `npm run build`), CodeQL (C#, Python, JS/TS), `actions/dependency-review-action` en PR, `dotnet list package --vulnerable`
- [ ] T141 [P] Agregar `npm run lint` (ESLint de la plantilla Vite) y corregir avisos en `egida-web/`
- [ ] T142 Revisar el uso de memoria con `docker stats` durante un escaneo (≤ 6 GB total, ZAP ≤ 1.5 GB) y anotar el resultado en `specs/001-prototipo-demostrable/quickstart.md`
- [ ] T143 [P] Revisión de seguridad: ningún secreto en el repo (`git grep -i "password\|clave"`), `.env` ignorado, todos los endpoints con `[Authorize(Roles)]` o `[AllowAnonymous]` explícito, Swagger solo en Development
- [ ] T144 Ejecutar el quickstart completo (escenarios 1–15, degradación de IA y reinicio del Worker) con cronómetro y confirmar < 5 min (SC-001); anotar resultados y desviaciones en `specs/001-prototipo-demostrable/quickstart.md`
- [ ] T145 [P] Actualizar `README.md` con cómo levantar, usuarios semilla, arquitectura (diagrama de contenedores) y "trabajo futuro" (Hangfire, Redis, spider AJAX, nube)

---

## Dependencias y orden de ejecución

### Dependencias entre fases

- **Setup (F1)**: sin dependencias.
- **Fundacional (F2)**: depende de F1. Bloquea todas las historias.
- **US1 (F3)**: depende de F2. Todas las demás historias necesitan login (JWT) para sus pruebas de integración → US1 se hace primero.
- **US2 (F4)**: depende de US1.
- **US3 (F5)**: depende de US2 (necesita escaneos que produzcan alertas ZAP).
- **US4 (F6)**: depende de US3 (necesita hallazgos). El servicio `egida-ia` (T100–T106) no depende de nada del backend y puede empezar en paralelo desde F2.
- **US5 (F7)**: depende de US3; aprovecha US4 si está (marca FP/pendiente), pero funciona con todo "análisis IA pendiente".
- **US6 (F8)**: depende de US2 y US3; muestra alertas si US5 está.
- **US7 (F9)**: depende de US2; sus pruebas se amplían si US3–US6 están.
- **Pulido (F10)**: después de las historias deseadas.

### Dentro de cada historia

- Pruebas primero (deben fallar) → dominio → abstracciones → infraestructura → casos de uso → endpoints → frontend.
- Al cerrar cada fase: pruebas en verde, commit (Conventional Commits en español) y push.

### Oportunidades de paralelismo

- F1: T006, T008, T009 en paralelo.
- F2: T011–T014 en paralelo; T024–T026 (frontend) en paralelo con todo el backend.
- Cada historia: todas sus pruebas `[P]` se escriben en paralelo; las entidades de dominio `[P]` también.
- `egida-ia` (T100–T106 y T094–T095) completo en paralelo con F4/F5.

---

## Ejemplo de paralelismo: US2

```text
# Pruebas de la US2 juntas:
T045 tests/Egida.Domain.Tests/Activos/ActivoTests.cs
T046 tests/Egida.Domain.Tests/Escaneos/EscaneoTests.cs
T047 tests/Egida.Application.Tests/Activos/ListaBlancaTests.cs
T050 tests/Egida.Api.Tests/Activos/ActivosTests.cs

# Dominio y abstracciones de la US2 juntos:
T054 src/Egida.Domain/Activos/Activo.cs
T055 src/Egida.Domain/Escaneos/Escaneo.cs
T056 src/Egida.Application/Abstracciones/*
T057 src/Egida.Application/Escaneos/OpcionesEscaneo.cs
```

---

## Estrategia de implementación

### MVP primero (US1 + US2 + US3)

1. F1 Setup → F2 Fundacional.
2. F3 US1 → validar login, bloqueo y 401.
3. F4 US2 → **validar R1 (T077)** antes de seguir: si Juice Shop no da ≥ 1 Alto en < 3 min, se ajusta el perfil.
4. F5 US3 → hallazgos clasificados. **PARAR y VALIDAR**: es el MVP demostrable (login → activo → escaneo → hallazgos).

### Entrega incremental

1. MVP (US1–US3) → demo parcial.
2. + US4 (IA) → diferenciador de la tesis.
3. + US5 (correo) → "alertamiento" del título.
4. + US6 y US7 → dashboard y vista de administrador.
5. Pulido → quickstart completo en < 5 min.

---

## Notas

- `[P]` = archivos distintos y sin dependencias pendientes.
- `[USn]` permite rastrear cada tarea hasta su historia.
- Las restricciones de campo citadas entre comillas vienen literalmente de [data-model.md](./data-model.md).
- Comandos para el usuario: PowerShell, uno a la vez.
