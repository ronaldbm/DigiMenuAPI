using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AppCore.Application.Utils
{
    /// <summary>
    /// Utilidades para generación y normalización de slugs.
    ///
    /// Un slug es un identificador legible para URLs:
    ///   "Sucursal Centro Norte" → "sucursal-centro-norte"
    ///
    /// Reglas aplicadas:
    ///   - Minúsculas
    ///   - Acentos y diacríticos eliminados (é → e, ñ → n, ü → u)
    ///   - Caracteres no alfanuméricos reemplazados por guión
    ///   - Guiones múltiples colapsados en uno solo
    ///   - Guiones al inicio o final eliminados
    ///   - Longitud máxima configurable (default 60)
    /// </summary>
    public static class SlugHelper
    {
        /// <summary>
        /// Convierte un texto libre en un slug normalizado.
        /// Ejemplo: "Sucursal Centro #1" → "sucursal-centro-1"
        /// </summary>
        public static string Slugify(string text, int maxLength = 60)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "branch";

            // 1. Normalizar Unicode para separar caracteres base de diacríticos
            var normalized = text.Normalize(NormalizationForm.FormD);

            // 2. Eliminar diacríticos (acentos, tildes, etc.)
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            // 3. Normalizar de vuelta a FormC y convertir a minúsculas
            var clean = sb.ToString()
                .Normalize(NormalizationForm.FormC)
                .ToLowerInvariant();

            // 4. Reemplazar ñ → n (caso especial del español que no cubre el paso anterior)
            clean = clean.Replace('ñ', 'n').Replace('ü', 'u');

            // 5. Reemplazar todo lo que no sea letra o dígito por guión
            clean = Regex.Replace(clean, @"[^a-z0-9]", "-");

            // 6. Colapsar guiones múltiples consecutivos en uno solo
            clean = Regex.Replace(clean, @"-{2,}", "-");

            // 7. Eliminar guiones al inicio y al final
            clean = clean.Trim('-');

            // 8. Truncar respetando el límite de caracteres sin cortar en medio de palabra
            if (clean.Length > maxLength)
            {
                clean = clean[..maxLength].TrimEnd('-');
            }

            // 9. Si quedó vacío después del proceso, usar fallback
            return string.IsNullOrEmpty(clean) ? "branch" : clean;
        }

        /// <summary>
        /// Genera un slug único dentro de una Company verificando contra los slugs
        /// existentes proporcionados.
        ///
        /// Si el slug base ya existe, agrega sufijo numérico incremental:
        ///   "centro" → "centro-2" → "centro-3" → ...
        ///
        /// Ejemplo de uso:
        ///   var existingSlugs = await _context.Branches
        ///       .Where(b => b.CompanyId == companyId)
        ///       .Select(b => b.Slug)
        ///       .ToListAsync();
        ///   var slug = SlugHelper.GenerateUnique("Sucursal Centro", existingSlugs);
        /// </summary>
        /// <param name="name">Nombre de la Branch a convertir en slug.</param>
        /// <param name="existingSlugs">Slugs ya usados dentro de la misma Company.</param>
        /// <param name="maxLength">Longitud máxima del slug base (default 60).</param>
        public static string GenerateUnique(
            string name,
            IEnumerable<string> existingSlugs,
            int maxLength = 60)
        {
            var baseSlug = Slugify(name, maxLength);
            var slugSet = new HashSet<string>(existingSlugs, StringComparer.OrdinalIgnoreCase);

            // Si el slug base no existe, usarlo directamente
            if (!slugSet.Contains(baseSlug))
                return baseSlug;

            // Buscar el primer sufijo numérico disponible desde 2
            // Truncar el base para dejar espacio al sufijo "-NNN"
            var truncatedBase = baseSlug.Length > 55
                ? baseSlug[..55].TrimEnd('-')
                : baseSlug;

            var counter = 2;
            string candidate;

            do
            {
                candidate = $"{truncatedBase}-{counter}";
                counter++;
            }
            while (slugSet.Contains(candidate));

            return candidate;
        }
    }
}
