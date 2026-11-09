namespace Egida.Domain.Comun;

/// <summary>Regla de negocio violada. <see cref="Codigo"/> es legible por máquina.</summary>
public class ExcepcionDominio(string codigo, string mensaje) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}
