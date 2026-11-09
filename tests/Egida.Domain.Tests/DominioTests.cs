using Egida.Domain.Activos;
using Egida.Domain.Alertas;
using Egida.Domain.Clasificacion;
using Egida.Domain.Comun;
using Egida.Domain.Escaneos;
using Egida.Domain.Usuarios;

namespace Egida.Domain.Tests;

public class UsuarioTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Correo_se_guarda_en_minusculas()
    {
        var u = new Usuario("Cliente", "  Cliente@EGIDA.test ", Rol.Cliente, Ahora);
        Assert.Equal("cliente@egida.test", u.Correo);
    }

    [Fact]
    public void Quinto_fallo_bloquea_15_minutos_y_reinicia_contador()
    {
        var u = new Usuario("C", "c@egida.test", Rol.Cliente, Ahora);
        for (var i = 0; i < 4; i++) Assert.False(u.RegistrarLoginFallido(Ahora));
        Assert.Equal(4, u.IntentosFallidos);

        Assert.True(u.RegistrarLoginFallido(Ahora));
        Assert.Equal(0, u.IntentosFallidos);
        Assert.Equal(Ahora.AddMinutes(15), u.BloqueadoHasta);
        Assert.True(u.EstaBloqueado(Ahora.AddMinutes(14)));
        Assert.False(u.EstaBloqueado(Ahora.AddMinutes(15)));
    }

    [Fact]
    public void Login_exitoso_reinicia_intentos()
    {
        var u = new Usuario("C", "c@egida.test", Rol.Cliente, Ahora);
        u.RegistrarLoginFallido(Ahora);
        u.RegistrarLoginExitoso();
        Assert.Equal(0, u.IntentosFallidos);
    }
}

public class ActivoTests
{
    [Theory]
    [InlineData("HTTP://Juice-Shop:3000/", "http://juice-shop:3000", "http://juice-shop:3000")]
    [InlineData("https://ejemplo.com:443/ruta/#frag", "https://ejemplo.com/ruta", "https://ejemplo.com")]
    [InlineData("http://juice-shop:3000/rest/?q=1", "http://juice-shop:3000/rest?q=1", "http://juice-shop:3000")]
    public void Url_se_normaliza(string entrada, string esperada, string origen)
    {
        var url = UrlNormalizada.Crear(entrada);
        Assert.Equal(esperada, url.Valor);
        Assert.Equal(origen, url.Origen);
    }

    [Theory]
    [InlineData("")]
    [InlineData("juice-shop:3000")]
    [InlineData("ftp://juice-shop")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/relativa")]
    public void Url_invalida_se_rechaza(string entrada) => Assert.False(UrlNormalizada.TryCrear(entrada, out _));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Nombre_vacio_se_rechaza(string nombre) =>
        Assert.Throws<ExcepcionDominio>(() =>
            new Activo(nombre, UrlNormalizada.Crear("http://juice-shop:3000"), Guid.NewGuid(), DateTimeOffset.UtcNow));

    [Fact]
    public void Nombre_mayor_a_100_se_rechaza() =>
        Assert.Throws<ExcepcionDominio>(() =>
            new Activo(new string('x', 101), UrlNormalizada.Crear("http://juice-shop:3000"), Guid.NewGuid(), DateTimeOffset.UtcNow));
}

public class EscaneoTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UtcNow;

    [Fact]
    public void Flujo_feliz_pendiente_en_curso_terminado()
    {
        var e = new Escaneo(Guid.NewGuid(), Guid.NewGuid(), T0);
        Assert.Equal(EstadoEscaneo.Pendiente, e.Estado);
        e.Iniciar(T0.AddSeconds(1));
        Assert.Equal(EstadoEscaneo.EnCurso, e.Estado);
        Assert.NotNull(e.IniciadoEn);
        e.Terminar(7, T0.AddSeconds(60));
        Assert.Equal(EstadoEscaneo.Terminado, e.Estado);
        Assert.Equal(7, e.TotalHallazgos);
        Assert.NotNull(e.FinalizadoEn);
    }

