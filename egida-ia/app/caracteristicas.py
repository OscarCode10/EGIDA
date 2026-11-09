"""Extracción de variables de entrada a partir de un hallazgo normalizado (research R8)."""

from __future__ import annotations

import re
from typing import Any

SEVERIDADES = {"Informativa": 0, "Baja": 1, "Media": 2, "Alta": 3}
CATEGORIAS_CRITICAS = {"A01", "A05", "A07"}  # control de acceso, inyección, autenticación
PATRON_ERROR = re.compile(r"(sql|syntax|sqlite|odbc|exception|stack|error|500|<script|alert\()", re.IGNORECASE)
PATRON_ESTATICA = re.compile(r"\.(js|css|png|jpe?g|gif|svg|ico|woff2?|ttf|map)(\?|$)", re.IGNORECASE)
PATRON_CATEGORIA = re.compile(r"^A(\d{2})")

# Reglas activas de ZAP (las demás son pasivas): inyección, XSS, traversal, etc.
REGLAS_ACTIVAS_MIN = 0


def es_regla_activa(plugin_id: int) -> bool:
    return plugin_id < 10000 or 20000 <= plugin_id < 50000 or 90017 <= plugin_id <= 90037


COLUMNAS = [
    "riesgo",
    "confianza",
    "regla_activa",
    "categoria",
    "categoria_critica",
    "tiene_cwe",
    "metodo_post",
    "tiene_parametro",
    "tiene_evidencia",
    "evidencia_error",
    "longitud_evidencia",
    "contenido_html",
    "url_estatica",
    "ocurrencias",
]


def extraer(h: dict[str, Any]) -> dict[str, float]:
    """Convierte un hallazgo (dict con las claves del contrato) en el vector de variables."""
    evidencia = h.get("evidencia") or ""
    categoria = h.get("categoria_owasp") or ""
    m = PATRON_CATEGORIA.match(categoria)
    codigo = int(m.group(1)) if m else 2
    tipo = (h.get("tipo_contenido") or "").lower()
    url = h.get("url") or ""
    return {
        "riesgo": float(SEVERIDADES.get(h.get("severidad", "Baja"), 1)),
        "confianza": float(h.get("confianza_escaner", 2)),
        "regla_activa": float(es_regla_activa(int(h.get("plugin_id", 0)))),
        "categoria": float(codigo),
        "categoria_critica": float(f"A{codigo:02d}" in CATEGORIAS_CRITICAS),
        "tiene_cwe": float(bool(h.get("cwe_id"))),
        "metodo_post": float((h.get("metodo_http") or "").upper() == "POST"),
        "tiene_parametro": float(bool(h.get("parametro"))),
        "tiene_evidencia": float(bool(evidencia.strip())),
        "evidencia_error": float(bool(PATRON_ERROR.search(evidencia))),
        "longitud_evidencia": float(min(len(evidencia), 2000)),
        "contenido_html": float("html" in tipo),
        "url_estatica": float(bool(PATRON_ESTATICA.search(url))),
        "ocurrencias": float(min(int(h.get("ocurrencias") or 1), 50)),
    }
