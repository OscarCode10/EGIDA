# Especificación de funcionalidad: Prototipo demostrable de ÉGIDA

**Rama de la funcionalidad**: `001-prototipo-demostrable`

**Creada**: 2026-11-08

**Estado**: Borrador

**Entrada**: Descripción del usuario: "Prototipo demostrable de ÉGIDA (Escaneo, Gestión Inteligente y Detección de Amenazas). Objetivo: demostrar en menos de 5 minutos el flujo completo: un cliente registra un sitio web, lo escanea, ve las vulnerabilidades priorizadas por inteligencia artificial y recibe una alerta por correo de las críticas. [...] Fuera de alcance: registro de usuarios, verificación de propiedad, escaneos programados, umbrales configurables, reportes exportables, métricas de infraestructura, despliegue en la nube."

## Clarificaciones

### Sesión 2026-11-08

- P: Si un escaneo termina con hallazgos de severidad Alta y la IA marca alguno como posible falso positivo, ¿se envía igual el correo? → R: Sí. Se alerta siempre por severidad Alta y en el correo los posibles falsos positivos aparecen marcados.
- P: ¿Cuántos usuarios semilla se crean para la demo? → R: Dos: un Administrador y un Cliente.
- P: ¿Qué escala usa la IA para la prioridad de cada hallazgo? → R: Etiquetas Alta / Media / Baja, mostradas siempre como "Prioridad IA" para no confundirlas con la severidad.
- P: ¿El administrador solo consulta o también opera sobre activos de clientes? → R: Consulta todo y además puede lanzar escaneos sobre activos de cualquier cliente; la alerta va al cliente dueño del activo.
- Nota de planificación (2026-11-08): FR-018 pasa de "solo pasivo" a "perfil demo acotado con pruebas activas limitadas". Un escaneo solo pasivo no produce hallazgos de severidad Alta en el sitio de pruebas, y sin ellos la historia 5 no se puede demostrar. Ver [research.md](./research.md) R1.

## Escenarios de usuario y pruebas *(obligatorio)*

### Historia de usuario 1 - Inicio de sesión con roles (Prioridad: P1)

Un usuario precargado (Administrador o Cliente) entra a la plataforma con su correo y contraseña. Según su rol, ve únicamente las opciones y datos que le corresponden. Sin sesión iniciada no se puede ver nada de la plataforma.

**Por qué esta prioridad**: todo el resto del flujo depende de saber quién es el usuario y qué puede ver. Además, una plataforma de ciberseguridad sin control de acceso pierde credibilidad.

**Prueba independiente**: iniciar sesión con cada uno de los dos usuarios semilla y comprobar que cada uno llega a su vista. Comprobar también que con credenciales erróneas no se entra.

**Escenarios de aceptación**:

1. **Dado** el usuario cliente precargado, **cuando** ingresa su correo y contraseña correctos, **entonces** entra a la plataforma y ve solo sus propios activos, escaneos y alertas.
2. **Dado** el usuario administrador precargado, **cuando** ingresa sus credenciales correctas, **entonces** entra a la plataforma y tiene acceso a la vista global.
3. **Dado** cualquier usuario, **cuando** ingresa una contraseña incorrecta, **entonces** ve un mensaje genérico ("Correo o contraseña incorrectos") que no revela si el correo existe, y el intento queda registrado.
4. **Dado** un visitante sin sesión, **cuando** intenta abrir cualquier página o dato de la plataforma, **entonces** es redirigido al inicio de sesión.
5. **Dado** un usuario con sesión iniciada, **cuando** cierra sesión, **entonces** ya no puede acceder a los datos sin volver a autenticarse.

---

### Historia de usuario 2 - Registrar un activo y escanearlo (Prioridad: P1)

El cliente registra un sitio web (nombre y URL) que quiere monitorear y lanza un escaneo sobre él. Mientras el escaneo avanza, el estado cambia solo (pendiente → en curso → terminado o fallido) y la pantalla sigue siendo usable.

**Por qué esta prioridad**: es el núcleo de la demostración. Sin activo y sin escaneo no hay hallazgos, ni IA, ni alertas.

**Prueba independiente**: registrar el sitio de pruebas, lanzar un escaneo y observar que el estado avanza hasta "terminado" sin recargar la página y sin que la interfaz se bloquee.

