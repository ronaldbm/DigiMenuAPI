namespace AppCore.Application.Common
{
    public class Constants
    {
        public enum Gender
        {
            Masculine,
            Feminine
        }

        public class EntityInfo
        {
            public string Name { get; }
            public Gender Gender { get; }

            public EntityInfo(string name, Gender gender)
            {
                Name = name;
                Gender = gender;
            }
        }

        public static class EntityNames
        {
            public static readonly EntityInfo Product = new("producto", Gender.Masculine);
            public static readonly EntityInfo Category = new("categoría", Gender.Feminine);
            public static readonly EntityInfo Tag = new("etiqueta", Gender.Feminine);
            public static readonly EntityInfo Setting = new("configuración", Gender.Feminine);
            public static readonly EntityInfo FooterLink = new("enlace", Gender.Masculine);
            public static readonly EntityInfo StandardIcon = new("icono", Gender.Masculine);
            public static readonly EntityInfo Reservation = new("reserva", Gender.Feminine);
            public static readonly EntityInfo StoreMenu = new("menú", Gender.Masculine);
        }
    }
}
