"""Esquemas del contrato POST /analizar (specs/001-prototipo-demostrable/contracts/ia-analizar.md)."""

from __future__ import annotations

from typing import Literal

from pydantic import BaseModel, Field, field_validator, model_validator

Severidad = Literal["Alta", "Media", "Baja", "Informativa"]
Prioridad = Literal["Alta", "Media", "Baja"]


class HallazgoEntrada(BaseModel):
    id: str = Field(min_length=1)
    nombre: str = Field(min_length=1, max_length=300)
    plugin_id: int = Field(ge=0)
    severidad: Severidad
    confianza_escaner: int = Field(ge=1, le=3)
    cwe_id: int | None = None
    categoria_owasp: str = Field(min_length=1)
    url: str = Field(min_length=1)
    metodo_http: str | None = None
    parametro: str | None = None
    evidencia: str | None = None
    tipo_contenido: str | None = None
    ocurrencias: int = Field(default=1, ge=1)

    @field_validator("evidencia")
    @classmethod
    def truncar_evidencia(cls, v: str | None) -> str | None:
        return v[:2000] if v else v


class SolicitudAnalizar(BaseModel):
    escaneo_id: str = Field(min_length=1)
    hallazgos: list[HallazgoEntrada] = Field(max_length=500)

    @model_validator(mode="after")
    def ids_unicos(self) -> "SolicitudAnalizar":
        ids = [h.id for h in self.hallazgos]
        if len(ids) != len(set(ids)):
            raise ValueError("Los id de los hallazgos deben ser únicos dentro del lote.")
        return self


class ResultadoHallazgo(BaseModel):
    id: str
    prioridad: Prioridad
    probabilidad_falso_positivo: float = Field(ge=0, le=1)
    justificacion: str = Field(min_length=1, max_length=500)


class RespuestaAnalizar(BaseModel):
    escaneo_id: str
    version_modelo: str
    resultados: list[ResultadoHallazgo]