**Escenarios de aceptación**:

1. **Dado** un cliente con sesión iniciada, **cuando** registra un activo con nombre y una URL que pertenece al entorno de pruebas autorizado, **entonces** el activo aparece en su lista de activos.
2. **Dado** un cliente, **cuando** intenta registrar una URL que no pertenece al entorno de pruebas autorizado, **entonces** el sistema rechaza el registro y le explica que solo se permiten objetivos de prueba autorizados.
3. **Dado** un cliente, **cuando** ingresa una URL con formato inválido o deja el nombre vacío, **entonces** el sistema rechaza el registro e indica qué campo corregir.
4. **Dado** un activo registrado, **cuando** el cliente lanza un escaneo, **entonces** el escaneo aparece de inmediato con estado "pendiente" y la pantalla sigue respondiendo.
5. **Dado** un escaneo en marcha, **cuando** cambia de etapa, **entonces** el cliente ve el nuevo estado ("en curso", "terminado" o "fallido") sin recargar la página.
6. **Dado** un escaneo que no puede completarse (objetivo caído, error del escáner o tiempo máximo excedido), **cuando** falla, **entonces** queda en estado "fallido" con un motivo comprensible.
7. **Dado** un activo con un escaneo pendiente o en curso, **cuando** el cliente intenta lanzar otro escaneo sobre el mismo activo, **entonces** el sistema lo impide e indica que ya hay uno en marcha.

---

### Historia de usuario 3 - Ver hallazgos clasificados (Prioridad: P1)

Al terminar un escaneo, el cliente ve la lista de hallazgos. Cada uno muestra nombre, severidad (Alta, Media, Baja, Informativa), categoría OWASP Top 10, URL afectada y una recomendación para corregirlo.

**Por qué esta prioridad**: es el resultado que le da valor al escaneo y la base sobre la que trabajan la IA y las alertas.

**Prueba independiente**: abrir un escaneo terminado sobre el sitio de pruebas y verificar que cada hallazgo tiene los cinco campos completos.

**Escenarios de aceptación**:

1. **Dado** un escaneo terminado con hallazgos, **cuando** el cliente abre su detalle, **entonces** ve cada hallazgo con nombre, severidad, categoría OWASP Top 10, URL afectada y recomendación.
2. **Dado** un hallazgo para el que el escáner no indica categoría OWASP, **cuando** se registra, **entonces** se le asigna una categoría según una regla de mapeo documentada; ningún hallazgo queda sin severidad ni categoría.
3. **Dado** un escaneo terminado sin hallazgos, **cuando** el cliente abre su detalle, **entonces** ve un mensaje claro de que no se encontraron vulnerabilidades.
4. **Dado** la lista de hallazgos, **cuando** el cliente la consulta, **entonces** puede ordenarla o filtrarla por severidad.

---

### Historia de usuario 4 - Priorización por IA (Prioridad: P2)

Para cada hallazgo, el cliente ve la prioridad que asignó el módulo de IA, si la IA lo considera un posible falso positivo y una breve justificación. Si la IA no está disponible, los hallazgos siguen visibles con su severidad original y la marca "análisis IA pendiente".

**Por qué esta prioridad**: es el diferenciador de la tesis (análisis de vulnerabilidades con IA). Depende de que existan hallazgos (historia 3).

**Prueba independiente**: con el módulo de IA activo, verificar que los hallazgos muestran prioridad, indicador de falso positivo y justificación. Con el módulo de IA apagado, verificar que los hallazgos aparecen igual, con la marca de pendiente.

**Escenarios de aceptación**:

1. **Dado** un escaneo terminado y el módulo de IA disponible, **cuando** el cliente ve los hallazgos, **entonces** cada uno muestra la prioridad asignada por la IA, si es posible falso positivo y una justificación breve.
2. **Dado** el módulo de IA no disponible, **cuando** termina un escaneo, **entonces** los hallazgos se muestran con su severidad original y la indicación "análisis IA pendiente", y el escaneo igual se marca como terminado.
3. **Dado** hallazgos marcados como "análisis IA pendiente", **cuando** el módulo de IA vuelve a estar disponible, **entonces** el sistema reintenta el análisis automáticamente y actualiza esos hallazgos.
4. **Dado** la lista de hallazgos analizados, **cuando** el cliente la ordena por Prioridad IA, **entonces** aparecen primero los de Prioridad IA Alta, luego Media y luego Baja; dentro de cada grupo se ordenan por severidad.

