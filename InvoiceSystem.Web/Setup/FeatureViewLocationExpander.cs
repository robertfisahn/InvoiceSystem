using Microsoft.AspNetCore.Mvc.Razor;

namespace InvoiceSystem.Web.Setup;

public class FeatureViewLocationExpander : IViewLocationExpander
{
    public void PopulateValues(ViewLocationExpanderContext context)
    {
    }

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        // {0} - Action Name
        // {1} - Controller Name
        // {2} - Area Name (not used here)

        return new[]
        {
            "/Features/{1}/{0}.cshtml",
            "/Features/{1}/{1}.cshtml", // Common pattern for single-action controllers
            "/Shared/{0}.cshtml"
        };
    }
}
