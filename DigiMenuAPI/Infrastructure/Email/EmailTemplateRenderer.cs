namespace DigiMenuAPI.Infrastructure.Email
{
    /// <summary>
    /// Carga plantillas HTML desde el sistema de archivos y reemplaza
    /// variables con el formato {{VARIABLE}}.
    ///
    /// Las plantillas viven en Infrastructure/Email/Templates/ y se
    /// copian al directorio de salida en la compilación.
    /// Configurar en .csproj:
    ///   &lt;Content Include="Infrastructure/Email/Templates/**"&gt;
    ///     &lt;CopyToOutputDirectory&gt;Always&lt;/CopyToOutputDirectory&gt;
    ///   &lt;/Content&gt;
    /// </summary>
    public static class EmailTemplateRenderer
    {
        private static readonly string TemplatesPath = Path.Combine(
            AppContext.BaseDirectory,
            "Infrastructure", "Email", "Templates");

        /// <summary>
        /// Carga el template por nombre y reemplaza todas las variables.
        /// </summary>
        /// <param name="templateName">Nombre sin extensión. Ej: "welcome"</param>
        /// <param name="variables">Diccionario de {{VARIABLE}} → valor</param>
        public static async Task<string> RenderAsync(
            string templateName,
            Dictionary<string, string> variables)
        {
            var path = Path.Combine(TemplatesPath, $"{templateName}.html");

            if (!File.Exists(path))
                throw new FileNotFoundException(
                    $"Template de email no encontrado: {templateName}.html", path);

            var html = await File.ReadAllTextAsync(path);

            foreach (var (placeholder, value) in variables)
                html = html.Replace(placeholder, value ?? string.Empty);

            return html;
        }
    }
}