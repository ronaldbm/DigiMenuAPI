using DigiMenuAPI.Infrastructure.Entities;

namespace DigiMenuAPI.UnitTests.TestInfrastructure.Builders;

/// <summary>Builder fluido para Product (catálogo global de la Company).</summary>
public sealed class ProductBuilder
{
    private int    _id         = 1;
    private int    _companyId  = 1;
    private int    _categoryId = 1;
    private bool   _isDeleted  = false;
    private readonly List<(string LangCode, string Name, string? ShortDesc)> _translations = [];

    public ProductBuilder WithId(int id)           { _id = id;           return this; }
    public ProductBuilder WithCompanyId(int id)    { _companyId = id;    return this; }
    public ProductBuilder WithCategoryId(int id)   { _categoryId = id;   return this; }
    public ProductBuilder Deleted()                { _isDeleted = true;  return this; }
    public ProductBuilder WithTranslation(string lang, string name, string? shortDesc = null)
    {
        _translations.Add((lang, name, shortDesc));
        return this;
    }

    public Product Build()
    {
        var translations = _translations.Count > 0
            ? _translations
            : [("es", $"Producto {_id}", (string?)null)];

        return new Product
        {
            Id         = _id,
            CompanyId  = _companyId,
            CategoryId = _categoryId,
            IsDeleted  = _isDeleted,
            Translations = translations
                .Select(t => new ProductTranslation
                {
                    LanguageCode     = t.LangCode,
                    Name             = t.Name,
                    ShortDescription = t.ShortDesc,
                })
                .ToList(),
        };
    }
}
