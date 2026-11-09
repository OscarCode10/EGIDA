FROM python:3.12-slim
COPY --from=ghcr.io/astral-sh/uv:0.9 /uv /usr/local/bin/uv
ENV UV_COMPILE_BYTECODE=1 UV_LINK_MODE=copy PYTHONUNBUFFERED=1 UV_PYTHON_DOWNLOADS=never
WORKDIR /app
COPY pyproject.toml uv.lock ./
RUN uv sync --frozen --no-dev
COPY app/ app/
COPY scripts/ scripts/
COPY datos/ datos/
# El modelo se entrena en el build (reproducible: dataset versionado y semillas fijas).
RUN uv run --no-dev python scripts/entrenar.py
RUN useradd --create-home --uid 10001 egida && chown -R egida /app
USER egida
EXPOSE 8000
CMD ["uv", "run", "--no-dev", "uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
