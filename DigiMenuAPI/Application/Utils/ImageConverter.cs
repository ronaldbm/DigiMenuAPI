using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using System;
using System.IO;

public class ImageConverter
{
    public static string ConvertBase64ToWebP(string? base64Image)
    {
        if (base64Image is null)
        {
            return "";
        }

        // Limpiar la cadena base64 eliminando saltos de línea y espacios
        base64Image = base64Image.Replace("\n", "").Replace("\r", "").Trim();

        // Verificar que la cadena base64 tiene el formato adecuado
        if (base64Image.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
        {
            // Eliminar el prefijo "data:image/jpeg;base64," o similar
            var index = base64Image.IndexOf("base64,", StringComparison.Ordinal);
            if (index > 0)
            {
                base64Image = base64Image.Substring(index + 7);
            }
        }

        // Agregar el encabezado MIME para WebP
        return $"data:image/webp;base64,{base64Image}";
    }

}