---

### Historia de usuario 5 - Alerta por correo de severidad alta (Prioridad: P2)

Cuando un escaneo termina con al menos un hallazgo de severidad Alta, el cliente dueño del activo recibe un correo con el resumen (activo, fecha, número de hallazgos por severidad y los hallazgos altos) y un enlace al detalle del escaneo.

**Por qué esta prioridad**: cumple el "alertamiento mediante correo electrónico" del título de la tesis. Depende de que existan hallazgos clasificados.

**Prueba independiente**: escanear el sitio de pruebas (que tiene vulnerabilidades altas conocidas) y comprobar en la bandeja de correo de pruebas que llega la alerta con el resumen y un enlace funcional.

**Escenarios de aceptación**:

1. **Dado** un escaneo que termina con uno o más hallazgos de severidad Alta, **cuando** termina, **entonces** el cliente dueño del activo recibe un único correo de alerta con el resumen y el enlace al detalle.
2. **Dado** un escaneo cuyos hallazgos de severidad Alta fueron todos marcados por la IA como posible falso positivo, **cuando** termina, **entonces** el correo se envía igual y cada uno de esos hallazgos aparece marcado como "posible falso positivo".
3. **Dado** un escaneo que termina sin hallazgos de severidad Alta, **cuando** termina, **entonces** no se envía correo de alerta.
4. **Dado** que el envío del correo falla, **cuando** ocurre el error, **entonces** el sistema reintenta el envío, la alerta queda registrada y visible en la plataforma aunque el correo no haya salido, y el escaneo no se marca como fallido por ese motivo.
5. **Dado** el enlace del correo, **cuando** el cliente lo abre, **entonces** llega al detalle del escaneo (iniciando sesión primero si no la tiene).

---

### Historia de usuario 6 - Dashboard en tiempo real (Prioridad: P3)

El usuario ve un panel con el total de activos, los escaneos recientes, los hallazgos por severidad y las últimas alertas. El panel se actualiza solo cuando termina un escaneo.

**Por qué esta prioridad**: hace visible el resultado de la demo de un vistazo, pero el flujo funciona sin él.

**Prueba independiente**: dejar el dashboard abierto, lanzar un escaneo desde otra pestaña y comprobar que los indicadores cambian solos al terminar.

**Escenarios de aceptación**:

1. **Dado** un cliente con sesión iniciada, **cuando** abre el dashboard, **entonces** ve su total de activos, sus escaneos recientes, sus hallazgos agrupados por severidad y sus últimas alertas.
2. **Dado** el dashboard abierto, **cuando** termina un escaneo del usuario, **entonces** los indicadores se actualizan sin recargar la página.
3. **Dado** un usuario sin activos ni escaneos, **cuando** abre el dashboard, **entonces** ve los indicadores en cero y una invitación a registrar su primer activo.

---

### Historia de usuario 7 - Vista global del administrador (Prioridad: P3)

El administrador ve los activos y escaneos de todos los clientes, con el cliente propietario identificado, y su dashboard resume los datos de toda la plataforma. También puede lanzar un escaneo sobre el activo de cualquier cliente.

**Por qué esta prioridad**: muestra la separación de roles en la demo. El valor principal está en el flujo del cliente.

**Prueba independiente**: después de que el cliente escanee un activo, iniciar sesión como administrador y verificar que ese activo y su escaneo aparecen con el cliente identificado. Luego relanzar un escaneo desde el administrador y verificar que la alerta llega al cliente dueño.

**Escenarios de aceptación**:

1. **Dado** el administrador con sesión iniciada, **cuando** abre la lista de activos o de escaneos, **entonces** ve los de todos los clientes con el propietario de cada uno.
2. **Dado** el administrador, **cuando** abre el detalle de un escaneo de cualquier cliente, **entonces** ve sus hallazgos y el análisis de IA.
3. **Dado** el administrador, **cuando** lanza un escaneo sobre el activo de un cliente, **entonces** el escaneo sigue el mismo flujo de estados, queda registrado como lanzado por el administrador en la bitácora de auditoría y, si hay hallazgos de severidad Alta, la alerta llega al cliente dueño del activo.
4. **Dado** un cliente, **cuando** intenta acceder directamente (por ejemplo, cambiando la dirección) a un activo o escaneo que no le pertenece, **entonces** el acceso es denegado. Como en la demo solo hay un cliente, esto se verifica con pruebas automatizadas que crean un segundo cliente de prueba.

