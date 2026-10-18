// SPDX-License-Identifier: MIT
pragma solidity ^0.8.20;

/// @title Pago de una obra por etapas
contract PagoPorEtapas {

    enum EstadoEtapa {
        EnCurso,
        Terminada,
        Aprobada
    }

    struct Etapa {
        string descripcion;
        uint256 monto;
        EstadoEtapa estado;
    }

    struct Obra {
        string descripcion;
        address cliente;
        address proveedor;
        address aprobador;
        uint256 saldoRetenido;
        Etapa[] etapas;
    }

    uint256 public constant MAXIMO_ETAPAS = 10;

    Obra[] private obras;

    event ObraCreada(uint256 indexed obraId, address indexed cliente, address indexed proveedor, uint256 total);
    event EtapaTerminada(uint256 indexed obraId, uint256 etapa);
    event EtapaAprobada(uint256 indexed obraId, uint256 etapa, uint256 monto);
    event EtapaRechazada(uint256 indexed obraId, uint256 etapa);

    function crearObra(
        string calldata descripcion,
        address proveedor,
        address aprobador,
        string[] calldata descripcionesEtapas,
        uint256[] calldata montosEtapas
    ) external payable returns (uint256 obraId) {
        require(proveedor != address(0) && aprobador != address(0), "Falta el proveedor o el aprobador");
        require(proveedor != msg.sender, "El cliente no puede ser el proveedor");
        require(aprobador != proveedor, "El proveedor no puede aprobar su propio trabajo");
        require(descripcionesEtapas.length > 0 && descripcionesEtapas.length <= MAXIMO_ETAPAS, "Cantidad de etapas invalida");
        require(descripcionesEtapas.length == montosEtapas.length, "Cada etapa necesita un monto");

        obraId = obras.length;
        Obra storage obra = obras.push();
        obra.descripcion = descripcion;
        obra.cliente = msg.sender;
        obra.proveedor = proveedor;
        obra.aprobador = aprobador;

        uint256 total = 0;
        for (uint256 i = 0; i < montosEtapas.length; i++) {
            require(montosEtapas[i] > 0, "Cada etapa debe tener un monto mayor a cero");
            obra.etapas.push(Etapa(descripcionesEtapas[i], montosEtapas[i], EstadoEtapa.EnCurso));
            total += montosEtapas[i];
        }

        require(msg.value == total, "El deposito debe ser igual a la suma de las etapas");
        obra.saldoRetenido = total;

        emit ObraCreada(obraId, msg.sender, proveedor, total);
    }

    function marcarTerminada(uint256 obraId, uint256 numeroEtapa) external {
        Obra storage obra = obtenerObra(obraId);
        require(msg.sender == obra.proveedor, "Solo el proveedor puede avisar que termino una etapa");

        Etapa storage etapa = obtenerEtapa(obra, numeroEtapa);
        require(etapa.estado != EstadoEtapa.Aprobada, "Esa etapa ya fue aprobada y pagada");
        require(etapa.estado == EstadoEtapa.EnCurso, "Esa etapa ya esta marcada como terminada");

        etapa.estado = EstadoEtapa.Terminada;
        emit EtapaTerminada(obraId, numeroEtapa);
    }

    function aprobar(uint256 obraId, uint256 numeroEtapa) external {
        Obra storage obra = obtenerObra(obraId);
        require(msg.sender == obra.aprobador, "Solo el aprobador acordado puede aprobar");

        Etapa storage etapa = obtenerEtapa(obra, numeroEtapa);
        require(etapa.estado != EstadoEtapa.Aprobada, "Esa etapa ya fue aprobada y pagada");
        require(etapa.estado == EstadoEtapa.Terminada, "El proveedor todavia no marco la etapa como terminada");

        // Se actualiza el estado antes de transferir para evitar reentrada
        etapa.estado = EstadoEtapa.Aprobada;
        obra.saldoRetenido -= etapa.monto;

        (bool transferido, ) = payable(obra.proveedor).call{value: etapa.monto}("");
        require(transferido, "No se pudo transferir el pago");

        emit EtapaAprobada(obraId, numeroEtapa, etapa.monto);
    }

    function rechazar(uint256 obraId, uint256 numeroEtapa) external {
        Obra storage obra = obtenerObra(obraId);
        require(msg.sender == obra.aprobador, "Solo el aprobador acordado puede rechazar");

        Etapa storage etapa = obtenerEtapa(obra, numeroEtapa);
        require(etapa.estado == EstadoEtapa.Terminada, "Solo se puede rechazar una etapa marcada como terminada");

        etapa.estado = EstadoEtapa.EnCurso;
        emit EtapaRechazada(obraId, numeroEtapa);
    }

    function cantidadObras() external view returns (uint256) {
        return obras.length;
    }

    function consultarObra(uint256 obraId)
        external
        view
        returns (
            string memory descripcion,
            address cliente,
            address proveedor,
            address aprobador,
            uint256 saldoRetenido,
            uint256 cantidadEtapas
        )
    {
        Obra storage obra = obtenerObra(obraId);
        return (obra.descripcion, obra.cliente, obra.proveedor, obra.aprobador, obra.saldoRetenido, obra.etapas.length);
    }

    function consultarEtapa(uint256 obraId, uint256 numeroEtapa)
        external
        view
        returns (string memory descripcion, uint256 monto, EstadoEtapa estado)
    {
        Etapa storage etapa = obtenerEtapa(obtenerObra(obraId), numeroEtapa);
        return (etapa.descripcion, etapa.monto, etapa.estado);
    }

    function obtenerObra(uint256 obraId) private view returns (Obra storage) {
        require(obraId < obras.length, "La obra no existe");
        return obras[obraId];
    }

    function obtenerEtapa(Obra storage obra, uint256 numeroEtapa) private view returns (Etapa storage) {
        require(numeroEtapa < obra.etapas.length, "La etapa no existe");
        return obra.etapas[numeroEtapa];
    }
}
