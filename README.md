# Pago de una obra por etapas con un contrato inteligente

**Universidad Nacional de La Matanza (UNLaM)** — Escuela de Formación Continua
Licenciatura en Gestión de la Tecnología — Programación Avanzada II
TP de investigación, tema 10: **Block Chain con .NET**

> 🚧 **Estado: avance.** Funcionan el contrato inteligente y la app web completa contra una blockchain
> Ethereum local de práctica. Lo que falta está en [Pendiente](#pendiente).

## La idea

Contratás a una empresa para remodelar tu cocina por USD 5.000. En lugar de adelantarle todo el dinero:

1. **Se deposita:** los USD 5.000 quedan bloqueados en un contrato inteligente.
2. **Se completa una etapa:** la empresa termina la instalación eléctrica y avisa.
3. **Se verifica:** el cliente o un inspector (el que acordaron) confirma que se cumplió lo pactado.
4. **Se libera el pago:** el contrato le transfiere automáticamente USD 1.500 a la empresa por esa etapa.

| | Qué gana |
|---|---|
| **Cliente** | No arriesga todo su dinero por adelantado. |
| **Empresa** | Sabe que los fondos están reservados y que va a cobrar cuando se verifique el trabajo. |
| **Los dos** | No dependen de un intermediario que custodie la plata y decida cuándo liberarla. |

### Por qué tiene sentido usar blockchain acá

Cliente y empresa **no se tienen confianza** y necesitan que alguien guarde la plata. Sin blockchain, ese alguien
es un intermediario (un banco, una escribanía, una plataforma) que cobra comisión y que podría equivocarse o
quedarse con el dinero. Con un contrato inteligente, la plata la guarda un programa cuyas reglas **nadie puede
cambiar**, ni el cliente, ni la empresa, ni quienes hicieron la app.

### El desafío: ¿cómo sabe la blockchain que la etapa está terminada?

**No lo sabe.** La blockchain no puede ver la cocina. Por eso, al crear la obra se acuerda **quién aprueba**
cada etapa (el cliente o un inspector independiente), y el contrato **solo acepta la aprobación de esa cuenta**.
Si cualquier otra persona intenta aprobar, incluida la propia empresa, el contrato lo rechaza.

Este problema tiene nombre: es el **problema del oráculo** (cómo meter en la blockchain información del mundo real).

## Cómo ejecutarlo

Requiere el **SDK de .NET 8** y **Node.js** (para la blockchain de práctica).

**1. Levantar la blockchain de práctica** (la primera vez, `npm install`):

```bash
cd RedLocal
npm install
npm start
```

Dejá esa terminal abierta. Es una red Ethereum que corre en tu compu (Ganache), con 10 cuentas de práctica.

**2. Ejecutar la app** (en otra terminal, desde la carpeta del proyecto):

```bash
dotnet run --launch-profile http
```

y abrir `http://localhost:5146`. También se puede abrir `PagoPorEtapas.sln` con Visual Studio o Rider y ejecutar con F5.

La primera vez, la app publica el contrato sola en la red local. Si reiniciás la red, se borra todo (obras y saldos)
y la app vuelve a publicar el contrato.

## Cómo probarlo

En la barra de arriba elegís **como quién estás actuando**. En la vida real cada uno usaría su propia billetera
(por ejemplo MetaMask); para la demo, la app tiene las tres cuentas de práctica:

| Participante | Rol |
|---|---|
| Laura (clienta) | Crea la obra y deposita el dinero. Puede ser la aprobadora. |
| Remodelaciones Sur SRL | Hace la obra y avisa cuando termina cada etapa. |
| Ing. Pérez (inspector) | Revisa y aprueba (o rechaza) las etapas, si se lo elige como aprobador. |

**Guion para la demo:**

1. Como **Laura**, crear la obra del ejemplo (cocina, USD 5.000 en 3 etapas). La caja fuerte del contrato pasa a USD 5.000.
2. Como **la empresa**, avisar que terminó la instalación eléctrica.
3. Como **Laura** (que no es la aprobadora): no aparece el botón de aprobar. El contrato tampoco se lo permitiría.
4. Como **el inspector**, aprobar la etapa: la empresa cobra USD 1.500 y la caja fuerte baja a USD 3.500.
5. Como **la empresa**, avisar que terminó la plomería. Como **el inspector**, rechazarla: vuelve a "en curso" y la plata sigue retenida.

## El contrato inteligente

Está en [Contratos/PagoPorEtapas.sol](Contratos/PagoPorEtapas.sol) (lenguaje Solidity). Sus reglas:

| Función | Quién puede | Qué hace |
|---|---|---|
| `crearObra` | Cualquiera (pasa a ser el cliente) | Crea la obra con sus etapas y retiene el depósito, que tiene que ser igual a la suma de las etapas. |
| `marcarTerminada` | Solo la empresa de esa obra | Pasa la etapa de "en curso" a "terminada". |
| `aprobar` | Solo el aprobador de esa obra | Pasa la etapa a "aprobada" y le transfiere su monto a la empresa. |
| `rechazar` | Solo el aprobador de esa obra | Devuelve la etapa a "en curso". |
| `consultarObra`, `consultarEtapa`, `cantidadObras` | Cualquiera, gratis | Leen el estado de las obras. |

Además, el contrato no deja que la empresa sea su propia aprobadora, ni que el cliente sea la empresa,
ni que se apruebe dos veces la misma etapa.

**Sobre los montos:** en la red de práctica se usa ETH de prueba, así que la app muestra los montos en dólares con
una cotización fija y ficticia (`Red:UsdPorEth`). En la vida real se usaría una *stablecoin* (como USDC), una
criptomoneda que siempre vale 1 dólar.

Para recompilar el contrato después de modificarlo: `npm run compilar` dentro de `RedLocal`.

## Estructura del proyecto

```
Contratos/
  PagoPorEtapas.sol             El contrato inteligente
  compilado/                    El contrato compilado (lo usa la app para publicarlo)
RedLocal/                       Blockchain de práctica (Ganache) y compilador (Node.js)
Dominio/                        ← Lógica, sin dependencias de la web ni de Ethereum
  Obra.cs                       Obra, Etapa y EstadoEtapa
  Participante.cs               Una persona o empresa con su billetera
  Cotizacion.cs                 Conversión entre dólares y wei
  IContratoObras.cs             Lo que la app puede hacer con el contrato (interfaz)
Datos/Ethereum/
  ContratoObrasEthereum.cs      Implementación con la librería Nethereum
  MensajesContrato.cs           Descripción de las funciones del contrato para Nethereum
  FabricaParticipantes.cs       Arma los participantes a partir de la configuración
  OpcionesRed.cs                Configuración de la red
Servicios/
  ServicioParticipantes.cs      Quiénes participan y como quién actúa cada navegador (sesión)
Filtros/
  ErrorDeRedFiltro.cs           Pantalla clara cuando no hay conexión con la blockchain
Controllers/                    Home, Obras, Participantes
Models/                         ViewModels con DataAnnotations
Views/                          Vistas Razor + vistas parciales (_Billeteras, _EstadoEtapa, _Mensaje)
```

## Contenidos de la materia aplicados

| Tema | Dónde se ve |
|---|---|
| **POO – Encapsulamiento** | Las clases del dominio (`Obra`, `Etapa`, `Participante`) solo tienen propiedades de lectura: se crean completas y no se modifican |
| **SOLID – SRP** | Cada clase tiene una sola tarea: `ContratoObrasEthereum` habla con la red, `ServicioParticipantes` maneja quién actúa, `Cotizacion` convierte montos |
| **SOLID – DIP** | `ObrasController` depende de la interfaz `IContratoObras`, no de Ethereum ni de Nethereum |
| **ASP.NET Core MVC** | Controladores, ruteo convencional, model binding (incluida una lista de etapas), acciones `async`, filtro de excepciones global |
| **Validaciones** | `[Required]`, `[StringLength]`, `[Range]` + validación propia con `IValidatableObject` (al menos una etapa, la empresa no puede aprobar) |
| **Layout y vistas parciales** | Selector de participante en el layout; `_Billeteras`, `_EstadoEtapa` y `_Mensaje` reutilizadas |
| **Pasaje de datos** | `TempData` + POST-Redirect-GET para los avisos; `Session` para recordar como quién actúa cada navegador |

## Estado del avance

### Hecho

- [x] Contrato inteligente con las reglas de crear, marcar terminada, aprobar (con pago automático) y rechazar
- [x] App web: lista de obras, creación con etapas, detalle con acciones según el participante, billeteras en vivo
- [x] Probado contra la red local: el flujo completo y los intentos de trampa (aprobar sin ser el aprobador,
      la empresa como su propia aprobadora, aprobar dos veces, marcar siendo otro), reinicio de la app y red apagada

### Pendiente

- [ ] **Plazos y devolución:** si la empresa abandona la obra, hoy la plata de las etapas no aprobadas queda retenida
      para siempre. Falta agregar una fecha límite a partir de la cual el cliente pueda recuperarla.
- [ ] Publicar el contrato en **Sepolia** (la red de prueba pública de Ethereum), para que cualquiera pueda
      verlo en Etherscan.