    [Fact]
    public void Fallar_exige_motivo()
    {
        var e = new Escaneo(Guid.NewGuid(), Guid.NewGuid(), T0);
        Assert.Throws<ExcepcionDominio>(() => e.Fallar(" ", T0));
        e.Fallar(MotivosFallo.TiempoExcedido, T0);
        Assert.Equal(EstadoEscaneo.Fallido, e.Estado);
        Assert.Equal("Tiempo excedido", e.MotivoFallo);
    }

    [Fact]
    public void Estados_finales_no_admiten_transiciones()
    {
        var terminado = new Escaneo(Guid.NewGuid(), Guid.NewGuid(), T0);
        terminado.Iniciar(T0);
        terminado.Terminar(0, T0);
        Assert.Throws<ExcepcionDominio>(() => terminado.Fallar("x", T0));
        Assert.Throws<ExcepcionDominio>(() => terminado.Iniciar(T0));

        var fallido = new Escaneo(Guid.NewGuid(), Guid.NewGuid(), T0);
        fallido.Fallar(MotivosFallo.Interrumpido, T0);
        Assert.Throws<ExcepcionDominio>(() => fallido.Iniciar(T0));
        Assert.Throws<ExcepcionDominio>(() => fallido.Terminar(1, T0));
    }

    [Fact]
    public void No_se_termina_sin_iniciar()
    {
        var e = new Escaneo(Guid.NewGuid(), Guid.NewGuid(), T0);
        Assert.Throws<ExcepcionDominio>(() => e.Terminar(0, T0));
    }
}

public class HallazgoTests
{
    public static DatosHallazgo Datos(Severidad? severidad = Severidad.Alta, bool conCategoria = true, int confianza = 2) => new(
        "SQL Injection", "desc", severidad,
        conCategoria ? MapeadorOwasp.Mapear(["OWASP_2025_A05"], 89) : null,
        "http://juice-shop:3000/rest/products/search?q=a", "Use consultas parametrizadas.", 40018, confianza,
        "GET", "q", new string('e', 3000), 89);

    [Fact]
    public void Sin_severidad_no_se_construye() =>
        Assert.Throws<ExcepcionDominio>(() => new Hallazgo(Guid.NewGuid(), Datos(severidad: null)));

    [Fact]
    public void Sin_categoria_no_se_construye() =>
        Assert.Throws<ExcepcionDominio>(() => new Hallazgo(Guid.NewGuid(), Datos(conCategoria: false)));

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Confianza_fuera_de_rango_se_rechaza(int confianza) =>
        Assert.Throws<ExcepcionDominio>(() => new Hallazgo(Guid.NewGuid(), Datos(confianza: confianza)));

    [Fact]
    public void Evidencia_se_trunca_y_estado_ia_inicial_es_pendiente()
    {
        var h = new Hallazgo(Guid.NewGuid(), Datos());
        Assert.Equal(2000, h.Evidencia!.Length);
        Assert.Equal(EstadoAnalisisIA.Pendiente, h.EstadoIA);
        Assert.Equal("A05:2025 - Injection", h.CategoriaOwasp);
    }

