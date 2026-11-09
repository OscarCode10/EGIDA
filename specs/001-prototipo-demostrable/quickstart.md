# Guía de validación (quickstart): Prototipo demostrable de ÉGIDA

Escenarios ejecutables que prueban el flujo de punta a punta. Contratos: [api-rest.md](./contracts/api-rest.md), [ia-analizar.md](./contracts/ia-analizar.md), [signalr-notificaciones.md](./contracts/signalr-notificaciones.md). Entidades: [data-model.md](./data-model.md).

## Prerrequisitos

- Windows 11 con Docker Desktop (WSL 2) en marcha y al menos 6 GB de RAM asignados a Docker.
- .NET SDK 10, Node 20+ (se recomienda 22 LTS), uv. Solo hacen falta para correr pruebas fuera de Docker.
- Puertos libres: 5173 (web), 5080 (API), 8025 (Mailpit).

## Levantar el sistema

Ejecutar en PowerShell desde la raíz del repo, un comando a la vez:

```powershell
Copy-Item .env.example .env
```

Editar `.env` y poner valores propios en `JWT_CLAVE`, `POSTGRES_PASSWORD`, `ZAP_API_KEY`, `SEMILLA_ADMIN_CONTRASENA` y `SEMILLA_CLIENTE_CONTRASENA` (siempre ficticios, nunca datos reales).

```powershell
docker compose up -d --build
```

```powershell
docker compose ps
```

**Esperado**: todos los servicios en `running` / `healthy`. ZAP puede tardar ~60 s en quedar sano.

## Escenarios (guion de la demo, < 5 min)

| # | Paso | Resultado esperado | Cubre |
|---|---|---|---|
| 1 | Abrir `http://localhost:5173` sin sesión | Redirige a `/login` | HU1-4 |
| 2 | Entrar con `cliente@egida.test` y una contraseña errónea | "Correo o contraseña incorrectos" | HU1-3, FR-005 |
| 3 | Entrar con `cliente@egida.test` y la contraseña correcta | Dashboard del cliente con indicadores en 0 | HU1-1, HU6-3 |
| 4 | Activos → Registrar: nombre "Juice Shop", URL `https://ejemplo.com` | Rechazo: "Solo se permiten objetivos del entorno de pruebas autorizado" | HU2-2, SC-007 |
| 5 | Registrar con URL `http://juice-shop:3000` | Aparece en la lista | HU2-1 |
| 6 | Lanzar escaneo | Aparece "Pendiente" de inmediato y pasa a "En curso" sin recargar | HU2-4/5, SC-009 |
| 7 | Pulsar "Lanzar escaneo" otra vez | "Ya hay un escaneo en marcha para este activo" | HU2-7 |
| 8 | Esperar (< 3 min) | Estado "Terminado" sin recargar | SC-002, SC-005 |
| 9 | Abrir el detalle | Hallazgos con severidad, OWASP, URL, recomendación y columna "Prioridad IA" | HU3, HU4, SC-003 |
| 10 | Ordenar por Prioridad IA | Primero Alta, luego Media, luego Baja | HU4-4 |
| 11 | Abrir `http://localhost:8025` (Mailpit) | Un correo "ÉGIDA – Alerta: N hallazgos de severidad Alta en Juice Shop", con los posibles FP marcados | HU5-1/2, SC-004 |
| 12 | Clic en el enlace del correo | Abre el detalle del escaneo | HU5-5 |
| 13 | Volver al dashboard | Activos = 1, escaneo reciente, conteos por severidad, alerta reciente | HU6-1/2 |
| 14 | Cerrar sesión y entrar como `admin@egida.test` | Vista global con el activo del cliente y su propietario | HU7-1/2 |
| 15 | Como admin, relanzar el escaneo | Termina y llega un **segundo** correo a `cliente@egida.test` | HU7-3 |

## Escenario de degradación de la IA (SC-006)

```powershell
docker compose stop egida-ia
```

Lanzar un escaneo nuevo como cliente.

**Esperado**: el escaneo termina; los hallazgos se ven con su severidad y la marca "análisis IA pendiente"; el correo indica "análisis IA pendiente".

```powershell
docker compose start egida-ia
```

**Esperado**: en ≤ 60 s los hallazgos pasan a "Analizado" sin recargar (FR-025).

## Escenario de reinicio del Worker (caso límite)

Lanzar un escaneo y, mientras está "En curso":

```powershell
docker compose restart worker
```

**Esperado**: el escaneo queda "Fallido – Interrumpido" y se puede lanzar uno nuevo.

## Pruebas automatizadas

```powershell
dotnet test Egida.slnx
```

```powershell
cd egida-ia
```

```powershell
uv run pytest
```

```powershell
cd ..
```

**Esperado**: todo en verde. `Egida.Api.Tests` incluye el aislamiento entre clientes (SC-008) con un segundo cliente de prueba creado por la propia prueba.

## Apagar

```powershell
docker compose down
```

Para borrar también los datos (volumen de PostgreSQL):

```powershell
docker compose down -v
```
