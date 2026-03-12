using static AppCore.Application.Common.Constants;

namespace AppCore.Application.Utils
{
    public static class MessageBuilder
    {
        public static string NotFound(EntityInfo entity)
            => $"No se ha encontrado {Article(entity, true)} {entity.Name}.";

        public static string AlreadyExists(EntityInfo entity)
            => $"Ya existe {Article(entity, false)} {entity.Name} con el mismo nombre.";

        public static string Created(EntityInfo entity)
            => $"{Capitalize(Article(entity, true))} {entity.Name} ha sido creado{Ending(entity)} correctamente.";

        public static string Updated(EntityInfo entity)
            => $"{Capitalize(Article(entity, true))} {entity.Name} ha sido modificado{Ending(entity)} correctamente.";

        public static string UpdatedError(EntityInfo entity)
            => $"Ha ocurrido un error inesperado al modificar {Article(entity, true)} {entity.Name}.";

        public static string Deleted(EntityInfo entity)
            => $"{Capitalize(Article(entity, true))} {entity.Name} ha sido eliminado{Ending(entity)} correctamente.";

        public static string Conflict(EntityInfo entity)
            => $"Conflicto al procesar {Article(entity, true)} {entity.Name}.";

        public static string UnexpectedError(EntityInfo entity)
            => $"Ha ocurrido un error inesperado al procesar {Article(entity, true)} {entity.Name}.";

        public static string PositionInvalid(EntityInfo entity)
            => $"La posición del/la {entity.Name} es inválida.";

        public static string NoChanges(EntityInfo entity)
            => $"No se realizaron cambios sobre {Article(entity, true)} {entity.Name}.";

        // -- Helpers
        private static string Article(EntityInfo entity, bool definite)
        {
            return (entity.Gender == Gender.Masculine)
                ? (definite ? "el" : "un")
                : (definite ? "la" : "una");
        }

        private static string Ending(EntityInfo entity)
        {
            return (entity.Gender == Gender.Masculine) ? "" : "a";
        }

        private static string Capitalize(string value)
        {
            return string.IsNullOrEmpty(value)
                ? value
                : char.ToUpper(value[0]) + value.Substring(1);
        }
    }
}
