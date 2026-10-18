using System.Numerics;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using PagoPorEtapas.Dominio;

namespace PagoPorEtapas.Datos.Ethereum;

public class ContratoObrasEthereum : IContratoObras
{
    private static readonly TimeSpan TiempoMaximoConfirmacion = TimeSpan.FromSeconds(90);

    private readonly OpcionesRed _opciones;
    private readonly Participante _publicador;
    private readonly string _abi;
    private readonly string _bytecode;

    // Evita que dos requests publiquen el contrato al mismo tiempo
    private readonly SemaphoreSlim _candadoPublicacion = new(1, 1);
    private string? _direccionPublicada;

    private readonly string? _archivoDireccion;

    public ContratoObrasEthereum(OpcionesRed opciones, Participante publicador, string abi, string bytecode, string? archivoDireccion = null)
    {
        _opciones = opciones;
        _publicador = publicador;
        _abi = abi;
        _bytecode = bytecode;
        _archivoDireccion = archivoDireccion;
    }

    public string NombreRed => _opciones.Nombre;

    public string? UrlExplorador(string ruta) =>
        string.IsNullOrWhiteSpace(_opciones.UrlExplorador) ? null : $"{_opciones.UrlExplorador.TrimEnd('/')}/{ruta}";

    public async Task<string> ObtenerDireccionContratoAsync()
    {
        if (!string.IsNullOrWhiteSpace(_opciones.DireccionContrato))
        {
            return _opciones.DireccionContrato;
        }

        if (!_opciones.PublicarContratoAutomaticamente)
        {
            throw new ErrorDeRedException("Falta configurar la dirección del contrato (Red:DireccionContrato).");
        }

        await _candadoPublicacion.WaitAsync();
        try
        {
            if (_direccionPublicada == null && _archivoDireccion != null && File.Exists(_archivoDireccion))
            {
                _direccionPublicada = File.ReadAllText(_archivoDireccion).Trim();
            }

            // Si se reinició la red local, el contrato anterior ya no existe
            if (_direccionPublicada != null && await TieneCodigoAsync(_direccionPublicada))
            {
                return _direccionPublicada;
            }

            var web3 = CrearWeb3(_publicador);
            var recibo = await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(
                _abi, _bytecode, _publicador.Direccion, new HexBigInteger(3_000_000));

            _direccionPublicada = recibo.ContractAddress;
            if (_archivoDireccion != null)
            {
                File.WriteAllText(_archivoDireccion, _direccionPublicada);
            }
            return _direccionPublicada;
        }
        catch (Exception ex) when (ex is not ErrorDeRedException)
        {
            throw new ErrorDeRedException(DescribirError(ex), ex);
        }
        finally
        {
            _candadoPublicacion.Release();
        }
    }

    public async Task<ResultadoOperacion> CrearObraAsync(Participante cliente, string descripcion, string direccionProveedor,
        string direccionAprobador, IReadOnlyList<NuevaEtapa> etapas)
    {
        var funcion = new CrearObraFuncion
        {
            Descripcion = descripcion,
            Proveedor = direccionProveedor,
            Aprobador = direccionAprobador,
            DescripcionesEtapas = etapas.Select(e => e.Descripcion).ToList(),
            MontosEtapas = etapas.Select(e => e.MontoWei).ToList(),
            AmountToSend = etapas.Aggregate(BigInteger.Zero, (suma, e) => suma + e.MontoWei)
        };

        return await EnviarAsync(cliente, funcion, recibo =>
        {
            var evento = recibo.DecodeAllEvents<ObraCreadaEvento>().FirstOrDefault();
            return evento == null ? null : (int)evento.Event.ObraId;
        });
    }

    public Task<ResultadoOperacion> MarcarTerminadaAsync(Participante proveedor, int obraId, int numeroEtapa) =>
        EnviarAsync(proveedor, new MarcarTerminadaFuncion { ObraId = obraId, NumeroEtapa = numeroEtapa });

    public Task<ResultadoOperacion> AprobarAsync(Participante aprobador, int obraId, int numeroEtapa) =>
        EnviarAsync(aprobador, new AprobarFuncion { ObraId = obraId, NumeroEtapa = numeroEtapa });

    public Task<ResultadoOperacion> RechazarAsync(Participante aprobador, int obraId, int numeroEtapa) =>
        EnviarAsync(aprobador, new RechazarFuncion { ObraId = obraId, NumeroEtapa = numeroEtapa });

