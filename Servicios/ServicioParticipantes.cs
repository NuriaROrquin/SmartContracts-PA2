using PagoPorEtapas.Dominio;

namespace PagoPorEtapas.Servicios;

public class ServicioParticipantes
{
    private const string ClaveSesion = "ParticipanteActual";

    public IReadOnlyList<Participante> Todos { get; }

    public ServicioParticipantes(IReadOnlyList<Participante> participantes)
    {
        Todos = participantes;
    }

    public Participante? BuscarPorId(string? id) =>
        Todos.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

    public Participante? BuscarPorDireccion(string direccion) =>
        Todos.FirstOrDefault(p => p.EsDuenioDe(direccion));

    public IEnumerable<Participante> ConRol(RolParticipante rol) => Todos.Where(p => p.Rol == rol);

    public string NombreDe(string direccion) => BuscarPorDireccion(direccion)?.Nombre ?? Abreviar(direccion);

    public Participante ObtenerActual(ISession sesion) =>
        BuscarPorId(sesion.GetString(ClaveSesion)) ?? ConRol(RolParticipante.Cliente).First();

    public void CambiarActual(ISession sesion, string id)
    {
        if (BuscarPorId(id) != null)
        {
            sesion.SetString(ClaveSesion, id);
        }
    }

    public static string Abreviar(string direccion) =>
        direccion.Length > 12 ? $"{direccion[..6]}…{direccion[^4..]}" : direccion;
}
