"""Dos RandomForest (research R8): (a) probabilidad de falso positivo, (b) Prioridad IA."""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

import joblib
import pandas as pd
from sklearn.ensemble import RandomForestClassifier

from app.caracteristicas import COLUMNAS, extraer

RAIZ = Path(__file__).resolve().parents[1]
DATASET = RAIZ / "datos" / "hallazgos_sinteticos.csv"
RUTA_MODELO = RAIZ / "modelos" / "modelo.joblib"
VERSION_MODELO = "rf-2026.11.1"
SEMILLA = 42


@dataclass
class ModeloEgida:
    falso_positivo: RandomForestClassifier
    prioridad: RandomForestClassifier
    version: str = VERSION_MODELO

    def importancias(self) -> dict[str, float]:
        """Peso de cada variable (promedio de ambos modelos); se usa para la justificación."""
        a = self.falso_positivo.feature_importances_
        b = self.prioridad.feature_importances_
        return {c: float((x + y) / 2) for c, x, y in zip(COLUMNAS, a, b)}

    def predecir(self, hallazgos: list[dict]) -> list[tuple[str, float, dict[str, float]]]:
        """Devuelve (prioridad, probabilidad_fp, variables) por hallazgo, en el mismo orden."""
        if not hallazgos:
            return []
        variables = [extraer(h) for h in hallazgos]
        x = pd.DataFrame(variables, columns=COLUMNAS)
        clases_fp = list(self.falso_positivo.classes_)
        indice_fp = clases_fp.index(1) if 1 in clases_fp else None
        prob_fp = self.falso_positivo.predict_proba(x)
        prioridades = self.prioridad.predict(x)
        resultado = []
        for i, v in enumerate(variables):
            p = float(prob_fp[i][indice_fp]) if indice_fp is not None else 0.0
            resultado.append((str(prioridades[i]), round(p, 4), v))
        return resultado


def entrenar(ruta_dataset: Path = DATASET) -> ModeloEgida:
    datos = pd.read_csv(ruta_dataset)
    x = datos[COLUMNAS]
    fp = RandomForestClassifier(n_estimators=150, max_depth=8, min_samples_leaf=3, random_state=SEMILLA)
    fp.fit(x, datos["es_falso_positivo"])
    pr = RandomForestClassifier(n_estimators=150, max_depth=8, min_samples_leaf=3, random_state=SEMILLA)
    pr.fit(x, datos["prioridad"])
    return ModeloEgida(fp, pr)


def guardar(modelo: ModeloEgida, ruta: Path = RUTA_MODELO) -> None:
    ruta.parent.mkdir(parents=True, exist_ok=True)
    joblib.dump(modelo, ruta)


def cargar_o_entrenar(ruta: Path = RUTA_MODELO) -> ModeloEgida:
    if ruta.exists():
        return joblib.load(ruta)
    modelo = entrenar()
    guardar(modelo, ruta)
    return modelo
