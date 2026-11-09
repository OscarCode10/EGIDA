"""Genera el dataset SINTÉTICO de hallazgos etiquetados (research R8, principio II).

Reglas expertas + ruido controlado, con semilla fija para que sea reproducible.
No contiene datos personales ni hallazgos reales.

Uso: uv run python scripts/generar_dataset.py
"""

from __future__ import annotations

import csv
import math
import random
import sys
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(RAIZ))

from app.caracteristicas import COLUMNAS, extraer  # noqa: E402

SEMILLA = 20261108
FILAS = 400
SALIDA = RAIZ / "datos" / "hallazgos_sinteticos.csv"

PLANTILLAS = [
    # (nombre, plugin, cwe, categoria, severidades posibles, regla_activa)
    ("SQL Injection", 40018, 89, "A05:2025 - Injection", ["Alta"]),
    ("Cross Site Scripting (Reflected)", 40012, 79, "A05:2025 - Injection", ["Alta"]),
    ("Cross Site Scripting (Persistent)", 40014, 79, "A05:2025 - Injection", ["Alta"]),
    ("Path Traversal", 6, 22, "A01:2025 - Broken Access Control", ["Alta"]),
    ("Remote OS Command Injection", 90020, 78, "A05:2025 - Injection", ["Alta"]),
    ("Directory Browsing", 0, 548, "A01:2025 - Broken Access Control", ["Media"]),
    ("Content Security Policy (CSP) Header Not Set", 10038, 693, "A02:2025 - Security Misconfiguration", ["Media"]),
    ("Missing Anti-clickjacking Header", 10020, 1021, "A02:2025 - Security Misconfiguration", ["Media"]),
    ("Cross-Domain Misconfiguration", 10098, 264, "A02:2025 - Security Misconfiguration", ["Media"]),
    ("Session ID in URL Rewrite", 3, 598, "A07:2025 - Authentication Failures", ["Media"]),
    ("Cross-Domain JavaScript Source File Inclusion", 10017, 829, "A03:2025 - Software Supply Chain Failures", ["Baja"]),
    ("Timestamp Disclosure - Unix", 10096, 497, "A01:2025 - Broken Access Control", ["Baja"]),
    ("X-Content-Type-Options Header Missing", 10021, 693, "A02:2025 - Security Misconfiguration", ["Baja"]),
    ("Private IP Disclosure", 2, 497, "A01:2025 - Broken Access Control", ["Baja"]),
    ("Information Disclosure - Suspicious Comments", 10027, 615, "A02:2025 - Security Misconfiguration", ["Informativa"]),
    ("Modern Web Application", 10109, None, "A02:2025 - Security Misconfiguration", ["Informativa"]),
    ("Retrieved from Cache", 10050, None, "A02:2025 - Security Misconfiguration", ["Informativa"]),
]

EVIDENCIAS_ERROR = ["SQLITE_ERROR: near \"'\": syntax error", "HTTP/1.1 500 Internal Server Error",
                    "<script>alert(1);</script>", "root:x:0:0:root:/root:/bin/bash", "SequelizeDatabaseError"]
EVIDENCIAS_NEUTRAS = ["", "", "1650000000", "<!-- TODO: revisar -->", "Access-Control-Allow-Origin: *", "x-powered-by"]
RUTAS = ["/rest/products/search?q=manzana", "/api/Products", "/ftp", "/main.js", "/styles.css", "/assets/logo.png",
         "/rest/user/login", "/api/Feedbacks", "/", "/socket.io/?EIO=4"]


def sigmoide(x: float) -> float:
    return 1 / (1 + math.exp(-x))


def generar(rng: random.Random) -> list[dict]:
    filas = []
    for i in range(FILAS):
        nombre, plugin, cwe, categoria, sevs = rng.choice(PLANTILLAS)
        severidad = rng.choice(sevs)
        ruta = rng.choice(RUTAS)
        activa = plugin < 10000 or 20000 <= plugin < 50000 or 90017 <= plugin <= 90037
        if activa:
            evidencia = rng.choice(EVIDENCIAS_ERROR + [""] * 2)
        else:
            evidencia = rng.choice(EVIDENCIAS_NEUTRAS)
        hallazgo = {
            "id": f"sint-{i}",
            "nombre": nombre,
            "plugin_id": plugin,
            "severidad": severidad,
            "confianza_escaner": rng.choices([1, 2, 3], weights=[3, 5, 2])[0],
            "cwe_id": cwe,
            "categoria_owasp": categoria,
            "url": "http://objetivo.test" + ruta,
            "metodo_http": rng.choice(["GET", "GET", "GET", "POST"]),
            "parametro": rng.choice(["q", "id", "email", None, None]) if activa else rng.choice([None, None, "Origin"]),
            "evidencia": evidencia,
            "tipo_contenido": rng.choice(["text/html", "application/json", "application/javascript", "text/css"]),
            "ocurrencias": rng.choice([1, 1, 1, 2, 3, 8]),
        }
        x = extraer(hallazgo)

        # Regla experta de falso positivo: confianza baja, sin evidencia, recursos estáticos → más probable.
        z_fp = (-1.2 - 0.9 * (x["confianza"] - 2) - 1.6 * x["evidencia_error"] + 1.1 * (1 - x["tiene_evidencia"])
                + 1.3 * x["url_estatica"] - 0.4 * x["tiene_parametro"] + rng.gauss(0, 0.6))
        es_fp = int(rng.random() < sigmoide(z_fp))

        # Regla experta de prioridad: riesgo, categoría crítica, evidencia y confianza; baja si es FP.
        puntaje = (1.2 * x["riesgo"] + 0.8 * x["categoria_critica"] + 0.7 * x["evidencia_error"]
                   + 0.3 * (x["confianza"] - 2) + 0.2 * x["tiene_parametro"] - 1.5 * es_fp + rng.gauss(0, 0.45))
        prioridad = "Alta" if puntaje >= 3.3 else "Media" if puntaje >= 1.6 else "Baja"

        filas.append({**x, "es_falso_positivo": es_fp, "prioridad": prioridad})
    return filas


def main() -> None:
    rng = random.Random(SEMILLA)
    filas = generar(rng)
    SALIDA.parent.mkdir(parents=True, exist_ok=True)
    with SALIDA.open("w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=[*COLUMNAS, "es_falso_positivo", "prioridad"])
        w.writeheader()
        w.writerows(filas)
    print(f"Dataset sintético: {len(filas)} filas en {SALIDA}")


if __name__ == "__main__":
    main()
