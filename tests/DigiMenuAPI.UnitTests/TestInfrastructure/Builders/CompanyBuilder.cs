using AppCore.Domain.Entities;

namespace DigiMenuAPI.UnitTests.TestInfrastructure.Builders;

/// <summary>Builder fluido para Company (raíz del tenant).</summary>
public sealed class CompanyBuilder
{
    private int    _id          = 1;
    private string _name        = "Test Company";
    private string _slug        = "test-company";
    private string _email       = "company@test.com";
    private int    _planId      = 99;
    private bool   _isActive    = true;
    private int    _maxBranches = -1;
    private int    _maxUsers    = -1;

    public CompanyBuilder WithId(int id)            { _id = id;       return this; }
    public CompanyBuilder WithSlug(string slug)     { _slug = slug;   return this; }
    public CompanyBuilder WithEmail(string email)   { _email = email; return this; }
    public CompanyBuilder WithPlanId(int planId)    { _planId = planId; return this; }
    public CompanyBuilder Inactive()                { _isActive = false; return this; }
    public CompanyBuilder WithMaxBranches(int max)  { _maxBranches = max; return this; }
    public CompanyBuilder WithMaxUsers(int max)     { _maxUsers = max; return this; }

    public Company Build() => new()
    {
        Id          = _id,
        Name        = _name,
        Slug        = _slug,
        Email       = _email,
        PlanId      = _planId,
        IsActive    = _isActive,
        MaxBranches = _maxBranches,
        MaxUsers    = _maxUsers,
    };
}
