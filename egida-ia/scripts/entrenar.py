"""Entrena los dos modelos con el dataset sintético y los guarda con joblib.

Uso: uv run python scripts/entrenar.py
"""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from app.modelo import DATASET, RUTA_MODELO, entrenar, guardar  # noqa: E402

if __name__ == "__main__":
    modelo = entrenar(DATASET)
    guardar(modelo, RUTA_MODELO)
    print(f"Modelo {modelo.version} guardado en {RUTA_MODELO}")