    [Theory]
    [InlineData(0.5, true)]
    [InlineData(0.4999, false)]
    [InlineData(0.9, true)]
    public void Posible_falso_positivo_desde_50_por_ciento(decimal prob, bool esperado)
    {
        var h = new Hallazgo(Guid.NewGuid(), Datos());
        h.RegistrarAnalisisIA(PrioridadIA.Media, prob, "justificación", "rf", DateTimeOffset.UtcNow);
        Assert.Equal(esperado, h.EsPosibleFalsoPositivo);
        Assert.Equal(Severidad.Alta, h.Severidad); // FR-026: la IA no cambia la severidad
        Assert.Equal(EstadoAnalisisIA.Analizado, h.EstadoIA);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.01)]
    public void Probabilidad_fuera_de_rango_se_rechaza(decimal prob)
    {
        var h = new Hallazgo(Guid.NewGuid(), Datos());
        Assert.Throws<ExcepcionDominio>(() => h.RegistrarAnalisisIA(PrioridadIA.Alta, prob, "x", "rf", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Severidad_no_tiene_setter_publico() =>
        Assert.False(typeof(Hallazgo).GetProperty(nameof(Hallazgo.Severidad))!.SetMethod!.IsPublic);
}

public class MapeadorOwaspTests
{
    [Fact]
    public void Etiqueta_2025_tiene_prioridad()
    {
        var c = MapeadorOwasp.Mapear(["OWASP_2021_A03", "OWASP_2025_A05", "CWE-89"], 89);
        Assert.Equal("A05:2025 - Injection", c.Texto);
        Assert.Equal(OrigenCategoriaOwasp.EtiquetaZap2025, c.Origen);
    }

    [Theory]
    [InlineData("OWASP_2021_A03", "A05:2025 - Injection")]
    [InlineData("OWASP_2021_A05", "A02:2025 - Security Misconfiguration")]
    [InlineData("OWASP_2021_A02", "A04:2025 - Cryptographic Failures")]
    [InlineData("OWASP_2021_A10", "A01:2025 - Broken Access Control")]
    public void Etiqueta_2021_se_traduce(string etiqueta, string esperada)
    {
        var c = MapeadorOwasp.Mapear([etiqueta], null);
        Assert.Equal(esperada, c.Texto);
        Assert.Equal(OrigenCategoriaOwasp.EtiquetaZap2021, c.Origen);
    }

    [Fact]
    public void Sin_etiquetas_usa_tabla_cwe()
    {
        var c = MapeadorOwasp.Mapear([], 79);
        Assert.Equal("A05:2025 - Injection", c.Texto);
        Assert.Equal(OrigenCategoriaOwasp.TablaCwe, c.Origen);
    }

    [Fact]
    public void Sin_nada_usa_categoria_por_defecto()
    {
        var c = MapeadorOwasp.Mapear(null, 999999);
        Assert.Equal("A02:2025 - Security Misconfiguration", c.Texto);
        Assert.Equal(OrigenCategoriaOwasp.PorDefecto, c.Origen);
    }

    [Fact]
    public void Todas_las_salidas_tienen_formato_y_recomendacion()
    {
        foreach (var codigo in CategoriasOwasp2025.Nombres.Keys)
        {
            var c = MapeadorOwasp.Mapear([$"OWASP_2025_{codigo}"], null);
            Assert.Matches(@"^A(0[1-9]|10):2025 - .+$", c.Texto);
            Assert.False(string.IsNullOrWhiteSpace(MapeadorOwasp.RecomendacionPara(c)));
        }
    }
}

public class AlertaTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.UtcNow;

    private static Alerta Nueva() => new(Guid.NewGuid(), Guid.NewGuid(), "c@egida.test", 2, 1, "resumen", T0);

    [Fact]
    public void No_se_crea_sin_altas() =>
        Assert.Throws<ExcepcionDominio>(() => new Alerta(Guid.NewGuid(), Guid.NewGuid(), "c@egida.test", 0, 0, "r", T0));

    [Fact]
    public void Reintenta_con_backoff_y_falla_al_tercer_intento()
    {
        var a = Nueva();
        a.RegistrarFalloEnvio("smtp caído", T0);
        Assert.Equal(EstadoEnvio.Pendiente, a.EstadoEnvio);
        Assert.Equal(T0.AddSeconds(10), a.ProximoIntentoEn);
        a.RegistrarFalloEnvio("smtp caído", T0);
        Assert.Equal(T0.AddSeconds(30), a.ProximoIntentoEn);
        a.RegistrarFalloEnvio("smtp caído", T0);
        Assert.Equal(EstadoEnvio.Fallido, a.EstadoEnvio);
        Assert.Equal(3, a.IntentosEnvio);
    }

    [Fact]
    public void Envio_exitoso()
    {
        var a = Nueva();
        a.RegistrarEnvioExitoso(T0);
        Assert.Equal(EstadoEnvio.Enviado, a.EstadoEnvio);
        Assert.Equal(T0, a.EnviadaEn);
    }
}