---

### Casos límite

- **URL fuera de la lista autorizada**: se rechaza tanto al registrar el activo como al lanzar el escaneo. Así, si la lista cambia después del registro, tampoco se escanea un objetivo que ya no está autorizado.
- **URL que redirige fuera del entorno autorizado**: el escaneo no debe seguir la redirección hacia objetivos no autorizados.
- **Activo duplicado**: un mismo cliente no puede registrar dos veces la misma URL; se le indica que ya existe.
- **Escaneo demasiado largo**: si supera el tiempo máximo, se detiene y queda como "fallido (tiempo excedido)".
- **Reinicio del sistema con escaneos en curso**: al volver, los escaneos interrumpidos se marcan como "fallido" con motivo "interrumpido", en lugar de quedar en curso para siempre.
- **IA que falla a mitad de un lote**: los hallazgos ya analizados conservan su análisis y el resto queda "análisis IA pendiente".
- **IA que responde con datos incompletos o inválidos**: se trata igual que si no estuviera disponible para esos hallazgos.
- **Sesión expirada durante un escaneo**: el escaneo continúa; al volver a iniciar sesión el usuario ve el resultado.
- **Intentos repetidos de inicio de sesión fallidos**: después de 5 intentos fallidos seguidos se bloquea temporalmente el ingreso de esa cuenta (15 minutos) y queda registrado.
- **Muchos hallazgos de severidad Alta en un escaneo**: se envía un solo correo por escaneo, no uno por hallazgo.

## Requisitos *(obligatorio)*

### Requisitos funcionales

**Autenticación y roles**

- **FR-001**: El sistema DEBE permitir iniciar sesión con correo y contraseña a los dos usuarios precargados: uno con rol Administrador y otro con rol Cliente.
- **FR-002**: El sistema DEBE denegar por defecto el acceso a cualquier dato o acción que no esté explícitamente permitido para el rol del usuario.
- **FR-003**: Un Cliente DEBE ver y operar únicamente sobre sus propios activos, escaneos, hallazgos y alertas.
- **FR-004**: Un Administrador DEBE poder consultar los activos, escaneos, hallazgos y alertas de todos los clientes, y lanzar escaneos sobre activos de cualquier cliente. El Administrador no registra activos.
- **FR-005**: El sistema DEBE mostrar un mensaje genérico ante credenciales incorrectas y bloquear temporalmente la cuenta tras 5 intentos fallidos consecutivos.
- **FR-006**: El sistema DEBE permitir cerrar sesión, y las sesiones DEBEN expirar tras un periodo de inactividad.
- **FR-007**: El sistema DEBE registrar en una bitácora de auditoría los inicios de sesión exitosos y fallidos, las operaciones administrativas y los accesos a resultados de escaneo (quién, qué y cuándo).

**Activos**

- **FR-008**: El Cliente DEBE poder registrar un activo con nombre (obligatorio) y URL (obligatoria, formato http/https válido).
- **FR-009**: El sistema DEBE aceptar solo URLs que pertenezcan a la lista de objetivos de prueba autorizados, y rechazar las demás con un mensaje explicativo.
- **FR-010**: El sistema DEBE impedir que un mismo cliente registre dos activos con la misma URL.
- **FR-011**: El Cliente DEBE poder ver la lista de sus activos con el estado y la fecha de su último escaneo.

**Escaneos**

