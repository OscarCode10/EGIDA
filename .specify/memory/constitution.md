<!--
Informe de Impacto de Sincronización (Sync Impact Report)
- Cambio de versión: 1.0.0 → 1.0.1 (PATCH)
- Principios modificados: III. Clasificación Obligatoria de Vulnerabilidades — se elimina la
  severidad "Crítica"; quedan 4 niveles (Alta, Media, Baja, Informativa) alineados con OWASP ZAP.
  Se clasifica como PATCH porque aún no existen especificaciones, planes ni código que dependan
  de la lista anterior.
- Secciones añadidas: ninguna.
- Secciones eliminadas: ninguna.
- Plantillas dependientes: no se modifican en este comando (leen la constitución en tiempo de
  ejecución).
- TODOs diferidos: ninguno.
-->

# Constitución de ÉGIDA

ÉGIDA (Escaneo, Gestión Inteligente y Detección de Amenazas) es una plataforma web de monitoreo de
ciberseguridad con alertamiento por correo y análisis de vulnerabilidades asistido por IA,
desarrollada como trabajo de grado de la Universidad Manuela Beltrán (UMB).

## Core Principles

### I. Seguridad desde el Diseño

- Todo componente DEBE diseñarse y revisarse frente a OWASP Top 10 y seguir las prácticas del
  NIST SSDF (SP 800-218).
- Todo tráfico externo DEBE viajar sobre HTTPS/TLS; no se exponen endpoints en HTTP plano fuera
  del entorno local de desarrollo.
- La autenticación DEBE usar JWT con roles `Administrador` y `Cliente`; cada endpoint DEBE
  declarar explícitamente el rol requerido (denegar por defecto).
- Los accesos (inicios de sesión exitosos y fallidos, operaciones administrativas y acceso a
  resultados de escaneo) DEBEN registrarse en una bitácora de auditoría.

**Justificación**: una plataforma de ciberseguridad que no es segura carece de credibilidad.

### II. Protección de Datos (Ley 1581 de 2012)

- Las pruebas, semillas de base de datos, demostraciones y conjuntos de entrenamiento DEBEN usar
  exclusivamente datos sintéticos.
- NUNCA se almacena, versiona ni procesa un dato personal real en ningún entorno del proyecto.

**Justificación**: cumplimiento del régimen colombiano de protección de datos personales.

### III. Clasificación Obligatoria de Vulnerabilidades

- Toda vulnerabilidad registrada DEBE tener una severidad (Alta, Media, Baja, Informativa),
  alineada con los niveles de riesgo de OWASP ZAP, y una categoría OWASP Top 10 asociada.
- Un hallazgo sin severidad o sin mapeo OWASP es inválido y NO DEBE persistirse ni mostrarse
  como tal; si el origen no aporta mapeo, se asigna una categoría por regla documentada.

**Justificación**: la priorización y el alertamiento dependen de una taxonomía consistente.

### IV. Pruebas Obligatorias (NO NEGOCIABLE)

- El código .NET DEBE probarse con xUnit y el código Python con pytest.
- Ninguna funcionalidad se considera terminada sin pruebas automatizadas de sus casos
  principales (camino feliz y errores esperados).
- Las pruebas DEBEN ejecutarse en CI y bloquear la fusión si fallan.

**Justificación**: la tesis debe demostrar que el sistema funciona, no solo afirmarlo.

### V. Arquitectura Modular por Capas y Degradación Elegante de la IA

- El backend es un monolito modular con Clean Architecture: `Egida.Domain`,
  `Egida.Application`, `Egida.Infrastructure`, `Egida.Api` y `Egida.Worker`. Las dependencias
  DEBEN apuntar hacia el dominio; `Egida.Domain` no depende de ninguna otra capa.
- El servicio de IA (Python) DEBE comunicarse con el backend únicamente mediante API REST; no
  comparte base de datos ni código.
- Si el servicio de IA no está disponible o falla, el sistema DEBE seguir operando y conservar
  la severidad original del escáner, dejando constancia de que el análisis IA quedó pendiente.

**Justificación**: aislamiento de responsabilidades y resiliencia ante fallos de un componente
opcional.

### VI. API para lo Inmediato, Worker para lo Diferido

- `Egida.Api` DEBE atender solo operaciones de respuesta inmediata (consultas, comandos cortos,
  encolado de trabajos).
- Las tareas largas, periódicas o con reintentos (escaneos ZAP, envío de correos, análisis IA)
  DEBEN ejecutarse en `Egida.Worker`.
- Los avances y resultados se notifican al frontend en tiempo real mediante SignalR.

**Justificación**: mantener la API responsiva y los procesos largos tolerantes a fallos.

### VII. Idioma y Convenciones

- Código (identificadores de dominio), documentación, mensajes de usuario, registros y
  comentarios DEBEN escribirse en español. Se exceptúan palabras reservadas, nombres de
  librerías y convenciones técnicas impuestas por los frameworks.
- Los commits DEBEN seguir Conventional Commits (`feat:`, `fix:`, `docs:`, `test:`,
  `chore:`, `refactor:`, `ci:`), con descripción en español.

