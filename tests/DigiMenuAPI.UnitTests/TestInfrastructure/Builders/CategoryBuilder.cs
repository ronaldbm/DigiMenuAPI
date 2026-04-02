using DigiMenuAPI.Infrastructure.Entities;

namespace DigiMenuAPI.UnitTests.TestInfrastructure.Builders;

/// <summary>Builder fluido para Category con soporte multi-idioma.</summary>
public sealed class CategoryBuilder
{
    private int    _id           = 1;
    private int    _companyId    = 1;
    private int    _displayOrder = 1;
    private bool   _isVisible    = true;
    private bool   _isDeleted    = false;
    private readonly List<(string LangCode, string Name)> _translations = [];

    public CategoryBuilder WithId(int id)                   { _id = id;                   return this; }
    public CategoryBuilder WithCompanyId(int id)            { _companyId = id;            return this; }
    public CategoryBuilder WithDisplayOrder(int order)      { _displayOrder = order;      return this; }
    public CategoryBuilder Hidden()                         { _isVisible = false;         return this; }
    public CategoryBuilder Deleted()                        { _isDeleted = true;          return this; }
    public CategoryBuilder WithTranslation(string lang, string name)
    {
        _translations.Add((lang, name));
        return this;
    }

    public Category Build()
    {
        var translations = _translations.Count > 0
            ? _translations
            : [("es", $"Categoría {_id}")];

        return new Category
        {
            Id           = _id,
            CompanyId    = _companyId,
            DisplayOrder = _displayOrder,
            IsVisible    = _isVisible,
            IsDeleted    = _isDeleted,
            Translations = translations
                .Select(t => new CategoryTranslation
                {
                    LanguageCode = t.LangCode,
                    Name         = t.Name,
                })
                .ToList(),
        };
    }
}
