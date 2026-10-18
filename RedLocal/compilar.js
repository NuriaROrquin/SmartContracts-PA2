// Compila Contratos/PagoPorEtapas.sol en Contratos/compilado/
const fs = require('fs');
const path = require('path');
const solc = require('solc');

const carpetaContratos = path.join(__dirname, '..', 'Contratos');
const fuente = fs.readFileSync(path.join(carpetaContratos, 'PagoPorEtapas.sol'), 'utf8');

const entrada = {
  language: 'Solidity',
  sources: { 'PagoPorEtapas.sol': { content: fuente } },
  settings: {
    optimizer: { enabled: true, runs: 200 },
    evmVersion: 'paris',
    outputSelection: { '*': { '*': ['abi', 'evm.bytecode.object'] } }
  }
};

const salida = JSON.parse(solc.compile(JSON.stringify(entrada)));
const errores = (salida.errors || []).filter(e => e.severity === 'error');
(salida.errors || []).forEach(e => console.log(e.formattedMessage));
if (errores.length > 0) {
  process.exit(1);
}

const contrato = salida.contracts['PagoPorEtapas.sol'].PagoPorEtapas;
const carpetaSalida = path.join(carpetaContratos, 'compilado');
fs.mkdirSync(carpetaSalida, { recursive: true });
fs.writeFileSync(path.join(carpetaSalida, 'PagoPorEtapas.abi.json'), JSON.stringify(contrato.abi, null, 2));
fs.writeFileSync(path.join(carpetaSalida, 'PagoPorEtapas.bin'), contrato.evm.bytecode.object);

const funciones = contrato.abi.filter(x => x.type === 'function').map(f => f.name);
console.log(`Compilado OK (${contrato.evm.bytecode.object.length / 2} bytes). Funciones: ${funciones.join(', ')}`);
