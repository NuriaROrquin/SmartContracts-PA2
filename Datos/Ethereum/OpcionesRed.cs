namespace PagoPorEtapas.Datos.Ethereum;

public class OpcionesRed
{
    public string Nombre { get; set; } = "Ethereum";
    public string UrlNodo { get; set; } = string.Empty;

    // 1337 = Ganache, 11155111 = Sepolia
    public long ChainId { get; set; }

    // Si está vacía y PublicarContratoAutomaticamente es true, la app publica el contrato
    public string DireccionContrato { get; set; } = string.Empty;
    public bool PublicarContratoAutomaticamente { get; set; }

    public string UrlExplorador { get; set; } = string.Empty;
    public decimal UsdPorEth { get; set; } = 1000;
}

public class OpcionesParticipante
{
    public string Id { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string ClavePrivada { get; set; } = string.Empty;
}
