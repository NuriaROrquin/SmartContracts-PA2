namespace PagoPorEtapas.Dominio;

public enum RolParticipante
{
    Cliente,
    Empresa,
    Inspector
}

public class Participante
{
    public string Id { get; }
    public string Nombre { get; }
    public RolParticipante Rol { get; }
    public string Direccion { get; }
    public string ClavePrivada { get; }

    public Participante(string id, string nombre, RolParticipante rol, string direccion, string clavePrivada)
    {
        Id = id;
        Nombre = nombre;
        Rol = rol;
        Direccion = direccion;
        ClavePrivada = clavePrivada;
    }

    public bool EsDuenioDe(string direccion) => string.Equals(Direccion, direccion, StringComparison.OrdinalIgnoreCase);
}
