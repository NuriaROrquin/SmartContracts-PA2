using System.Numerics;

namespace PagoPorEtapas.Dominio;

// Mismo orden que el enum EstadoEtapa del contrato
public enum EstadoEtapa
{
    EnCurso = 0,
    Terminada = 1,
    Aprobada = 2
}

public class Etapa
{
    public int Numero { get; }
    public string Descripcion { get; }
    public BigInteger MontoWei { get; }
    public EstadoEtapa Estado { get; }

    public Etapa(int numero, string descripcion, BigInteger montoWei, EstadoEtapa estado)
    {
        Numero = numero;
        Descripcion = descripcion;
        MontoWei = montoWei;
        Estado = estado;
    }
}

public class Obra
{
    public int Id { get; }
    public string Descripcion { get; }
    public string Cliente { get; }
    public string Proveedor { get; }
    public string Aprobador { get; }
    public BigInteger SaldoRetenidoWei { get; }
    public IReadOnlyList<Etapa> Etapas { get; }

    public Obra(int id, string descripcion, string cliente, string proveedor, string aprobador, BigInteger saldoRetenidoWei, IReadOnlyList<Etapa> etapas)
    {
        Id = id;
        Descripcion = descripcion;
        Cliente = cliente;
        Proveedor = proveedor;
        Aprobador = aprobador;
        SaldoRetenidoWei = saldoRetenidoWei;
        Etapas = etapas;
    }

    public BigInteger TotalWei => Etapas.Aggregate(BigInteger.Zero, (suma, etapa) => suma + etapa.MontoWei);
    public BigInteger PagadoWei => TotalWei - SaldoRetenidoWei;
    public int EtapasAprobadas => Etapas.Count(e => e.Estado == EstadoEtapa.Aprobada);
    public bool Finalizada => EtapasAprobadas == Etapas.Count;
}
