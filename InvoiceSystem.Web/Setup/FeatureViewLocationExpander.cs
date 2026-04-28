using Microsoft.AspNetCore.Mvc.Razor;

namespace InvoiceSystem.Web.Setup;

public class FeatureViewLocationExpander : IViewLocationExpander
{
    public void PopulateValues(ViewLocationExpanderContext context)
    {
    }

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        var controllerActionDescriptor = context.ActionContext.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
        if (controllerActionDescriptor != null)
        {
            // Wyciągamy namespace, np. InvoiceSystem.Web.Features.Invoices.GetInvoiceList
            var @namespace = controllerActionDescriptor.ControllerTypeInfo.Namespace;
            if (@namespace != null && @namespace.Contains(".Features."))
            {
                // Wycinamy część po ".Features."
                var featurePath = @namespace.Substring(@namespace.IndexOf(".Features.") + 10).Replace('.', '/');
                yield return $"/Features/{featurePath}/{{0}}.cshtml";
                yield return $"/Features/{featurePath}/{{1}}.cshtml";
            }
        }

        yield return "/Shared/{0}.cshtml";

        foreach (var location in viewLocations)
        {
            yield return location;
        }
    }
}
