using System.Numerics;

namespace PagoPorEtapas.Dominio;

// Cotización fija para mostrar en dólares los montos en ETH de prueba
public class Cotizacion
{
    private const decimal WeiPorEth = 1_000_000_000_000_000_000m;

    public decimal UsdPorEth { get; }

    public Cotizacion(decimal usdPorEth)
    {
        if (usdPorEth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(usdPorEth), "La cotización tiene que ser mayor a cero.");
        }
        UsdPorEth = usdPorEth;
    }

    public BigInteger UsdAWei(decimal usd) => new BigInteger(decimal.Round(usd / UsdPorEth * WeiPorEth));

    public decimal WeiAUsd(BigInteger wei) => (decimal)wei / WeiPorEth * UsdPorEth;

    public decimal WeiAEth(BigInteger wei) => (decimal)wei / WeiPorEth;
}
