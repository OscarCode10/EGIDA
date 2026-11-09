# Modelo de datos (Fase 1): Prototipo demostrable de ÉGIDA

**Funcionalidad**: [spec.md](./spec.md) | **Investigación**: [research.md](./research.md)

Las entidades viven en `Egida.Domain` (sin dependencias externas). La persistencia la hace EF Core en `Egida.Infrastructure`, con nombres de tabla y columna en `snake_case`. Los identificadores son `Guid` (v7, ordenables por tiempo) y las fechas `DateTimeOffset` en UTC.

## Enumeraciones

| Enum | Valores | Notas |
|---|---|---|
| `Rol` | `Administrador`, `Cliente` | Claim `role` del JWT |
| `EstadoEscaneo` | `Pendiente`, `EnCurso`, `Terminado`, `Fallido` | Ver máquina de estados |
| `Severidad` | `Informativa` = 0, `Baja` = 1, `Media` = 2, `Alta` = 3 | Desde `riskcode` de ZAP; el orden numérico permite ordenar |
| `PrioridadIA` | `Baja` = 1, `Media` = 2, `Alta` = 3 | Rotulada siempre "Prioridad IA" en la UI |
| `EstadoAnalisisIA` | `Pendiente`, `Analizado` | "análisis IA pendiente" en la UI |
| `EstadoEnvio` | `Pendiente`, `Enviado`, `Fallido` | Estado del correo de una Alerta |
| `OrigenCategoriaOwasp` | `EtiquetaZap2025`, `EtiquetaZap2021`, `TablaCwe`, `PorDefecto` | Trazabilidad de la regla de mapeo (R3) |

## Usuario (`usuarios`)

| Campo | Tipo | Reglas |
|---|---|---|
| `id` | Guid | PK |
| `nombre` | texto(100) | obligatorio |
| `correo` | texto(254) | obligatorio, guardado en minúsculas, **único** |
| `hash_contrasena` | texto | PBKDF2 (`PasswordHasher`) |
| `rol` | Rol | obligatorio |
| `intentos_fallidos` | int | ≥ 0; se reinicia con un login exitoso |
| `bloqueado_hasta` | DateTimeOffset? | si es > ahora, el login se rechaza (FR-005) |
| `creado_en` | DateTimeOffset | |

**Regla de bloqueo**: un login fallido incrementa `intentos_fallidos`. Al llegar a 5, `bloqueado_hasta = ahora + 15 min` e `intentos_fallidos = 0`. Mientras haya bloqueo, el mensaje sigue siendo genérico.

## Activo (`activos`)

| Campo | Tipo | Reglas |
|---|---|---|
| `id` | Guid | PK |
| `nombre` | texto(100) | obligatorio, 1–100 caracteres sin espacios sobrantes |
| `url` | texto(2048) | obligatoria; `http`/`https` absoluta; **normalizada** (esquema y host en minúsculas, sin puerto por defecto, sin fragmento, sin barra final salvo la raíz) |
| `propietario_id` | Guid | FK → `usuarios` (rol Cliente) |
| `creado_en` | DateTimeOffset | |

- **Único**: (`propietario_id`, `url`) → FR-010 (409 si se repite).
- **Validación de lista blanca** (FR-009): el **origen** de la URL (esquema + host + puerto) DEBE coincidir exactamente con el origen de un objetivo configurado. Se comprueba al registrar y otra vez al ejecutar el escaneo (FR-013).
- Relación: Usuario 1 — N Activo.

## Escaneo (`escaneos`)

| Campo | Tipo | Reglas |
|---|---|---|
| `id` | Guid | PK |
| `activo_id` | Guid | FK → `activos` |
| `solicitado_por_id` | Guid | FK → `usuarios` (Cliente dueño o Administrador) |
| `estado` | EstadoEscaneo | inicial `Pendiente` |
| `solicitado_en` | DateTimeOffset | |
| `iniciado_en` | DateTimeOffset? | se fija al pasar a `EnCurso` |
| `finalizado_en` | DateTimeOffset? | se fija en `Terminado` o `Fallido` |
| `motivo_fallo` | texto(500)? | obligatorio si `Fallido` (p. ej., "Tiempo excedido", "Interrumpido", "Objetivo no disponible", "Objetivo ya no autorizado") |
| `total_hallazgos` | int | calculado al terminar |

- **Índice único parcial**: `activo_id` WHERE `estado IN ('Pendiente','EnCurso')` → un solo escaneo activo por activo (FR-015).
- Índice: (`estado`, `solicitado_en`) para la cola.
- Relación: Activo 1 — N Escaneo.

### Máquina de estados

```text
            ┌──────────── (Worker toma el trabajo) ───────────┐
 [crear] → Pendiente ──────────────────────────────────────→ EnCurso
              │                                                │
              │ objetivo ya no autorizado                      ├── ZAP termina + normalización OK ──→ Terminado
              └────────────────────────────→ Fallido ←─────────┤
                                                               ├── error ZAP / objetivo caído ──────→ Fallido
                                                               ├── supera 15 min (FR-016) ──────────→ Fallido
                                                               └── reinicio del Worker ─────────────→ Fallido ("Interrumpido")
```

`Terminado` y `Fallido` son estados finales: no hay más transiciones. La falla de la IA o del correo **no** lleva a `Fallido`.

