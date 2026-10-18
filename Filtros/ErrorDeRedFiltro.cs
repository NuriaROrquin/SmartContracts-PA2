using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using PagoPorEtapas.Dominio;

namespace PagoPorEtapas.Filtros;

public class ErrorDeRedFiltro : IExceptionFilter
{
    private readonly IModelMetadataProvider _metadatos;

    public ErrorDeRedFiltro(IModelMetadataProvider metadatos)
    {
        _metadatos = metadatos;
    }

    public void OnException(ExceptionContext contexto)
    {
        if (contexto.Exception is not ErrorDeRedException error)
        {
            return;
        }

        contexto.Result = new ViewResult
        {
            ViewName = "ErrorRed",
            ViewData = new ViewDataDictionary(_metadatos, contexto.ModelState) { Model = error.Message }
        };
        contexto.ExceptionHandled = true;
    }
}
