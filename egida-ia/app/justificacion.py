"""Justificación breve en español a partir de las variables que más pesan en el modelo (R8)."""

from __future__ import annotations

MAX = 500


def _frases(v: dict[str, float]) -> dict[str, str | None]:
    confianza = {1: "baja", 2: "media", 3: "alta"}.get(int(v["confianza"]), "media")
    return {
        "riesgo": {3: "riesgo Alto según el escáner", 2: "riesgo Medio según el escáner",
                   1: "riesgo Bajo según el escáner", 0: "hallazgo informativo"}.get(int(v["riesgo"])),
        "confianza": f"confianza {confianza} del escáner",
        "evidencia_error": "evidencia de error o ejecución en la respuesta" if v["evidencia_error"] else None,
        "tiene_evidencia": None if v["tiene_evidencia"] else "sin evidencia en la respuesta",
        "categoria_critica": "categoría OWASP crítica (inyección, acceso o autenticación)" if v["categoria_critica"] else None,
        "url_estatica": "afecta un recurso estático" if v["url_estatica"] else None,
        "tiene_parametro": "parámetro de entrada manipulable" if v["tiene_parametro"] else None,
        "regla_activa": "confirmado con prueba activa" if v["regla_activa"] else "detectado por análisis pasivo",
    }


def construir(prioridad: str, probabilidad_fp: float, variables: dict[str, float],
              importancias: dict[str, float]) -> str:
    frases = _frases(variables)
    orden = sorted((k for k, f in frases.items() if f), key=lambda k: importancias.get(k, 0), reverse=True)
    motivos = [frases[k] for k in orden[:3]]
    texto = ", ".join(motivos)
    texto = texto[0].upper() + texto[1:] if texto else "Sin variables relevantes"

    if probabilidad_fp >= 0.5:
        conclusion = f"probable falso positivo ({probabilidad_fp:.0%}); verificar manualmente"
    elif prioridad == "Alta":
        conclusion = "atender primero"
    elif prioridad == "Media":
        conclusion = "planificar su corrección"
    else:
        conclusion = "baja urgencia"
    return f"{texto}: {conclusion}."[:MAX]
