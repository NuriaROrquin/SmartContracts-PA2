using Microsoft.AspNetCore.Mvc;
using PagoPorEtapas.Dominio;
using PagoPorEtapas.Models;
using PagoPorEtapas.Servicios;

namespace PagoPorEtapas.Controllers;

public class ObrasController : Controller
{
    private readonly IContratoObras _contrato;
    private readonly ServicioParticipantes _participantes;
    private readonly Cotizacion _cotizacion;

    public ObrasController(IContratoObras contrato, ServicioParticipantes participantes, Cotizacion cotizacion)
    {
        _contrato = contrato;
        _participantes = participantes;
        _cotizacion = cotizacion;
    }

    private Participante Actual => _participantes.ObtenerActual(HttpContext.Session);

    // GET: /Obras
    public async Task<IActionResult> Index()
    {
        IReadOnlyList<Obra> obras = await _contrato.ListarObrasAsync();
        var modelo = new ListaObrasViewModel(obras.Reverse().ToList(), await ArmarBilleterasAsync());
        return View(modelo);
    }

    // GET: /Obras/Nueva
    [HttpGet]
    public IActionResult Nueva()
    {
        string empresa = _participantes.ConRol(RolParticipante.Empresa).First().Id;
        string aprobador = _participantes.ConRol(RolParticipante.Inspector).FirstOrDefault()?.Id ?? Actual.Id;
        return View(NuevaObraViewModel.ConEjemplo(empresa, aprobador));
    }

    // POST: /Obras/Nueva
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Nueva(NuevaObraViewModel modelo)
    {
        Participante cliente = Actual;
        Participante? proveedor = _participantes.BuscarPorId(modelo.ProveedorId);
        Participante? aprobador = _participantes.BuscarPorId(modelo.AprobadorId);

        if (cliente.Rol != RolParticipante.Cliente)
        {
            ModelState.AddModelError(string.Empty, "Solo un cliente puede crear una obra. Cambiá de participante en la barra de arriba.");
        }
        if (proveedor == null || aprobador == null)
        {
            ModelState.AddModelError(string.Empty, "La empresa o el aprobador elegidos no existen.");
        }

        if (!ModelState.IsValid)
        {
            modelo.CompletarFilasVacias();
            return View(modelo);
        }

        var etapas = modelo.EtapasCompletas()
            .Select(e => new NuevaEtapa(e.Descripcion!.Trim(), _cotizacion.UsdAWei(e.MontoUsd!.Value)))
            .ToList();

        ResultadoOperacion resultado = await _contrato.CrearObraAsync(
            cliente, modelo.Descripcion.Trim(), proveedor!.Direccion, aprobador!.Direccion, etapas);

        if (!resultado.Exito)
        {
            ModelState.AddModelError(string.Empty, resultado.Error!);
            modelo.CompletarFilasVacias();
            return View(modelo);
        }

        Avisar(true, $"Obra creada. Se depositaron USD {modelo.TotalUsd:N0} que quedan retenidos en el contrato.", resultado.HashTransaccion);

        return RedirectToAction(nameof(Detalle), new { id = resultado.ObraId });
    }

    // GET: /Obras/Detalle/0
    public async Task<IActionResult> Detalle(int id)
    {
        Obra? obra = await _contrato.ObtenerObraAsync(id);
        if (obra == null)
        {
            Avisar(false, $"La obra #{id} no existe. Si reiniciaste la red local, las obras anteriores se borraron.");
            return RedirectToAction(nameof(Index));
        }

        return View(new DetalleObraViewModel(obra, Actual, await ArmarBilleterasAsync()));
    }

    // POST: /Obras/MarcarTerminada/0
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarTerminada(int id, int etapa)
    {
        ResultadoOperacion resultado = await _contrato.MarcarTerminadaAsync(Actual, id, etapa);
        Avisar(resultado, $"Avisaste que la etapa {etapa + 1} está terminada. Ahora tiene que revisarla el aprobador.");
        return RedirectToAction(nameof(Detalle), new { id });
    }

    // POST: /Obras/Aprobar/0
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprobar(int id, int etapa)
    {
        Obra? obra = await _contrato.ObtenerObraAsync(id);
        ResultadoOperacion resultado = await _contrato.AprobarAsync(Actual, id, etapa);

        string monto = obra != null && etapa >= 0 && etapa < obra.Etapas.Count
            ? $"USD {_cotizacion.WeiAUsd(obra.Etapas[etapa].MontoWei):N0}"
            : "el monto de la etapa";
        Avisar(resultado, $"Etapa {etapa + 1} aprobada. El contrato le transfirió {monto} a la empresa automáticamente.");
        return RedirectToAction(nameof(Detalle), new { id });
    }

    // POST: /Obras/Rechazar/0
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(int id, int etapa)
    {
        ResultadoOperacion resultado = await _contrato.RechazarAsync(Actual, id, etapa);
        Avisar(resultado, $"Etapa {etapa + 1} rechazada: vuelve a estar en curso y la plata sigue retenida.");
        return RedirectToAction(nameof(Detalle), new { id });
    }

    private async Task<BilleterasViewModel> ArmarBilleterasAsync()
    {
        var saldos = new List<SaldoParticipante>();
        foreach (Participante participante in _participantes.Todos)
        {
            decimal saldo = _cotizacion.WeiAUsd(await _contrato.ObtenerSaldoAsync(participante.Direccion));
            saldos.Add(new SaldoParticipante(participante, saldo));
        }

        string direccionContrato = await _contrato.ObtenerDireccionContratoAsync();
        decimal saldoContrato = _cotizacion.WeiAUsd(await _contrato.ObtenerSaldoAsync(direccionContrato));

        return new BilleterasViewModel(saldos, saldoContrato, direccionContrato,
            _contrato.UrlExplorador($"address/{direccionContrato}"), Actual.Id);
    }

    private void Avisar(bool exito, string mensaje, string? hashTransaccion = null)
    {
        TempData["Mensaje"] = mensaje;
        TempData["TipoMensaje"] = exito ? "success" : "danger";

        if (hashTransaccion != null)
        {
            TempData["HashTransaccion"] = hashTransaccion;
            string? url = _contrato.UrlExplorador($"tx/{hashTransaccion}");
            if (url != null)
            {
                TempData["UrlTransaccion"] = url;
            }
        }
    }

    private void Avisar(ResultadoOperacion resultado, string mensajeExito)
    {
        if (resultado.Exito)
        {
            Avisar(true, mensajeExito, resultado.HashTransaccion);
        }
        else
        {
            Avisar(false, resultado.Error!);
        }
    }
}