## Hallazgo (`hallazgos`)

| Campo | Tipo | Reglas |
|---|---|---|
| `id` | Guid | PK |
| `escaneo_id` | Guid | FK → `escaneos` |
| `nombre` | texto(300) | obligatorio (nombre de la alerta ZAP) |
| `descripcion` | texto | obligatorio |
| `severidad` | Severidad | **obligatorio** (principio III) |
| `categoria_owasp` | texto(60) | **obligatorio**, formato `A0X:2025 - <Nombre>` (principio III) |
| `origen_categoria` | OrigenCategoriaOwasp | trazabilidad |
| `url_afectada` | texto(2048) | obligatoria |
| `recomendacion` | texto | obligatoria (`solution` de ZAP; si viene vacía se usa la de la tabla de recomendaciones por categoría) |
| `metodo_http` | texto(10)? | |
| `parametro` | texto(300)? | |
| `evidencia` | texto(2000)? | truncada |
| `cwe_id` | int? | |
| `plugin_id` | int | id de la regla ZAP |
| `confianza_escaner` | int | 1–3 (Baja, Media, Alta); las alertas con 0 se descartan |
| `ocurrencias` | int | ≥ 1 (agrupación por plugin + URL + parámetro) |
| `estado_ia` | EstadoAnalisisIA | inicial `Pendiente` |
| `prioridad_ia` | PrioridadIA? | requerido si `Analizado` |
| `probabilidad_falso_positivo` | decimal(5,4)? | en [0, 1]; requerido si `Analizado` |
| `es_posible_falso_positivo` | bool (calculado) | `probabilidad_falso_positivo >= 0.5` (FR-023) |
| `justificacion_ia` | texto(500)? | requerido si `Analizado` |
| `version_modelo_ia` | texto(40)? | |
| `analizado_en` | DateTimeOffset? | |
| `intentos_ia` | int | contador de reintentos |

- La **severidad nunca se modifica** después de crear el hallazgo (FR-026).
- Un `Hallazgo` sin severidad o sin categoría no se puede construir: el constructor del dominio lo rechaza (principio III).
- Índices: (`escaneo_id`, `severidad` desc); (`estado_ia`) para el reintentador.
- Relación: Escaneo 1 — N Hallazgo.

## Alerta (`alertas`)

| Campo | Tipo | Reglas |
|---|---|---|
| `id` | Guid | PK |
| `escaneo_id` | Guid | FK → `escaneos`, **único** (una alerta por escaneo) |
| `destinatario_id` | Guid | FK → `usuarios`: **siempre el propietario del activo**, aunque el escaneo lo haya lanzado el Administrador |
| `correo_destino` | texto(254) | copia del correo al momento de crearla |
| `creada_en` | DateTimeOffset | |
| `total_altas` | int | ≥ 1 |
| `total_posibles_fp` | int | hallazgos Altos marcados como posible falso positivo |
| `resumen` | texto(1000) | texto corto que se muestra en la plataforma y en el dashboard |
| `estado_envio` | EstadoEnvio | inicial `Pendiente` |
| `intentos_envio` | int | 0–3 |
| `proximo_intento_en` | DateTimeOffset? | backoff |
| `ultimo_error` | texto(500)? | |
| `enviada_en` | DateTimeOffset? | |

- Se crea solo si el escaneo termina con ≥ 1 hallazgo `Alta` (FR-027).
- Estados: `Pendiente` →(envío OK)→ `Enviado`. `Pendiente` →(3 fallos)→ `Fallido`. Cada fallo intermedio deja `Pendiente`, incrementa `intentos_envio` y fija `proximo_intento_en`.

## RegistroAuditoria (`registros_auditoria`)

| Campo | Tipo | Reglas |
|---|---|---|
| `id` | Guid | PK |
| `fecha` | DateTimeOffset | |
| `usuario_id` | Guid? | nulo si el login falla con un correo inexistente |
| `correo_intentado` | texto(254)? | solo en eventos de login |
| `accion` | texto(60) | `LoginExitoso`, `LoginFallido`, `CuentaBloqueada`, `ActivoRegistrado`, `EscaneoSolicitado`, `EscaneoSolicitadoPorAdmin`, `ResultadoEscaneoConsultado`, `AccesoDenegado` |
| `recurso` | texto(200)? | p. ej. `escaneos/{id}` |
| `resultado` | texto(20) | `Exito`, `Fallo`, `Denegado` |
| `ip_origen` | texto(45)? | |

Solo se inserta, nunca se modifica (principio I). Sin datos personales reales (principio II).

## Diagrama de relaciones

```text
Usuario 1──N Activo 1──N Escaneo 1──N Hallazgo
   │                       │
   │                       └──0..1 Alerta N──1 Usuario (destinatario = propietario)
   └──N Escaneo (solicitado_por)
Usuario 0..1──N RegistroAuditoria
```

## Configuración (no persistida en BD)

`ObjetivoPermitido` (desde variables de entorno, sección `Egida:Escaneo:Objetivos`):

| Campo | Ejemplo |
|---|---|
| `Url` | `http://juice-shop:3000` |
| `Nombre` | `OWASP Juice Shop (pruebas)` |
| `UrlsSemilla` | `/rest/products/search?q=manzana`, `/ftp`, `/api/Products` |
