namespace DigiMenuAPI.Application.Common
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
            public static readonly EntityInfo Subcategory = new("subcategoría", Gender.Feminine);
            public static readonly EntityInfo SocialLink = new("link", Gender.Masculine);
        }
    }
}
