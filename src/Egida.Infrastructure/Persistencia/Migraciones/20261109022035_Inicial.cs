using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Egida.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    correo = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    hash_contrasena = table.Column<string>(type: "text", nullable: false),
                    rol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    intentos_fallidos = table.Column<int>(type: "integer", nullable: false),
                    bloqueado_hasta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "activos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    propietario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_activos", x => x.id);
                    table.ForeignKey(
                        name: "fk_activos_usuarios_propietario_id",
                        column: x => x.propietario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "registros_auditoria",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correo_intentado = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    accion = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    recurso = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    resultado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ip_origen = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registros_auditoria", x => x.id);
                    table.ForeignKey(
                        name: "fk_registros_auditoria_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "escaneos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    activo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    solicitado_por_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    solicitado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    iniciado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finalizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    motivo_fallo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    total_hallazgos = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_escaneos", x => x.id);
                    table.ForeignKey(
                        name: "fk_escaneos_activos_activo_id",
                        column: x => x.activo_id,
                        principalTable: "activos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_escaneos_usuarios_solicitado_por_id",
                        column: x => x.solicitado_por_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "alertas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    escaneo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destinatario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    correo_destino = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    creada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    total_altas = table.Column<int>(type: "integer", nullable: false),
                    total_posibles_fp = table.Column<int>(type: "integer", nullable: false),
                    resumen = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    estado_envio = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    intentos_envio = table.Column<int>(type: "integer", nullable: false),
                    proximo_intento_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ultimo_error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    enviada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alertas", x => x.id);
                    table.ForeignKey(
                        name: "fk_alertas_escaneos_escaneo_id",
                        column: x => x.escaneo_id,
                        principalTable: "escaneos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_alertas_usuarios_destinatario_id",
                        column: x => x.destinatario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "hallazgos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    escaneo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    severidad = table.Column<int>(type: "integer", nullable: false),
                    categoria_owasp = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    origen_categoria = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    url_afectada = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    recomendacion = table.Column<string>(type: "text", nullable: false),
                    metodo_http = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    parametro = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    evidencia = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    tipo_contenido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cwe_id = table.Column<int>(type: "integer", nullable: true),
                    plugin_id = table.Column<int>(type: "integer", nullable: false),
                    confianza_escaner = table.Column<int>(type: "integer", nullable: false),
                    ocurrencias = table.Column<int>(type: "integer", nullable: false),
                    estado_ia = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    prioridad_ia = table.Column<int>(type: "integer", nullable: true),
                    probabilidad_falso_positivo = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    justificacion_ia = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    version_modelo_ia = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    analizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    intentos_ia = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hallazgos", x => x.id);
                    table.ForeignKey(
                        name: "fk_hallazgos_escaneos_escaneo_id",
                        column: x => x.escaneo_id,
                        principalTable: "escaneos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_activos_propietario_id_url",
                table: "activos",
                columns: new[] { "propietario_id", "url" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_alertas_destinatario_id",
                table: "alertas",
                column: "destinatario_id");

            migrationBuilder.CreateIndex(
                name: "ix_alertas_escaneo_id",
                table: "alertas",
                column: "escaneo_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_alertas_estado_envio_proximo_intento_en",
                table: "alertas",
                columns: new[] { "estado_envio", "proximo_intento_en" });

            migrationBuilder.CreateIndex(
                name: "ix_escaneos_estado_solicitado_en",
                table: "escaneos",
                columns: new[] { "estado", "solicitado_en" });

            migrationBuilder.CreateIndex(
                name: "ix_escaneos_solicitado_por_id",
                table: "escaneos",
                column: "solicitado_por_id");

            migrationBuilder.CreateIndex(
                name: "ux_escaneos_activo_en_marcha",
                table: "escaneos",
                column: "activo_id",
                unique: true,
                filter: "estado IN ('Pendiente','EnCurso')");

            migrationBuilder.CreateIndex(
                name: "ix_hallazgos_escaneo_id_severidad",
                table: "hallazgos",
                columns: new[] { "escaneo_id", "severidad" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_hallazgos_estado_ia",
                table: "hallazgos",
                column: "estado_ia");

            migrationBuilder.CreateIndex(
                name: "ix_registros_auditoria_fecha",
                table: "registros_auditoria",
                column: "fecha");

            migrationBuilder.CreateIndex(
                name: "ix_registros_auditoria_usuario_id",
                table: "registros_auditoria",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_correo",
                table: "usuarios",
                column: "correo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alertas");

            migrationBuilder.DropTable(
                name: "hallazgos");

            migrationBuilder.DropTable(
                name: "registros_auditoria");

            migrationBuilder.DropTable(
                name: "escaneos");

            migrationBuilder.DropTable(
                name: "activos");

            migrationBuilder.DropTable(
                name: "usuarios");
        }
    }
}
