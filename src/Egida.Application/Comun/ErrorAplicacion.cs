namespace Egida.Application.Comun;

public enum TipoError
{
    Validacion,
    NoAutorizado,
    Prohibido,
    NoEncontrado,
    Conflicto,
}

/// <summary>Error esperado de un caso de uso. La API lo traduce a ProblemDetails.</summary>
public sealed class ErrorAplicacion(TipoError tipo, string codigo, string mensaje,
    IDictionary<string, string[]>? errores = null) : Exception(mensaje)
{
    public TipoError Tipo { get; } = tipo;
    public string Codigo { get; } = codigo;
    public IDictionary<string, string[]>? Errores { get; } = errores;

    public static ErrorAplicacion NoEncontrado(string recurso) =>
        new(TipoError.NoEncontrado, "no_encontrado", $"No se encontró {recurso}.");

    public static ErrorAplicacion Validacion(string campo, string mensaje) =>
        new(TipoError.Validacion, "validacion", "La solicitud tiene datos inválidos.",
            new Dictionary<string, string[]> { [campo] = [mensaje] });
}