    private async Task<ResultadoOperacion> EnviarAsync<TFuncion>(Participante quien, TFuncion funcion,
        Func<Nethereum.RPC.Eth.DTOs.TransactionReceipt, int?>? leerObraId = null)
        where TFuncion : FunctionMessage, new()
    {
        try
        {
            string direccionContrato = await ObtenerDireccionContratoAsync();
            var web3 = CrearWeb3(quien);
            var manejador = web3.Eth.GetContractTransactionHandler<TFuncion>();

            using var cancelacion = new CancellationTokenSource(TiempoMaximoConfirmacion);
            var recibo = await manejador.SendRequestAndWaitForReceiptAsync(direccionContrato, funcion, cancelacion.Token);

            if (recibo.Status.Value != BigInteger.One)
            {
                return ResultadoOperacion.Fallida("La red rechazó la transacción.");
            }

            return ResultadoOperacion.Exitosa(recibo.TransactionHash, leerObraId?.Invoke(recibo));
        }
        catch (OperationCanceledException)
        {
            return ResultadoOperacion.Fallida($"La red no confirmó la transacción en {TiempoMaximoConfirmacion.TotalSeconds} segundos.");
        }
        catch (ErrorDeRedException ex)
        {
            return ResultadoOperacion.Fallida(ex.Message);
        }
        catch (Exception ex)
        {
            return ResultadoOperacion.Fallida(DescribirError(ex));
        }
    }

    public async Task<IReadOnlyList<Obra>> ListarObrasAsync()
    {
        try
        {
            string direccionContrato = await ObtenerDireccionContratoAsync();
            var web3 = new Web3(_opciones.UrlNodo);

            BigInteger cantidad = await web3.Eth.GetContractQueryHandler<CantidadObrasFuncion>()
                .QueryAsync<BigInteger>(direccionContrato, new CantidadObrasFuncion());

            var obras = new List<Obra>();
            for (int id = 0; id < (int)cantidad; id++)
            {
                obras.Add(await LeerObraAsync(web3, direccionContrato, id));
            }
            return obras;
        }
        catch (Exception ex) when (ex is not ErrorDeRedException)
        {
            throw new ErrorDeRedException(DescribirError(ex), ex);
        }
    }

    public async Task<Obra?> ObtenerObraAsync(int obraId)
    {
        try
        {
            string direccionContrato = await ObtenerDireccionContratoAsync();
            var web3 = new Web3(_opciones.UrlNodo);

            BigInteger cantidad = await web3.Eth.GetContractQueryHandler<CantidadObrasFuncion>()
                .QueryAsync<BigInteger>(direccionContrato, new CantidadObrasFuncion());

            if (obraId < 0 || obraId >= cantidad)
            {
                return null;
            }
            return await LeerObraAsync(web3, direccionContrato, obraId);
        }
        catch (Exception ex) when (ex is not ErrorDeRedException)
        {
            throw new ErrorDeRedException(DescribirError(ex), ex);
        }
    }

    public async Task<BigInteger> ObtenerSaldoAsync(string direccion)
    {
        try
        {
            var web3 = new Web3(_opciones.UrlNodo);
            HexBigInteger saldo = await web3.Eth.GetBalance.SendRequestAsync(direccion);
            return saldo.Value;
        }
        catch (Exception ex)
        {
            throw new ErrorDeRedException(DescribirError(ex), ex);
        }
    }

    private static async Task<Obra> LeerObraAsync(Web3 web3, string direccionContrato, int obraId)
    {
        ConsultarObraResultado datos = await web3.Eth.GetContractQueryHandler<ConsultarObraFuncion>()
            .QueryDeserializingToObjectAsync<ConsultarObraResultado>(new ConsultarObraFuncion { ObraId = obraId }, direccionContrato);

        var consultaEtapa = web3.Eth.GetContractQueryHandler<ConsultarEtapaFuncion>();
        var etapas = new List<Etapa>();
        for (int numero = 0; numero < (int)datos.CantidadEtapas; numero++)
        {
            ConsultarEtapaResultado etapa = await consultaEtapa.QueryDeserializingToObjectAsync<ConsultarEtapaResultado>(
                new ConsultarEtapaFuncion { ObraId = obraId, NumeroEtapa = numero }, direccionContrato);

            etapas.Add(new Etapa(numero, etapa.Descripcion, etapa.Monto, (EstadoEtapa)etapa.Estado));
        }

        return new Obra(obraId, datos.Descripcion, datos.Cliente, datos.Proveedor, datos.Aprobador, datos.SaldoRetenido, etapas);
    }

    private Web3 CrearWeb3(Participante quien) =>
        new(new Account(quien.ClavePrivada, _opciones.ChainId), _opciones.UrlNodo);

    private async Task<bool> TieneCodigoAsync(string direccion)
    {
        string codigo = await new Web3(_opciones.UrlNodo).Eth.GetCode.SendRequestAsync(direccion);
        return !string.IsNullOrEmpty(codigo) && codigo != "0x";
    }

    private static string DescribirError(Exception ex)
    {
        if (ex is SmartContractRevertException rechazo)
        {
            return $"El contrato rechazó la operación: {rechazo.RevertMessage}";
        }

        if (ex.Message.Contains("insufficient funds", StringComparison.OrdinalIgnoreCase))
        {
            return "La billetera no tiene saldo suficiente para el depósito y el gas.";
        }

        if (ex is RpcClientUnknownException or RpcClientTimeoutException or HttpRequestException
            || ex.InnerException is HttpRequestException)
        {
            return "No se pudo conectar con la red. Si estás usando la red local, ¿está corriendo? (npm start en la carpeta RedLocal).";
        }

        return ex.Message;
    }
}