- **FR-012**: El Cliente DEBE poder lanzar un escaneo sobre uno de sus activos, y el Administrador sobre cualquier activo; el escaneo registra quién lo lanzó. La solicitud DEBE responder de inmediato, dejando el escaneo en estado "pendiente", sin esperar a que el escaneo termine.
- **FR-013**: El sistema DEBE volver a verificar, al momento de ejecutar el escaneo, que el objetivo sigue en la lista autorizada, y DEBE impedir que el escaneo siga redirecciones hacia objetivos no autorizados.
- **FR-014**: Cada escaneo DEBE pasar por los estados pendiente → en curso → terminado o fallido, con fecha de inicio y fin y, si falla, un motivo legible.
- **FR-015**: El sistema DEBE impedir un segundo escaneo sobre un activo que ya tenga uno pendiente o en curso.
- **FR-016**: El sistema DEBE aplicar un tiempo máximo por escaneo (15 minutos por defecto) y marcarlo como fallido si lo excede.
- **FR-017**: El sistema DEBE notificar los cambios de estado de un escaneo a la pantalla del usuario sin que este tenga que recargarla.
- **FR-018**: El escaneo DEBE usar un perfil de demostración acotado: rastreo del sitio, análisis pasivo de las respuestas y un conjunto reducido de pruebas activas (p. ej., inyección y XSS) con duración limitada. Las pruebas activas solo se permiten porque el objetivo pertenece al entorno de pruebas aislado y autorizado.

**Hallazgos**

- **FR-019**: Cada hallazgo DEBE registrar nombre, descripción, severidad (Alta, Media, Baja o Informativa), categoría OWASP Top 10, URL afectada y recomendación.
- **FR-020**: Si el escáner no aporta categoría OWASP, el sistema DEBE asignarla mediante una regla de mapeo documentada. Un hallazgo sin severidad o sin categoría NO DEBE guardarse ni mostrarse como válido.
- **FR-021**: El Cliente DEBE poder ver los hallazgos de un escaneo y ordenarlos o filtrarlos por severidad y por prioridad de IA.

**Análisis con IA**

- **FR-022**: Al terminar un escaneo, el sistema DEBE enviar sus hallazgos al módulo de IA y guardar, para cada hallazgo, la Prioridad IA (Alta, Media o Baja), la probabilidad de falso positivo (0–100 %) y una justificación breve.
- **FR-023**: El sistema DEBE marcar un hallazgo como "posible falso positivo" cuando la probabilidad asignada por la IA sea del 50 % o más.
- **FR-024**: Si el módulo de IA no responde, responde con error o devuelve datos inválidos, el sistema DEBE conservar la severidad original, marcar el hallazgo como "análisis IA pendiente" y no bloquear el resto del flujo.
- **FR-025**: El sistema DEBE reintentar automáticamente el análisis de los hallazgos pendientes cuando el módulo de IA vuelva a estar disponible.
- **FR-026**: El análisis de IA NO DEBE modificar la severidad original del hallazgo. La Prioridad IA se muestra como un dato adicional, en una columna propia rotulada "Prioridad IA" y con un estilo visual distinto al de la severidad, para que no se confundan.

**Alertas**

- **FR-027**: Cuando un escaneo termina con al menos un hallazgo de severidad Alta, el sistema DEBE generar una alerta y enviar un único correo al cliente dueño del activo, sin importar quién lanzó el escaneo y aunque la IA haya marcado esos hallazgos como posibles falsos positivos.
- **FR-028**: El correo DEBE incluir el nombre del activo, la fecha del escaneo, el conteo de hallazgos por severidad, la lista de hallazgos de severidad Alta (con su Prioridad IA y la marca "posible falso positivo" cuando aplique, o "análisis IA pendiente") y un enlace al detalle del escaneo.
- **FR-029**: Si el envío del correo falla, el sistema DEBE reintentarlo (hasta 3 veces) y la alerta DEBE quedar visible en la plataforma con su estado de envío.
- **FR-030**: El usuario DEBE poder ver en la plataforma la lista de sus alertas.

**Dashboard**

- **FR-031**: El dashboard DEBE mostrar el total de activos, los 5 escaneos más recientes, el conteo de hallazgos por severidad y las 5 alertas más recientes, según el alcance del rol (propios para el Cliente, globales para el Administrador).
- **FR-032**: El dashboard DEBE actualizarse automáticamente cuando termine un escaneo dentro del alcance del usuario.

**Datos y entorno**

- **FR-033**: Todos los datos precargados (usuarios, activos de ejemplo, datos de entrenamiento de la IA) DEBEN ser sintéticos. NO DEBE existir ningún dato personal real.
- **FR-034**: Todo el sistema, incluido el sitio objetivo de pruebas y la bandeja de correo de pruebas, DEBE poder levantarse en un equipo local con un solo comando, y los usuarios semilla DEBEN crearse automáticamente al iniciar.

### Entidades clave

