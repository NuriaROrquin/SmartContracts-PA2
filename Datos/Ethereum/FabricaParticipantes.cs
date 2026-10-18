using Nethereum.Signer;
using PagoPorEtapas.Dominio;

namespace PagoPorEtapas.Datos.Ethereum;

public static class FabricaParticipantes
{
    public static IReadOnlyList<Participante> Crear(IEnumerable<OpcionesParticipante> opciones)
    {
        var participantes = new List<Participante>();

        foreach (OpcionesParticipante opcion in opciones)
        {
            if (!Enum.TryParse(opcion.Rol, ignoreCase: true, out RolParticipante rol))
            {
                throw new InvalidOperationException($"El participante '{opcion.Id}' tiene un rol desconocido: '{opcion.Rol}'.");
            }

            if (string.IsNullOrWhiteSpace(opcion.ClavePrivada))
            {
                throw new InvalidOperationException($"Falta la clave privada del participante '{opcion.Id}'.");
            }

            string direccion = new EthECKey(opcion.ClavePrivada).GetPublicAddress();
            participantes.Add(new Participante(opcion.Id, opcion.Nombre, rol, direccion, opcion.ClavePrivada));
        }

        if (!participantes.Any(p => p.Rol == RolParticipante.Cliente) || !participantes.Any(p => p.Rol == RolParticipante.Empresa))
        {
            throw new InvalidOperationException("La sección \"Participantes\" tiene que tener al menos un Cliente y una Empresa.");
        }

        return participantes;
    }
}
