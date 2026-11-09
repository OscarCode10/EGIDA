"""Pruebas del contrato de egida-ia (camino feliz, validación 422, determinismo, salud)."""

from __future__ import annotations

import re

import pytest
from fastapi.testclient import TestClient

from app import main


def hallazgo(i: str = "h1", **cambios) -> dict:
    base = {
        "id": i,
        "nombre": "SQL Injection",
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
        "ocurrencias": 1,
    }
    base.update(cambios)
    return base


@pytest.fixture(scope="module")
def cliente():
    with TestClient(main.app) as c:
        yield c


def test_analizar_camino_feliz(cliente):
    lote = [hallazgo("a"), hallazgo("b", severidad="Baja", nombre="Timestamp Disclosure", plugin_id=10096,
                                      cwe_id=497, categoria_owasp="A01:2025 - Broken Access Control",
                                      parametro=None, evidencia="", url="http://juice-shop:3000/main.js")]
    r = cliente.post("/analizar", json={"escaneo_id": "e1", "hallazgos": lote})
    assert r.status_code == 200
    cuerpo = r.json()
    assert cuerpo["escaneo_id"] == "e1"
    assert cuerpo["version_modelo"]
    assert [x["id"] for x in cuerpo["resultados"]] == ["a", "b"]
    for x in cuerpo["resultados"]:
        assert x["prioridad"] in {"Alta", "Media", "Baja"}
        assert 0 <= x["probabilidad_falso_positivo"] <= 1
        assert round(x["probabilidad_falso_positivo"], 4) == x["probabilidad_falso_positivo"]
        assert 1 <= len(x["justificacion"]) <= 500
        assert re.search(r"[áéíóúñ]|confianza|riesgo", x["justificacion"], re.IGNORECASE)


def test_inyeccion_con_evidencia_es_prioridad_alta(cliente):
    r = cliente.post("/analizar", json={"escaneo_id": "e1", "hallazgos": [hallazgo(confianza_escaner=3)]})
    assert r.json()["resultados"][0]["prioridad"] == "Alta"


def test_lote_vacio(cliente):
    r = cliente.post("/analizar", json={"escaneo_id": "e1", "hallazgos": []})
    assert r.status_code == 200
    assert r.json()["resultados"] == []


@pytest.mark.parametrize("cambio", [
    {"severidad": "Critica"},
    {"confianza_escaner": 0},
    {"confianza_escaner": 4},
    {"plugin_id": -1},
    {"nombre": ""},
])
def test_validacion_422(cliente, cambio):
    r = cliente.post("/analizar", json={"escaneo_id": "e1", "hallazgos": [hallazgo(**cambio)]})
    assert r.status_code == 422


def test_campo_faltante_422(cliente):
    h = hallazgo()
    del h["categoria_owasp"]
    assert cliente.post("/analizar", json={"escaneo_id": "e1", "hallazgos": [h]}).status_code == 422


def test_ids_duplicados_422(cliente):
    r = cliente.post("/analizar", json={"escaneo_id": "e1", "hallazgos": [hallazgo("x"), hallazgo("x")]})
    assert r.status_code == 422


def test_lote_mayor_a_500_422(cliente):
    lote = [hallazgo(str(i)) for i in range(501)]
    assert cliente.post("/analizar", json={"escaneo_id": "e1", "hallazgos": lote}).status_code == 422


def test_evidencia_se_trunca(cliente):
    r = cliente.post("/analizar", json={"escaneo_id": "e1", "hallazgos": [hallazgo(evidencia="x" * 5000)]})
    assert r.status_code == 200


def test_determinismo(cliente):
    cuerpo = {"escaneo_id": "e1", "hallazgos": [hallazgo("a"), hallazgo("b", confianza_escaner=1, evidencia="")]}
    assert cliente.post("/analizar", json=cuerpo).json() == cliente.post("/analizar", json=cuerpo).json()


def test_salud_ok(cliente):
    r = cliente.get("/salud")
    assert r.status_code == 200
    assert r.json()["estado"] == "ok"
    assert r.json()["version_modelo"]


def test_salud_503_sin_modelo(cliente):
    anterior = main.estado["modelo"]
    main.estado["modelo"] = None
    try:
        assert cliente.get("/salud").status_code == 503
        assert cliente.post("/analizar", json={"escaneo_id": "e1", "hallazgos": [hallazgo()]}).status_code == 503
    finally:
        main.estado["modelo"] = anterior