**Justificación**: coherencia con el contexto académico y trazabilidad del historial.

### VIII. Todo en Contenedores

- Todo componente (API, Worker, servicio IA, ZAP, PostgreSQL, frontend en desarrollo) DEBE
  ejecutarse en contenedores Docker.
- Existe un único `docker-compose.yml` para desarrollo que levanta el sistema completo.

**Justificación**: reproducibilidad del entorno para evaluadores y despliegue.

### IX. Despliegue Portable

- Despliegue objetivo: VM en Azure for Students con Docker Compose y Caddy (HTTPS), PostgreSQL
  administrado y frontend en Azure Static Web Apps.
- Ninguna decisión técnica PUEDE atar el proyecto a un proveedor específico: se usan protocolos
  y estándares abiertos (cadenas de conexión, SMTP, contenedores OCI) y no SDKs propietarios en
  el código de dominio o aplicación.

**Justificación**: el proyecto debe sobrevivir al vencimiento de créditos estudiantiles.

### X. Integración y Entrega Continuas

- Cada pull request DEBE ejecutar en GitHub Actions: compilación, pruebas (.NET y Python) y
  análisis de seguridad (SAST y revisión de dependencias).
- Un pull request con cualquiera de estas verificaciones fallida NO DEBE fusionarse.

**Justificación**: aplicar a nuestro propio código el rigor que la plataforma promueve.

### XI. Gestión de Secretos

- Los secretos (claves JWT, credenciales SMTP, cadenas de conexión, claves de API de ZAP) NUNCA
  se versionan.
- Se suministran mediante variables de entorno y GitHub Secrets; el repositorio solo contiene
  archivos de ejemplo (`.env.example`) con valores ficticios.

**Justificación**: una filtración de secretos invalida la postura de seguridad del sistema.

### XII. Escaneo Ético y Autorizado

- El escáner DEBE operar únicamente sobre activos del entorno de pruebas aislado o sobre activos
  cuya propiedad haya sido verificada por la plataforma.
- El sistema DEBE rechazar el lanzamiento de un escaneo sobre un objetivo no verificado.

**Justificación**: escanear sistemas ajenos sin autorización es ilegal (Ley 1273 de 2009) y
contrario a la ética profesional.

### XIII. Prototipo Demostrable y Simplicidad

- La fase actual es PROTOTIPO DEMOSTRABLE: se prioriza el flujo de punta a punta (registrar
  activo → escanear → clasificar → analizar con IA → alertar por correo → visualizar) sobre la
  completitud de cada módulo.
- Se DEBE elegir la solución más simple que respete la arquitectura; toda complejidad adicional
  requiere justificación escrita en el plan.
- Lo que quede fuera del alcance del prototipo se documenta como trabajo futuro y NO se
  implementa.

**Justificación**: entregar un sistema funcional y defendible dentro del calendario académico.

## Restricciones Tecnológicas y de Despliegue

Stack obligatorio; cualquier sustitución requiere enmienda constitucional:

- **Backend**: ASP.NET Core sobre .NET 10 (C#), Clean Architecture, EF Core con PostgreSQL.
- **Tiempo real**: SignalR.
- **Módulo de IA**: microservicio Python con FastAPI y scikit-learn.
- **Escaneo**: OWASP ZAP controlado vía su API.
- **Correo**: MailKit sobre SMTP estándar.
- **Frontend**: React + Vite + TypeScript.
- **Infraestructura**: Docker y Docker Compose; Caddy como proxy inverso con HTTPS automático.
- **CI/CD**: GitHub Actions.

## Flujo de Desarrollo y Puertas de Calidad

- Cada funcionalidad sigue el flujo Spec Kit: especificación → plan → tareas → implementación.
- El plan de cada funcionalidad DEBE incluir una verificación explícita frente a esta
  constitución ("Constitution Check") antes de implementar.
- Definición de Terminado: pruebas de casos principales en verde, CI en verde, sin secretos ni
  datos personales reales en el diff, hallazgos con severidad y mapeo OWASP, documentación en
  español actualizada.
- Todo cambio entra por pull request hacia `main`; no se hacen commits directos a `main` salvo
  la configuración inicial del repositorio.

## Governance

- Esta constitución prevalece sobre cualquier otra práctica, guía o preferencia del proyecto.
- **Enmiendas**: se proponen mediante pull request que modifique este archivo, con descripción
  del cambio, justificación y efecto sobre especificaciones o planes existentes.
- **Versionado** (SemVer):
  - MAJOR: eliminación o redefinición incompatible de un principio o del stack obligatorio.
  - MINOR: nuevo principio o sección, o ampliación material de una guía.
  - PATCH: aclaraciones, redacción o correcciones sin efecto semántico.
- **Cumplimiento**: toda revisión de pull request y todo plan DEBE verificar el cumplimiento de
  estos principios; las desviaciones DEBEN justificarse por escrito en la sección de
  complejidad del plan o rechazarse.

**Version**: 1.0.1 | **Ratified**: 2026-11-03 | **Last Amended**: 2026-11-08
