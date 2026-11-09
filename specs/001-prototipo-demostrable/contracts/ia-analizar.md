# Contrato: servicio de IA (`egida-ia`)

Comunicación solo por REST (principio V). Solo es accesible en la red interna `red-app`, sin puerto publicado al host en el perfil de demo. Formato JSON en `snake_case`.

## `POST /analizar`

Recibe un lote de hallazgos normalizados (máx. 500) y devuelve un resultado por hallazgo.

### Solicitud

```json
{
  "escaneo_id": "0193…",
  "hallazgos": [
    {
      "id": "0193…",
      "nombre": "SQL Injection - SQLite",
      "plugin_id": 40018,
      "severidad": "Alta",
      "confianza_escaner": 2,
      "cwe_id": 89,
      "categoria_owasp": "A05:2025 - Injection",
      "url": "http://juice-shop:3000/rest/products/search?q=manzana",
      "metodo_http": "GET",
      "parametro": "q",
      "evidencia": "SQLITE_ERROR",
      "tipo_contenido": "application/json",
      "ocurrencias": 1
    }
  ]
}
```

| Campo | Tipo | Obligatorio | Validación |
|---|---|---|---|
| `id` | string | sí | único dentro del lote |
| `nombre` | string | sí | 1–300 |
| `plugin_id` | int | sí | ≥ 0 |
| `severidad` | enum | sí | `Alta`, `Media`, `Baja`, `Informativa` |
| `confianza_escaner` | int | sí | 1–3 |
| `cwe_id` | int \| null | no | |
| `categoria_owasp` | string | sí | |
| `url` | string | sí | |
| `metodo_http` | string \| null | no | |
| `parametro` | string \| null | no | |
| `evidencia` | string \| null | no | se trunca a 2000 |
| `tipo_contenido` | string \| null | no | |
| `ocurrencias` | int | no (def. 1) | ≥ 1 |

### Respuesta 200

```json
{
  "escaneo_id": "0193…",
  "version_modelo": "rf-2026.11.1",
  "resultados": [
    {
      "id": "0193…",
      "prioridad": "Alta",
      "probabilidad_falso_positivo": 0.08,
      "justificacion": "Inyección con evidencia de error de base de datos y confianza media del escáner: atender primero."
    }
  ]
}
```

| Campo | Regla |
|---|---|
| `resultados` | exactamente un elemento por cada `id` de la solicitud |
| `prioridad` | `Alta` \| `Media` \| `Baja` |
| `probabilidad_falso_positivo` | float en [0, 1], 4 decimales |
| `justificacion` | español, 1–500 caracteres |

### Errores

- **422**: validación de FastAPI (campo faltante o enum inválido).
- **503**: modelo no cargado.

### Validaciones del consumidor (`Egida.Infrastructure.ClienteIA`)

Si la respuesta no cumple el contrato (ids que faltan o sobran, prioridad desconocida, probabilidad fuera de rango), el lote completo se trata como **no analizado**: `EstadoIA = Pendiente` (FR-024).

## `GET /salud`

- **200** `{ "estado": "ok", "version_modelo": "rf-2026.11.1" }`
- **503** si el modelo no está cargado.

## Determinismo

Con el mismo modelo, la misma entrada produce la misma salida (`random_state` fijo en el entrenamiento). Lo verifica pytest.