- **Usuario**: persona que accede a la plataforma. Atributos: nombre, correo, credencial protegida, rol (Administrador o Cliente), estado de bloqueo.
- **Activo**: sitio web registrado por un Cliente para monitorear. Atributos: nombre, URL, propietario (Cliente), fecha de registro. Un Cliente tiene muchos Activos.
- **Escaneo**: ejecución de un análisis sobre un Activo. Atributos: activo, lanzado por (Usuario), estado (pendiente, en curso, terminado, fallido), fecha de solicitud, inicio y fin, motivo de fallo. Un Activo tiene muchos Escaneos.
- **Hallazgo**: vulnerabilidad detectada en un Escaneo. Atributos: nombre, descripción, severidad, categoría OWASP Top 10, URL afectada, recomendación, y resultado de IA (Prioridad IA: Alta/Media/Baja, probabilidad de falso positivo, justificación, estado del análisis: analizado o pendiente). Un Escaneo tiene muchos Hallazgos.
- **Alerta**: aviso generado cuando un Escaneo termina con hallazgos de severidad Alta. Atributos: escaneo, destinatario, fecha, resumen, estado de envío del correo (pendiente, enviado, fallido) e intentos.
- **Registro de auditoría**: evento de seguridad. Atributos: usuario (si aplica), acción, recurso, resultado, fecha y hora, origen.

## Criterios de éxito *(obligatorio)*

### Resultados medibles

- **SC-001**: Una persona que sigue el guion de la demo completa el flujo entero (iniciar sesión → registrar activo → escanear → ver hallazgos con IA → ver el correo de alerta → ver el dashboard → vista del administrador) en menos de 5 minutos.
- **SC-002**: El escaneo del sitio de pruebas termina en menos de 3 minutos en el equipo de demostración.
- **SC-003**: El 100 % de los hallazgos mostrados tienen severidad y categoría OWASP Top 10.
- **SC-004**: El correo de alerta llega a la bandeja de pruebas en menos de 1 minuto después de que el escaneo termina.
- **SC-005**: El estado del escaneo y el dashboard reflejan la finalización en menos de 5 segundos, sin que el usuario recargue la página.
- **SC-006**: Con el módulo de IA apagado, el 100 % de los hallazgos siguen visibles con su severidad original y la marca "análisis IA pendiente", y el flujo termina sin errores.
- **SC-007**: El 0 % de los intentos de escaneo sobre objetivos fuera de la lista autorizada llegan a ejecutarse.
- **SC-008**: El 0 % de los intentos de un Cliente de acceder a recursos de otro cliente tienen éxito.
- **SC-009**: Mientras un escaneo está en curso, la interfaz responde a la navegación del usuario en menos de 2 segundos.

## Supuestos

- "Las críticas" de la descripción original equivale a los hallazgos de severidad **Alta**, que es el nivel máximo de la taxonomía (constitución v1.0.1, principio III).
- El objetivo de pruebas es una aplicación web deliberadamente vulnerable que corre en el mismo entorno aislado. Reemplaza a la réplica institucional prevista en la tesis, que queda para una fase posterior.
- La lista de objetivos autorizados la define quien despliega el sistema (configuración), no los usuarios desde la interfaz.
- La bandeja de correo de la demostración es un servidor de pruebas que captura los correos sin enviarlos a Internet.
- Hay dos usuarios semilla: un Administrador y un Cliente. El aislamiento entre clientes (SC-008) se comprueba con pruebas automatizadas que crean clientes de prueba adicionales, no en la demo en vivo.
- El Administrador consulta todo y puede lanzar escaneos sobre activos de cualquier cliente, pero no registra activos.
- No se edita ni elimina un activo en el prototipo; basta con registrarlo y consultarlo.
- El modelo de IA inicial se entrena con un conjunto pequeño de hallazgos sintéticos etiquetados. Medir la reducción de falsos positivos (>30 %) queda para una fase posterior, cuando se defina el dataset con el director.
- La interfaz es web para escritorio; el soporte móvil no es requisito del prototipo.
- Fuera de alcance: registro de usuarios, recuperación de contraseña, MFA, verificación de propiedad de dominios, escaneos programados o completos (activos), umbrales configurables, reportes exportables (PDF/CSV), métricas de infraestructura (CPU/RAM), CVSS completo, MITRE ATT&CK, reentrenamiento continuo de la IA y despliegue en la nube.
