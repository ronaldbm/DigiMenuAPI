using System.Text.RegularExpressions;

namespace AppCore.Application.Utils
{
    /// <summary>
    /// Validación centralizada de complejidad de contraseñas.
    ///
    /// Reglas aplicadas:
    ///   - Mínimo 8 caracteres
    ///   - Al menos 1 letra mayúscula
    ///   - Al menos 1 número
    ///
    /// Usado en:
    ///   - AuthService.RegisterCompany  → contraseña del primer admin
    ///   - AuthService.RegisterUser     → contraseña del usuario creado por admin
    ///   - AuthService.ChangePassword   → nueva contraseña al cambiar
    /// </summary>
    public static class PasswordValidator
    {
        private static readonly Regex HasUppercase = new(@"[A-Z]", RegexOptions.Compiled);
        private static readonly Regex HasNumber = new(@"[0-9]", RegexOptions.Compiled);

        /// <summary>
        /// Valida la complejidad de una contraseña.
        /// Devuelve null si es válida, o el mensaje de error si no cumple.
        /// </summary>
        public static string? Validate(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return "La contraseña no puede estar vacía.";

            if (password.Length < 8)
                return "La contraseña debe tener al menos 8 caracteres.";

            if (!HasUppercase.IsMatch(password))
                return "La contraseña debe contener al menos una letra mayúscula.";

            if (!HasNumber.IsMatch(password))
                return "La contraseña debe contener al menos un número.";

            return null;
        }

        /// <summary>
        /// Genera una contraseña temporal segura que cumple las reglas de complejidad.
        /// Formato: 4 letras minúsculas + 2 mayúsculas + 2 números = 8 caracteres.
        /// Ejemplo: "xkBmR7Aq"
        /// </summary>
        public static string GenerateTemporary()
        {
            const string lower = "abcdefghijkmnpqrstuvwxyz";
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string numbers = "23456789";

            var rng = Random.Shared;
            var chars = new char[8];

            // Garantizar al menos 2 mayúsculas y 2 números
            chars[0] = upper[rng.Next(upper.Length)];
            chars[1] = upper[rng.Next(upper.Length)];
            chars[2] = numbers[rng.Next(numbers.Length)];
            chars[3] = numbers[rng.Next(numbers.Length)];

            // Rellenar el resto con minúsculas
            for (int i = 4; i < 8; i++)
                chars[i] = lower[rng.Next(lower.Length)];

            // Mezclar para que no sea predecible el patrón
            return new string(chars.OrderBy(_ => rng.Next()).ToArray());
        }
    }
}
