using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace PagoPorEtapas.Datos.Ethereum;

// Mapeo de las funciones y eventos de PagoPorEtapas.sol para Nethereum

[Function("crearObra", "uint256")]
public class CrearObraFuncion : FunctionMessage
{
    [Parameter("string", "descripcion", 1)]
    public string Descripcion { get; set; } = string.Empty;

    [Parameter("address", "proveedor", 2)]
    public string Proveedor { get; set; } = string.Empty;

    [Parameter("address", "aprobador", 3)]
    public string Aprobador { get; set; } = string.Empty;

    [Parameter("string[]", "descripcionesEtapas", 4)]
    public List<string> DescripcionesEtapas { get; set; } = new();

    [Parameter("uint256[]", "montosEtapas", 5)]
    public List<BigInteger> MontosEtapas { get; set; } = new();
}

[Function("marcarTerminada")]
public class MarcarTerminadaFuncion : FunctionMessage
{
    [Parameter("uint256", "obraId", 1)]
    public BigInteger ObraId { get; set; }

    [Parameter("uint256", "numeroEtapa", 2)]
    public BigInteger NumeroEtapa { get; set; }
}

[Function("aprobar")]
public class AprobarFuncion : FunctionMessage
{
    [Parameter("uint256", "obraId", 1)]
    public BigInteger ObraId { get; set; }

    [Parameter("uint256", "numeroEtapa", 2)]
    public BigInteger NumeroEtapa { get; set; }
}

[Function("rechazar")]
public class RechazarFuncion : FunctionMessage
{
    [Parameter("uint256", "obraId", 1)]
    public BigInteger ObraId { get; set; }

    [Parameter("uint256", "numeroEtapa", 2)]
    public BigInteger NumeroEtapa { get; set; }
}

[Function("cantidadObras", "uint256")]
public class CantidadObrasFuncion : FunctionMessage
{
}

[Function("consultarObra", typeof(ConsultarObraResultado))]
public class ConsultarObraFuncion : FunctionMessage
{
    [Parameter("uint256", "obraId", 1)]
    public BigInteger ObraId { get; set; }
}

[FunctionOutput]
public class ConsultarObraResultado : IFunctionOutputDTO
{
    [Parameter("string", "descripcion", 1)]
    public string Descripcion { get; set; } = string.Empty;

    [Parameter("address", "cliente", 2)]
    public string Cliente { get; set; } = string.Empty;

    [Parameter("address", "proveedor", 3)]
    public string Proveedor { get; set; } = string.Empty;

    [Parameter("address", "aprobador", 4)]
    public string Aprobador { get; set; } = string.Empty;

    [Parameter("uint256", "saldoRetenido", 5)]
    public BigInteger SaldoRetenido { get; set; }

    [Parameter("uint256", "cantidadEtapas", 6)]
    public BigInteger CantidadEtapas { get; set; }
}

[Function("consultarEtapa", typeof(ConsultarEtapaResultado))]
public class ConsultarEtapaFuncion : FunctionMessage
{
    [Parameter("uint256", "obraId", 1)]
    public BigInteger ObraId { get; set; }

    [Parameter("uint256", "numeroEtapa", 2)]
    public BigInteger NumeroEtapa { get; set; }
}

[FunctionOutput]
public class ConsultarEtapaResultado : IFunctionOutputDTO
{
    [Parameter("string", "descripcion", 1)]
    public string Descripcion { get; set; } = string.Empty;

    [Parameter("uint256", "monto", 2)]
    public BigInteger Monto { get; set; }

    // Los enum de Solidity se codifican como uint8
    [Parameter("uint8", "estado", 3)]
    public byte Estado { get; set; }
}

[Event("ObraCreada")]
public class ObraCreadaEvento : IEventDTO
{
    [Parameter("uint256", "obraId", 1, true)]
    public BigInteger ObraId { get; set; }

    [Parameter("address", "cliente", 2, true)]
    public string Cliente { get; set; } = string.Empty;

    [Parameter("address", "proveedor", 3, true)]
    public string Proveedor { get; set; } = string.Empty;

    [Parameter("uint256", "total", 4, false)]
    public BigInteger Total { get; set; }
}
