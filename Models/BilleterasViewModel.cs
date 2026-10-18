using PagoPorEtapas.Dominio;

namespace PagoPorEtapas.Models;

public class BilleterasViewModel
{
    public IReadOnlyList<SaldoParticipante> Participantes { get; }
    public decimal SaldoContratoUsd { get; }
    public string DireccionContrato { get; }
    public string? UrlContrato { get; }
    public string? IdParticipanteActual { get; }

    public BilleterasViewModel(IReadOnlyList<SaldoParticipante> participantes, decimal saldoContratoUsd,
        string direccionContrato, string? urlContrato, string? idParticipanteActual)
    {
        Participantes = participantes;
        SaldoContratoUsd = saldoContratoUsd;
        DireccionContrato = direccionContrato;
        UrlContrato = urlContrato;
        IdParticipanteActual = idParticipanteActual;
    }
}

public class SaldoParticipante
{
    public Participante Participante { get; }
    public decimal SaldoUsd { get; }

    public SaldoParticipante(Participante participante, decimal saldoUsd)
    {
        Participante = participante;
        SaldoUsd = saldoUsd;
    }
}

public class ListaObrasViewModel
{
    public IReadOnlyList<Obra> Obras { get; }
    public BilleterasViewModel Billeteras { get; }

    public ListaObrasViewModel(IReadOnlyList<Obra> obras, BilleterasViewModel billeteras)
    {
        Obras = obras;
        Billeteras = billeteras;
    }
}

public class DetalleObraViewModel
{
    public Obra Obra { get; }
    public Participante Actual { get; }
    public BilleterasViewModel Billeteras { get; }

    public DetalleObraViewModel(Obra obra, Participante actual, BilleterasViewModel billeteras)
    {
        Obra = obra;
        Actual = actual;
        Billeteras = billeteras;
    }

    public bool ActualEsCliente => Actual.EsDuenioDe(Obra.Cliente);
    public bool ActualEsEmpresa => Actual.EsDuenioDe(Obra.Proveedor);
    public bool ActualEsAprobador => Actual.EsDuenioDe(Obra.Aprobador);
}
