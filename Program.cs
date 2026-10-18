using PagoPorEtapas.Datos.Ethereum;
using PagoPorEtapas.Dominio;
using PagoPorEtapas.Filtros;
using PagoPorEtapas.Servicios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(opciones =>
{
    opciones.Filters.Add<ErrorDeRedFiltro>();
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opciones =>
{
    opciones.IdleTimeout = TimeSpan.FromHours(2);
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.IsEssential = true;
});

var opcionesRed = builder.Configuration.GetSection("Red").Get<OpcionesRed>() ?? new OpcionesRed();
var participantes = FabricaParticipantes.Crear(
    builder.Configuration.GetSection("Participantes").Get<List<OpcionesParticipante>>() ?? new List<OpcionesParticipante>());

string carpetaContrato = Path.Combine(AppContext.BaseDirectory, "Contratos", "compilado");
string abi = File.ReadAllText(Path.Combine(carpetaContrato, "PagoPorEtapas.abi.json"));
string bytecode = File.ReadAllText(Path.Combine(carpetaContrato, "PagoPorEtapas.bin"));

builder.Services.AddSingleton(new Cotizacion(opcionesRed.UsdPorEth));
builder.Services.AddSingleton(new ServicioParticipantes(participantes));
builder.Services.AddSingleton<IContratoObras>(new ContratoObrasEthereum(opcionesRed, participantes[0], abi, bytecode,
    archivoDireccion: Path.Combine(AppContext.BaseDirectory, "contrato-red-local.txt")));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseRequestLocalization("es-AR");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
