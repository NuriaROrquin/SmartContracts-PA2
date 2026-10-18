using System.Numerics;

namespace PagoPorEtapas.Dominio;

public interface IContratoObras
{
    string NombreRed { get; }

    Task<string> ObtenerDireccionContratoAsync();

    string? UrlExplorador(string ruta);

    Task<ResultadoOperacion> CrearObraAsync(Participante cliente, string descripcion, string direccionProveedor,
        string direccionAprobador, IReadOnlyList<NuevaEtapa> etapas);

    Task<ResultadoOperacion> MarcarTerminadaAsync(Participante proveedor, int obraId, int numeroEtapa);

    Task<ResultadoOperacion> AprobarAsync(Participante aprobador, int obraId, int numeroEtapa);

    Task<ResultadoOperacion> RechazarAsync(Participante aprobador, int obraId, int numeroEtapa);

    Task<IReadOnlyList<Obra>> ListarObrasAsync();

    Task<Obra?> ObtenerObraAsync(int obraId);

    Task<BigInteger> ObtenerSaldoAsync(string direccion);
}

public class NuevaEtapa
{
    public string Descripcion { get; }
    public BigInteger MontoWei { get; }

    public NuevaEtapa(string descripcion, BigInteger montoWei)
    {
        Descripcion = descripcion;
        MontoWei = montoWei;
    }
}

public class ResultadoOperacion
{
    public bool Exito { get; }
    public string? HashTransaccion { get; }
    public string? Error { get; }
    public int? ObraId { get; }

    private ResultadoOperacion(bool exito, string? hashTransaccion, string? error, int? obraId)
    {
        Exito = exito;
        HashTransaccion = hashTransaccion;
        Error = error;
        ObraId = obraId;
    }

    public static ResultadoOperacion Exitosa(string hashTransaccion, int? obraId = null) => new(true, hashTransaccion, null, obraId);

    public static ResultadoOperacion Fallida(string error) => new(false, null, error, null);
}

public class ErrorDeRedException : Exception
{
    public ErrorDeRedException(string mensaje, Exception? causa = null) : base(mensaje, causa)
    {
    }
}
