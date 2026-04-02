using AppCore.Domain.Entities;

namespace DigiMenuAPI.UnitTests.TestInfrastructure.Builders;

/// <summary>Builder fluido para Branch (ubicación dentro de un tenant).</summary>
public sealed class BranchBuilder
{
    private int    _id        = 1;
    private int    _companyId = 1;
    private string _name      = "Main Branch";
    private string _slug      = "main-branch";
    private bool   _isActive  = true;
    private bool   _isDeleted = false;

    public BranchBuilder WithId(int id)            { _id = id;              return this; }
    public BranchBuilder WithCompanyId(int id)     { _companyId = id;       return this; }
    public BranchBuilder WithSlug(string slug)     { _slug = slug;          return this; }
    public BranchBuilder WithName(string name)     { _name = name;          return this; }
    public BranchBuilder Inactive()                { _isActive = false;     return this; }
    public BranchBuilder Deleted()                 { _isDeleted = true;     return this; }

    public Branch Build() => new()
    {
        Id        = _id,
        CompanyId = _companyId,
        Name      = _name,
        Slug      = _slug,
        IsActive  = _isActive,
        IsDeleted = _isDeleted,
    };
}
