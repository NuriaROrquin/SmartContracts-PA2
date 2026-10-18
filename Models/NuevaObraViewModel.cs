using System.ComponentModel.DataAnnotations;

namespace PagoPorEtapas.Models;

public class NuevaObraViewModel : IValidatableObject
{
    public const int CantidadFilasEtapas = 5;

    [Required(ErrorMessage = "Describí la obra.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "La descripción debe tener entre 3 y 100 caracteres.")]
    [Display(Name = "Descripción de la obra")]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "Elegí la empresa que hace la obra.")]
    [Display(Name = "Empresa que hace la obra")]
    public string ProveedorId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Elegí quién aprueba las etapas.")]
    [Display(Name = "¿Quién aprueba cada etapa?")]
    public string AprobadorId { get; set; } = string.Empty;

    public List<EtapaViewModel> Etapas { get; set; } = new();

    public IEnumerable<EtapaViewModel> EtapasCompletas() => Etapas.Where(e => !e.EstaVacia);

    public int TotalUsd => EtapasCompletas().Sum(e => e.MontoUsd ?? 0);

    public static NuevaObraViewModel ConEjemplo(string proveedorId, string aprobadorId)
    {
        var modelo = new NuevaObraViewModel
        {
            Descripcion = "Remodelación de cocina",
            ProveedorId = proveedorId,
            AprobadorId = aprobadorId,
            Etapas = new List<EtapaViewModel>
            {
                new() { Descripcion = "Instalación eléctrica", MontoUsd = 1500 },
                new() { Descripcion = "Plomería y gas", MontoUsd = 1500 },
                new() { Descripcion = "Terminaciones y pintura", MontoUsd = 2000 }
            }
        };
        modelo.CompletarFilasVacias();
        return modelo;
    }

    public void CompletarFilasVacias()
    {
        while (Etapas.Count < CantidadFilasEtapas)
        {
            Etapas.Add(new EtapaViewModel());
        }
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!EtapasCompletas().Any())
        {
            yield return new ValidationResult("Cargá al menos una etapa.");
        }

        for (int i = 0; i < Etapas.Count; i++)
        {
            EtapaViewModel etapa = Etapas[i];
            if (etapa.EstaVacia)
            {
                continue;
            }
            if (string.IsNullOrWhiteSpace(etapa.Descripcion))
            {
                yield return new ValidationResult("Falta describir la etapa.", new[] { $"Etapas[{i}].Descripcion" });
            }
            if (etapa.MontoUsd == null)
            {
                yield return new ValidationResult("Falta el monto de la etapa.", new[] { $"Etapas[{i}].MontoUsd" });
            }
        }

        if (!string.IsNullOrEmpty(ProveedorId) && ProveedorId == AprobadorId)
        {
            yield return new ValidationResult("La empresa no puede aprobar su propio trabajo.", new[] { nameof(AprobadorId) });
        }
    }
}

public class EtapaViewModel
{
    [StringLength(60, ErrorMessage = "Máximo 60 caracteres.")]
    public string? Descripcion { get; set; }

    [Range(1, 1_000_000, ErrorMessage = "El monto debe ser entre USD 1 y USD 1.000.000.")]
    public int? MontoUsd { get; set; }

    public bool EstaVacia => string.IsNullOrWhiteSpace(Descripcion) && MontoUsd == null;
}
