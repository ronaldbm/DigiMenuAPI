using DigiMenuAPI.Infrastructure.Entities;

namespace DigiMenuAPI.UnitTests.TestInfrastructure.Builders;

public class TagBuilder
{
    private int    _id        = 100;
    private int    _companyId = 100;
    private string _color     = "#ffffff";
    private bool   _deleted   = false;
    private readonly List<(string Code, string Name)> _translations = [];

    public TagBuilder WithId(int id)            { _id = id;               return this; }
    public TagBuilder WithCompanyId(int id)     { _companyId = id;        return this; }
    public TagBuilder WithColor(string color)   { _color = color;         return this; }
    public TagBuilder Deleted()                 { _deleted = true;        return this; }

    public TagBuilder WithTranslation(string lang, string name)
    {
        _translations.Add((lang, name));
        return this;
    }

    public Tag Build()
    {
        var tag = new Tag
        {
            Id        = _id,
            CompanyId = _companyId,
            Color     = _color,
            IsDeleted = _deleted,
        };

        foreach (var (code, name) in _translations)
            tag.Translations.Add(new TagTranslation { TagId = _id, LanguageCode = code, Name = name });

        return tag;
    }
}
