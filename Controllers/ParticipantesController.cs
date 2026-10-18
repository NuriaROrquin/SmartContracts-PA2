using Microsoft.AspNetCore.Mvc;
using PagoPorEtapas.Servicios;

namespace PagoPorEtapas.Controllers;

public class ParticipantesController : Controller
{
    private readonly ServicioParticipantes _participantes;

    public ParticipantesController(ServicioParticipantes participantes)
    {
        _participantes = participantes;
    }

    // POST: /Participantes/Cambiar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cambiar(string id, string? volverA)
    {
        _participantes.CambiarActual(HttpContext.Session, id);

        if (!string.IsNullOrEmpty(volverA) && Url.IsLocalUrl(volverA))
        {
            return LocalRedirect(volverA);
        }
        return RedirectToAction("Index", "Obras");
    }
}
