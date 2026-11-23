namespace ByteForge.Toolkit.Security.Rbac;

/// <summary>Renders the shared handoff page with the application's own anti-forgery field.</summary>
public static class TelcoPortalHandoffPage
{
    /// <summary>Inserts only the MVC application-generated anti-forgery field.</summary>
    public static string Render(string antiForgeryHtml)
    {
        using var stream=typeof(TelcoPortalHandoffPage).Assembly.GetManifestResourceStream("ByteForge.Toolkit.HTML.Rbac.portal-sign-in.html")
            ?? throw new InvalidOperationException("The shared portal sign-in page is missing.");
        using var reader=new System.IO.StreamReader(stream);
        return reader.ReadToEnd().Replace("{{AntiForgery}}",antiForgeryHtml);
    }
}
