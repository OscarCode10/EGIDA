"""Servicio egida-ia: POST /analizar y GET /salud. Solo accesible en la red interna (principio V)."""

from __future__ import annotations

import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI, HTTPException
from fastapi.responses import JSONResponse

from app import justificacion
from app.esquemas import RespuestaAnalizar, ResultadoHallazgo, SolicitudAnalizar
from app.modelo import ModeloEgida, cargar_o_entrenar

log = logging.getLogger("egida-ia")
estado: dict[str, ModeloEgida | None] = {"modelo": None}


@asynccontextmanager
async def ciclo_vida(_: FastAPI):
    try:
        estado["modelo"] = cargar_o_entrenar()
        log.info("Modelo %s cargado", estado["modelo"].version)
    except Exception:  # pragma: no cover - el servicio responde 503 si no hay modelo
        log.exception("No se pudo cargar el modelo")
    yield


app = FastAPI(title="ÉGIDA IA", version="0.1.0", lifespan=ciclo_vida)


def _modelo() -> ModeloEgida:
    modelo = estado["modelo"]
    if modelo is None:
        raise HTTPException(status_code=503, detail="Modelo no cargado")
    return modelo


@app.get("/salud")
def salud():
    modelo = estado["modelo"]
    if modelo is None:
        return JSONResponse(status_code=503, content={"estado": "sin_modelo"})
    return {"estado": "ok", "version_modelo": modelo.version}


@app.post("/analizar", response_model=RespuestaAnalizar)
def analizar(solicitud: SolicitudAnalizar) -> RespuestaAnalizar:
    modelo = _modelo()
    entradas = [h.model_dump() for h in solicitud.hallazgos]
    importancias = modelo.importancias()
    resultados = [
        ResultadoHallazgo(
            id=h["id"],
            prioridad=prioridad,
            probabilidad_falso_positivo=prob,
            justificacion=justificacion.construir(prioridad, prob, variables, importancias),
        )
        for h, (prioridad, prob, variables) in zip(entradas, modelo.predecir(entradas))
    ]
    return RespuestaAnalizar(escaneo_id=solicitud.escaneo_id, version_modelo=modelo.version, resultados=resultados)
