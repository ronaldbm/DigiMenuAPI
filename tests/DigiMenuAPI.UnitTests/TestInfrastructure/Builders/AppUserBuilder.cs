using AppCore.Application.Common;
using AppCore.Domain.Entities;

namespace DigiMenuAPI.UnitTests.TestInfrastructure.Builders;

/// <summary>Builder fluido para AppUser.</summary>
public sealed class AppUserBuilder
{
    private int    _id        = 1;
    private int    _companyId = 1;
    private int?   _branchId  = null;
    private string _fullName  = "Test User";
    private string _email     = "user@test.com";
    private byte   _role      = UserRoles.CompanyAdmin;
    private bool   _isActive  = true;
    private bool   _isDeleted = false;

    public AppUserBuilder WithId(int id)              { _id = id;              return this; }
    public AppUserBuilder WithCompanyId(int id)       { _companyId = id;       return this; }
    public AppUserBuilder WithBranchId(int? id)       { _branchId = id;        return this; }
    public AppUserBuilder WithEmail(string email)     { _email = email;        return this; }
    public AppUserBuilder WithFullName(string name)   { _fullName = name;      return this; }
    public AppUserBuilder WithRole(byte role)         { _role = role;          return this; }
    public AppUserBuilder AsBranchAdmin(int branchId) { _role = UserRoles.BranchAdmin; _branchId = branchId; return this; }
    public AppUserBuilder AsStaff(int branchId)       { _role = UserRoles.Staff;       _branchId = branchId; return this; }
    public AppUserBuilder AsCompanyAdmin()            { _role = UserRoles.CompanyAdmin; _branchId = null;    return this; }
    public AppUserBuilder AsSuperAdmin()              { _role = UserRoles.SuperAdmin;   _branchId = null;    return this; }
    public AppUserBuilder Inactive()                  { _isActive = false;     return this; }
    public AppUserBuilder Deleted()                   { _isDeleted = true;     return this; }

    public AppUser Build() => new()
    {
        Id           = _id,
        CompanyId    = _companyId,
        BranchId     = _branchId,
        FullName     = _fullName,
        Email        = _email,
        PasswordHash = "$2a$11$placeholder_for_tests_only",
        Role         = _role,
        IsActive     = _isActive,
        IsDeleted    = _isDeleted,
    };
}
