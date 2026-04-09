using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DigiMenuAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DecorativeFrames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SvgContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecorativeFrames", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    TargetUserId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RelatedEntity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RelatedEntityId = table.Column<int>(type: "int", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AnnualPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxBranches = table.Column<int>(type: "int", nullable: false),
                    MaxUsers = table.Column<int>(type: "int", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformModules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformModules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StandardIcons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SvgContent = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandardIcons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupportedLanguages",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Flag = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportedLanguages", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CountryCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    MaxBranches = table.Column<int>(type: "int", nullable: false),
                    MaxUsers = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Companies_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Location = table.Column<Point>(type: "geography", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Branches_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    HeaderImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeaderStyleOverride = table.Column<byte>(type: "tinyint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    BusinessName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tagline = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FaviconUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BackgroundImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TabsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    DefaultMaxOpenTabs = table.Column<int>(type: "int", nullable: false),
                    DefaultMaxTabAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TabRequiresManagerApproval = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyInfos_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyLanguages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyLanguages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyLanguages_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyLanguages_SupportedLanguages_LanguageCode",
                        column: x => x.LanguageCode,
                        principalTable: "SupportedLanguages",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyModules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    PlatformModuleId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActivatedByUserId = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyModules_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyModules_PlatformModules_PlatformModuleId",
                        column: x => x.PlatformModuleId,
                        principalTable: "PlatformModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanySeos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    MetaTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    GoogleAnalyticsId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FacebookPixelId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySeos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanySeos_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyThemes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    IsDarkMode = table.Column<bool>(type: "bit", nullable: false),
                    DarkModeAutoGenerate = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    HeaderStyle = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    MenuLayout = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    ProductDisplay = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    CategoryHeaderStyle = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    ShowCategoryImages = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ShowProductDetails = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FilterMode = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    ShowContactButton = table.Column<bool>(type: "bit", nullable: false),
                    ShowModalProductInfo = table.Column<bool>(type: "bit", nullable: false),
                    ShowMapInMenu = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true),
                    BackgroundSettings = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ColorPalette = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DarkModePalette = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FrameSettings = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyThemes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyThemes_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxOpenTabs = table.Column<int>(type: "int", nullable: false),
                    MaxTabAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextBillingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrialEndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SuspendedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SuspendedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false, defaultValue: "#ffffff"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tags_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BranchDiscounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DiscountType = table.Column<byte>(type: "tinyint", nullable: false),
                    DefaultValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AppliesTo = table.Column<byte>(type: "tinyint", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    MaxValueForStaff = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchDiscounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BranchDiscounts_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BranchEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EventDate = table.Column<DateTime>(type: "date", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndDate = table.Column<DateTime>(type: "date", nullable: false),
                    FlyerImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ShowPromotionalModal = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    FlyerObjectFit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FlyerObjectPosition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BranchEvents_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BranchLocales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    PhoneCode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    CurrencyLocale = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    TimeZone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Decimals = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchLocales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BranchLocales_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BranchReservationForms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    MaxCapacity = table.Column<int>(type: "int", nullable: false),
                    MinutesBeforeClosing = table.Column<int>(type: "int", nullable: false),
                    MaxCapacityPerReservation = table.Column<int>(type: "int", nullable: false),
                    FormShowName = table.Column<bool>(type: "bit", nullable: false),
                    FormRequireName = table.Column<bool>(type: "bit", nullable: false),
                    FormShowPhone = table.Column<bool>(type: "bit", nullable: false),
                    FormRequirePhone = table.Column<bool>(type: "bit", nullable: false),
                    FormShowTable = table.Column<bool>(type: "bit", nullable: false),
                    FormRequireTable = table.Column<bool>(type: "bit", nullable: false),
                    FormShowPersons = table.Column<bool>(type: "bit", nullable: false),
                    FormRequirePersons = table.Column<bool>(type: "bit", nullable: false),
                    FormShowAllergies = table.Column<bool>(type: "bit", nullable: false),
                    FormRequireAllergies = table.Column<bool>(type: "bit", nullable: false),
                    FormShowBirthday = table.Column<bool>(type: "bit", nullable: false),
                    FormRequireBirthday = table.Column<bool>(type: "bit", nullable: false),
                    FormShowComments = table.Column<bool>(type: "bit", nullable: false),
                    FormRequireComments = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchReservationForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BranchReservationForms_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BranchSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<byte>(type: "tinyint", nullable: false),
                    IsOpen = table.Column<bool>(type: "bit", nullable: false),
                    OpenTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    CloseTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BranchSchedules_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BranchSpecialDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    OpenTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    CloseTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchSpecialDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BranchSpecialDays_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FooterLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StandardIconId = table.Column<int>(type: "int", nullable: true),
                    CustomSvgContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FooterLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FooterLinks_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FooterLinks_StandardIcons_StandardIconId",
                        column: x => x.StandardIconId,
                        principalTable: "StandardIcons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutboxEmails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    ToEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ToName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    EmailType = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LastAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutboxEmails_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OutboxEmails_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReservationDate = table.Column<DateTime>(type: "date", nullable: false),
                    ReservationTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    PeopleCount = table.Column<int>(type: "int", nullable: false),
                    TableNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Allergies = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<byte>(type: "tinyint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    AdminLang = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CategoryTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryTranslations_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    MainImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ImageObjectFit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ImageObjectPosition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TagTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TagId = table.Column<int>(type: "int", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TagTranslations_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutboxEmailBodies",
                columns: table => new
                {
                    OutboxEmailId = table.Column<int>(type: "int", nullable: false),
                    HtmlBody = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxEmailBodies", x => x.OutboxEmailId);
                    table.ForeignKey(
                        name: "FK_OutboxEmailBodies_OutboxEmails_OutboxEmailId",
                        column: x => x.OutboxEmailId,
                        principalTable: "OutboxEmails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    ClientIdentifier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TabAuthorizedByUserId = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accounts_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Accounts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Accounts_Users_TabAuthorizedByUserId",
                        column: x => x.TabAuthorizedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImpersonationSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SuperAdminUserId = table.Column<int>(type: "int", nullable: false),
                    TargetCompanyId = table.Column<int>(type: "int", nullable: false),
                    TargetUserId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpersonationSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImpersonationSessions_Companies_TargetCompanyId",
                        column: x => x.TargetCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImpersonationSessions_Users_SuperAdminUserId",
                        column: x => x.SuperAdminUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImpersonationSessions_Users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetRequests_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PasswordResetRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    SubscriptionId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Method = table.Column<byte>(type: "tinyint", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    RecordedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRecords_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentRecords_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentRecords_Users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BranchProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OfferPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ImageOverrideUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ImageObjectFit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ImageObjectPosition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BranchProducts_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BranchProducts_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BranchProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductTags",
                columns: table => new
                {
                    ProductsId = table.Column<int>(type: "int", nullable: false),
                    TagsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTags", x => new { x.ProductsId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_ProductTags_Products_ProductsId",
                        column: x => x.ProductsId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductTags_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LongDescription = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductTranslations_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountAuditEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    HumanReadable = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountAuditEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountAuditEntries_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountSplits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    SplitName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountSplits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountSplits_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    BranchProductId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountItems_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountItems_BranchProducts_BranchProductId",
                        column: x => x.BranchProductId,
                        principalTable: "BranchProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BranchPromotions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    BranchProductId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Label = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PromoImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    ShowInCarousel = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PromoObjectFit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PromoObjectPosition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchPromotions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BranchPromotions_BranchProducts_BranchProductId",
                        column: x => x.BranchProductId,
                        principalTable: "BranchProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BranchPromotions_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountDiscounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    BranchDiscountId = table.Column<int>(type: "int", nullable: false),
                    AccountItemId = table.Column<int>(type: "int", nullable: true),
                    DiscountType = table.Column<byte>(type: "tinyint", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AppliesTo = table.Column<byte>(type: "tinyint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    AuthorizedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountDiscounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountDiscounts_AccountItems_AccountItemId",
                        column: x => x.AccountItemId,
                        principalTable: "AccountItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountDiscounts_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountDiscounts_BranchDiscounts_BranchDiscountId",
                        column: x => x.BranchDiscountId,
                        principalTable: "BranchDiscounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountDiscounts_Users_AuthorizedByUserId",
                        column: x => x.AuthorizedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountSplitItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountSplitId = table.Column<int>(type: "int", nullable: false),
                    AccountItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountSplitItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountSplitItems_AccountItems_AccountItemId",
                        column: x => x.AccountItemId,
                        principalTable: "AccountItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AccountSplitItems_AccountSplits_AccountSplitId",
                        column: x => x.AccountSplitId,
                        principalTable: "AccountSplits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DecorativeFrames",
                columns: new[] { "Id", "Category", "CreatedAt", "CreatedUserId", "DisplayOrder", "ModifiedAt", "ModifiedUserId", "Name", "SvgContent" },
                values: new object[,]
                {
                    { 1, "universal", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, null, "Classic Border", "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100' fill='none' stroke='currentColor' stroke-width='1'><rect x='2' y='2' width='96' height='96' rx='4'/></svg>" },
                    { 2, "fine-dining", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, null, "Double Line", "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100' fill='none' stroke='currentColor'><rect x='2' y='2' width='96' height='96' stroke-width='0.5'/><rect x='4' y='4' width='92' height='92' stroke-width='0.5'/></svg>" },
                    { 3, "cafe", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, null, "Corner Accents", "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100' fill='none' stroke='currentColor' stroke-width='1.5'><path d='M2 18 L2 2 L18 2'/><path d='M82 2 L98 2 L98 18'/><path d='M98 82 L98 98 L82 98'/><path d='M18 98 L2 98 L2 82'/></svg>" },
                    { 4, "universal", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, null, null, "Rounded", "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100' fill='none' stroke='currentColor' stroke-width='1'><rect x='2' y='2' width='96' height='96' rx='12'/></svg>" },
                    { 5, "fine-dining", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, null, null, "Ornate", "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100' fill='none' stroke='currentColor' stroke-width='0.8'><rect x='3' y='3' width='94' height='94'/><rect x='6' y='6' width='88' height='88'/><circle cx='3' cy='3' r='2' fill='currentColor'/><circle cx='97' cy='3' r='2' fill='currentColor'/><circle cx='97' cy='97' r='2' fill='currentColor'/><circle cx='3' cy='97' r='2' fill='currentColor'/></svg>" },
                    { 6, "bar", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 6, null, null, "Minimal Top", "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100' fill='none' stroke='currentColor' stroke-width='1'><line x1='5' y1='3' x2='95' y2='3'/><line x1='5' y1='97' x2='95' y2='97'/></svg>" },
                    { 7, "fine-dining", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 7, null, null, "Diamond Corners", "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100' fill='none' stroke='currentColor' stroke-width='1'><rect x='3' y='3' width='94' height='94'/><polygon points='3,10 10,3 3,3' fill='currentColor'/><polygon points='90,3 97,3 97,10' fill='currentColor'/><polygon points='97,90 97,97 90,97' fill='currentColor'/><polygon points='10,97 3,97 3,90' fill='currentColor'/></svg>" },
                    { 8, "bar", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 8, null, null, "Wave", "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100' fill='none' stroke='currentColor' stroke-width='1'><path d='M2 2 Q10 8 18 2 Q26 -4 34 2 Q42 8 50 2 Q58 -4 66 2 Q74 8 82 2 Q90 -4 98 2'/><path d='M2 98 Q10 92 18 98 Q26 104 34 98 Q42 92 50 98 Q58 104 66 98 Q74 92 82 98 Q90 104 98 98'/></svg>" }
                });

            migrationBuilder.InsertData(
                table: "Plans",
                columns: new[] { "Id", "AnnualPrice", "Code", "Description", "DisplayOrder", "IsActive", "IsPublic", "MaxBranches", "MaxUsers", "MonthlyPrice", "Name" },
                values: new object[,]
                {
                    { 1, null, "BASIC", "Ideal para negocios con una sola sucursal.", 1, true, true, 1, 3, 0m, "Básico" },
                    { 2, 24.99m, "PRO", "Para negocios en crecimiento con hasta 3 sucursales.", 2, true, true, 3, 10, 29.99m, "Pro" },
                    { 3, 64.99m, "BUSINESS", "Para cadenas con múltiples locales.", 3, true, true, 10, 30, 79.99m, "Business" },
                    { 4, 159.99m, "ENTERPRISE", "Sin límites. Ideal para grandes cadenas y franquicias.", 4, true, true, -1, -1, 199.99m, "Enterprise" }
                });

            migrationBuilder.InsertData(
                table: "PlatformModules",
                columns: new[] { "Id", "Code", "Description", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "RESERVATIONS", "Gestión de reservas de mesas desde el menú público.", 1, true, "Reservaciones" },
                    { 2, "TABLE_MANAGEMENT", "Control visual del estado de las mesas del local.", 2, true, "Gestión de Mesas" },
                    { 3, "ANALYTICS", "Reportes de visitas, productos más vistos y reservas.", 3, true, "Analíticas" },
                    { 4, "ONLINE_ORDERS", "Permite recibir pedidos directamente desde el menú.", 4, true, "Pedidos en Línea" }
                });

            migrationBuilder.InsertData(
                table: "StandardIcons",
                columns: new[] { "Id", "Name", "SvgContent" },
                values: new object[,]
                {
                    { 1, "Facebook", "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z'></path></svg>" },
                    { 2, "Instagram", "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><rect x='2' y='2' width='20' height='20' rx='5' ry='5'></rect><path d='M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z'></path><line x1='17.5' y1='6.5' x2='17.51' y2='6.5'></line></svg>" },
                    { 3, "WhatsApp", "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 0 1-7.6 4.2 8.38 8.38 0 0 1-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 0 1-.9-3.8 8.5 8.5 0 0 1 4.2-7.6 8.38 8.38 0 0 1 3.8-.9h.5a8.48 8.48 0 0 1 8 8v.5z'/></svg>" },
                    { 4, "TikTok", "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M9 12a4 4 0 1 0 4 4V4a5 5 0 0 0 5 5'></path></svg>" },
                    { 5, "YouTube", "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M22.54 6.42a2.78 2.78 0 0 0-1.95-1.96C18.88 4 12 4 12 4s-6.88 0-8.59.46a2.78 2.78 0 0 0-1.95 1.96A29 29 0 0 0 1 12a29 29 0 0 0 .46 5.58A2.78 2.78 0 0 0 3.41 19.6C5.12 20 12 20 12 20s6.88 0 8.59-.46a2.78 2.78 0 0 0 1.95-1.95A29 29 0 0 0 23 12a29 29 0 0 0-.46-5.58z'></path><polygon points='9.75 15.02 15.5 12 9.75 8.98 9.75 15.02'></polygon></svg>" },
                    { 6, "X", "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M4 4l16 16M4 20L20 4'/></svg>" },
                    { 7, "Web", "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><circle cx='12' cy='12' r='10'></circle><line x1='2' y1='12' x2='22' y2='12'></line><path d='M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z'></path></svg>" }
                });

            migrationBuilder.InsertData(
                table: "SupportedLanguages",
                columns: new[] { "Code", "DisplayOrder", "Flag", "IsActive", "Name" },
                values: new object[,]
                {
                    { "en", 2, "🇺🇸", true, "English" },
                    { "es", 1, "🇪🇸", true, "Español" },
                    { "fr", 4, "🇫🇷", true, "Français" },
                    { "pt", 3, "🇧🇷", true, "Português" }
                });

            migrationBuilder.InsertData(
                table: "Companies",
                columns: new[] { "Id", "CountryCode", "CreatedAt", "CreatedUserId", "Email", "IsActive", "MaxBranches", "MaxUsers", "ModifiedAt", "ModifiedUserId", "Name", "Phone", "PlanId", "Slug" },
                values: new object[] { 1, "CR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "admin@digimenu.app", true, -1, -1, null, null, "DigiMenu Platform", "+50600000000", 4, "demo" });

            migrationBuilder.InsertData(
                table: "Branches",
                columns: new[] { "Id", "Address", "CompanyId", "CreatedAt", "CreatedUserId", "Email", "IsActive", "IsDeleted", "Location", "ModifiedAt", "ModifiedUserId", "Name", "Phone", "Slug" },
                values: new object[] { 1, "San José, Costa Rica", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "admin@digimenu.app", true, false, null, null, null, "Sucursal Principal", "+50600000000", "Principal" });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CompanyId", "CreatedAt", "CreatedUserId", "DisplayOrder", "HeaderImageUrl", "HeaderStyleOverride", "IsDeleted", "IsVisible", "ModifiedAt", "ModifiedUserId" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, null, null, false, true, null, null },
                    { 2, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, null, null, false, true, null, null },
                    { 3, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, null, null, false, true, null, null },
                    { 4, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, null, null, false, true, null, null },
                    { 5, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, null, null, false, true, null, null }
                });

            migrationBuilder.InsertData(
                table: "CompanyInfos",
                columns: new[] { "Id", "BackgroundImageUrl", "BusinessName", "CompanyId", "CreatedAt", "CreatedUserId", "DefaultMaxOpenTabs", "DefaultMaxTabAmount", "FaviconUrl", "LogoUrl", "ModifiedAt", "ModifiedUserId", "TabRequiresManagerApproval", "TabsEnabled", "Tagline" },
                values: new object[] { 1, null, "DigiMenu Demo", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, 0m, null, null, null, null, false, false, "El mejor menú digital para tu restaurante" });

            migrationBuilder.InsertData(
                table: "CompanyLanguages",
                columns: new[] { "Id", "CompanyId", "CreatedAt", "CreatedUserId", "IsDefault", "LanguageCode", "ModifiedAt", "ModifiedUserId" },
                values: new object[] { 1, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "es", null, null });

            migrationBuilder.InsertData(
                table: "CompanyModules",
                columns: new[] { "Id", "ActivatedAt", "ActivatedByUserId", "CompanyId", "ExpiresAt", "IsActive", "Notes", "PlatformModuleId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1, null, true, null, 1 },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1, null, true, null, 2 },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1, null, true, null, 3 },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1, null, true, null, 4 }
                });

            migrationBuilder.InsertData(
                table: "CompanySeos",
                columns: new[] { "Id", "CompanyId", "CreatedAt", "CreatedUserId", "FacebookPixelId", "GoogleAnalyticsId", "MetaDescription", "MetaTitle", "ModifiedAt", "ModifiedUserId" },
                values: new object[] { 1, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "El mejor menú digital para tu restaurante", "DigiMenu Demo", null, null });

            migrationBuilder.InsertData(
                table: "Tags",
                columns: new[] { "Id", "Color", "CompanyId", "CreatedAt", "CreatedUserId", "IsDeleted", "ModifiedAt", "ModifiedUserId" },
                values: new object[,]
                {
                    { 1, "#4CAF50", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, null, null },
                    { 2, "#F44336", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, null, null },
                    { 3, "#9C27B0", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, null, null },
                    { 4, "#FF9800", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, null, null },
                    { 5, "#F50057", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, null, null },
                    { 6, "#2196F3", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, null, null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AdminLang", "BranchId", "CompanyId", "CreatedAt", "CreatedUserId", "Email", "FullName", "IsActive", "IsDeleted", "LastLoginAt", "ModifiedAt", "ModifiedUserId", "MustChangePassword", "PasswordHash", "Role" },
                values: new object[] { 1, null, null, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "admin@digimenu.app", "Super Admin", true, false, null, null, null, false, "$2y$12$tJGKbEhmd00CrIaQU8yf0eKU.doWOliVml/J48.NCwhXlF./.ZZgS", (byte)255 });

            migrationBuilder.InsertData(
                table: "BranchLocales",
                columns: new[] { "Id", "BranchId", "CountryCode", "CreatedAt", "CreatedUserId", "Currency", "CurrencyLocale", "Decimals", "Language", "ModifiedAt", "ModifiedUserId", "PhoneCode", "TimeZone" },
                values: new object[] { 1, 1, "CR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "CRC", "es-CR", (byte)0, "es", null, null, "+506", "America/Costa_Rica" });

            migrationBuilder.InsertData(
                table: "BranchReservationForms",
                columns: new[] { "Id", "BranchId", "CreatedAt", "CreatedUserId", "FormRequireAllergies", "FormRequireBirthday", "FormRequireComments", "FormRequireName", "FormRequirePersons", "FormRequirePhone", "FormRequireTable", "FormShowAllergies", "FormShowBirthday", "FormShowComments", "FormShowName", "FormShowPersons", "FormShowPhone", "FormShowTable", "MaxCapacity", "MaxCapacityPerReservation", "MinutesBeforeClosing", "ModifiedAt", "ModifiedUserId" },
                values: new object[] { 1, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, false, false, true, true, true, false, false, false, true, true, true, true, false, 0, 0, 0, null, null });

            migrationBuilder.InsertData(
                table: "BranchSchedules",
                columns: new[] { "Id", "BranchId", "CloseTime", "CreatedAt", "CreatedUserId", "DayOfWeek", "IsOpen", "ModifiedAt", "ModifiedUserId", "OpenTime" },
                values: new object[,]
                {
                    { 1, 1, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, (byte)0, false, null, null, null },
                    { 2, 1, new TimeSpan(0, 22, 0, 0, 0), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, (byte)1, true, null, null, new TimeSpan(0, 9, 0, 0, 0) },
                    { 3, 1, new TimeSpan(0, 22, 0, 0, 0), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, (byte)2, true, null, null, new TimeSpan(0, 9, 0, 0, 0) },
                    { 4, 1, new TimeSpan(0, 22, 0, 0, 0), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, (byte)3, true, null, null, new TimeSpan(0, 9, 0, 0, 0) },
                    { 5, 1, new TimeSpan(0, 22, 0, 0, 0), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, (byte)4, true, null, null, new TimeSpan(0, 9, 0, 0, 0) },
                    { 6, 1, new TimeSpan(0, 22, 0, 0, 0), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, (byte)5, true, null, null, new TimeSpan(0, 9, 0, 0, 0) },
                    { 7, 1, new TimeSpan(0, 22, 0, 0, 0), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, (byte)6, true, null, null, new TimeSpan(0, 9, 0, 0, 0) }
                });

            migrationBuilder.InsertData(
                table: "CategoryTranslations",
                columns: new[] { "Id", "CategoryId", "LanguageCode", "Name" },
                values: new object[,]
                {
                    { 1001, 1, "es", "Entradas" },
                    { 1002, 2, "es", "Platos Fuertes" },
                    { 1003, 3, "es", "Postres" },
                    { 1004, 4, "es", "Bebidas" },
                    { 1005, 5, "es", "Bebidas Alcohólicas" }
                });

            migrationBuilder.InsertData(
                table: "FooterLinks",
                columns: new[] { "Id", "BranchId", "CreatedAt", "CreatedUserId", "CustomSvgContent", "DisplayOrder", "IsDeleted", "IsVisible", "Label", "ModifiedAt", "ModifiedUserId", "StandardIconId", "Url" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 1, false, true, "Instagram", null, null, 2, "https://instagram.com/digimenu" },
                    { 2, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 2, false, true, "WhatsApp", null, null, 3, "https://wa.me/5060616827" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "CategoryId", "CompanyId", "CreatedAt", "CreatedUserId", "ImageObjectFit", "ImageObjectPosition", "IsDeleted", "MainImageUrl", "ModifiedAt", "ModifiedUserId" },
                values: new object[,]
                {
                    { 1, 1, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "cover", "50% 50%", false, null, null, null },
                    { 2, 1, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "cover", "50% 50%", false, null, null, null },
                    { 3, 2, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "cover", "50% 50%", false, null, null, null },
                    { 4, 2, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "cover", "50% 50%", false, null, null, null },
                    { 5, 2, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "cover", "50% 50%", false, null, null, null },
                    { 6, 3, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "cover", "50% 50%", false, null, null, null },
                    { 7, 3, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "cover", "50% 50%", false, null, null, null },
                    { 8, 4, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "cover", "50% 50%", false, null, null, null },
                    { 9, 4, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "cover", "50% 50%", false, null, null, null },
                    { 10, 5, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "cover", "50% 50%", false, null, null, null },
                    { 11, 5, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "cover", "50% 50%", false, null, null, null }
                });

            migrationBuilder.InsertData(
                table: "TagTranslations",
                columns: new[] { "Id", "LanguageCode", "Name", "TagId" },
                values: new object[,]
                {
                    { 1001, "es", "Vegano", 1 },
                    { 1002, "es", "Picante", 2 },
                    { 1003, "es", "Alcohólico", 3 },
                    { 1004, "es", "Sin Gluten", 4 },
                    { 1005, "es", "Popular", 5 },
                    { 1006, "es", "Nuevo", 6 }
                });

            migrationBuilder.InsertData(
                table: "BranchProducts",
                columns: new[] { "Id", "BranchId", "CategoryId", "CreatedAt", "CreatedUserId", "DisplayOrder", "ImageObjectFit", "ImageObjectPosition", "ImageOverrideUrl", "IsDeleted", "IsVisible", "ModifiedAt", "ModifiedUserId", "OfferPrice", "Price", "ProductId" },
                values: new object[,]
                {
                    { 1, 1, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, "cover", "50% 50%", null, false, true, null, null, null, 5500m, 1 },
                    { 2, 1, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, "cover", "50% 50%", null, false, true, null, null, 3000m, 3500m, 2 },
                    { 3, 1, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, "cover", "50% 50%", null, false, true, null, null, null, 7500m, 3 },
                    { 4, 1, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, "cover", "50% 50%", null, false, true, null, null, 10900m, 12500m, 4 },
                    { 5, 1, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, "cover", "50% 50%", null, false, true, null, null, null, 8500m, 5 },
                    { 6, 1, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, "cover", "50% 50%", null, false, true, null, null, null, 3200m, 6 },
                    { 7, 1, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, "cover", "50% 50%", null, false, true, null, null, null, 3800m, 7 },
                    { 8, 1, 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, "cover", "50% 50%", null, false, true, null, null, null, 1500m, 8 },
                    { 9, 1, 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, "cover", "50% 50%", null, false, true, null, null, null, 1800m, 9 },
                    { 10, 1, 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, "cover", "50% 50%", null, false, true, null, null, null, 2200m, 10 },
                    { 11, 1, 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, "cover", "50% 50%", null, false, true, null, null, null, 3500m, 11 }
                });

            migrationBuilder.InsertData(
                table: "ProductTranslations",
                columns: new[] { "Id", "LanguageCode", "LongDescription", "Name", "ProductId", "ShortDescription" },
                values: new object[,]
                {
                    { 1001, "es", null, "Ceviche Clásico", 1, "Fresco ceviche de corvina con limón y culantro." },
                    { 1002, "es", null, "Patacones con Guacamole", 2, "Patacones crocantes con guacamole casero." },
                    { 1003, "es", null, "Casado Tradicional", 3, "Arroz, frijoles, ensalada, maduro y carne a elegir." },
                    { 1004, "es", null, "Lomo al Chimichurri", 4, "Lomo de res al punto con salsa chimichurri." },
                    { 1005, "es", null, "Bowl Vegano", 5, "Quinoa, vegetales asados, hummus y tahini." },
                    { 1006, "es", null, "Tres Leches", 6, "Bizcocho esponjoso bañado en tres tipos de leche." },
                    { 1007, "es", null, "Brownies con Helado", 7, "Brownie de chocolate caliente con helado de vainilla." },
                    { 1008, "es", null, "Café Americano", 8, "Café negro de tueste medio." },
                    { 1009, "es", null, "Refresco Natural", 9, "Cas, tamarindo o guanábana. A elegir." },
                    { 1010, "es", null, "Imperial", 10, "Cerveza nacional 355ml bien fría." },
                    { 1011, "es", null, "Guaro Sour", 11, "Guaro Cacique, limón, azúcar y hielo." }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountAuditEntries_AccountId",
                table: "AccountAuditEntries",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountDiscounts_AccountId",
                table: "AccountDiscounts",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountDiscounts_AccountItemId",
                table: "AccountDiscounts",
                column: "AccountItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountDiscounts_AuthorizedByUserId",
                table: "AccountDiscounts",
                column: "AuthorizedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountDiscounts_BranchDiscountId",
                table: "AccountDiscounts",
                column: "BranchDiscountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountItems_AccountId",
                table: "AccountItems",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountItems_BranchProductId",
                table: "AccountItems",
                column: "BranchProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_BranchId_Status",
                table: "Accounts",
                columns: new[] { "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_CustomerId",
                table: "Accounts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_TabAuthorizedByUserId",
                table: "Accounts",
                column: "TabAuthorizedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountSplitItems_AccountItemId",
                table: "AccountSplitItems",
                column: "AccountItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountSplitItems_AccountSplitId",
                table: "AccountSplitItems",
                column: "AccountSplitId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountSplits_AccountId",
                table: "AccountSplits",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchDiscounts_BranchId_IsActive",
                table: "BranchDiscounts",
                columns: new[] { "BranchId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Branches_CompanyId_IsDeleted",
                table: "Branches",
                columns: new[] { "CompanyId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Branches_CompanyId_Slug",
                table: "Branches",
                columns: new[] { "CompanyId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BranchEvents_BranchId_EventDate",
                table: "BranchEvents",
                columns: new[] { "BranchId", "EventDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BranchEvents_BranchId_ShowPromotionalModal_IsActive",
                table: "BranchEvents",
                columns: new[] { "BranchId", "ShowPromotionalModal", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_BranchLocales_BranchId",
                table: "BranchLocales",
                column: "BranchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BranchProducts_BranchId_IsDeleted_DisplayOrder",
                table: "BranchProducts",
                columns: new[] { "BranchId", "IsDeleted", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_BranchProducts_BranchId_ProductId",
                table: "BranchProducts",
                columns: new[] { "BranchId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BranchProducts_CategoryId",
                table: "BranchProducts",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchProducts_ProductId",
                table: "BranchProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchPromotions_BranchId_ShowInCarousel_IsActive_DisplayOrder",
                table: "BranchPromotions",
                columns: new[] { "BranchId", "ShowInCarousel", "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_BranchPromotions_BranchProductId",
                table: "BranchPromotions",
                column: "BranchProductId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchReservationForms_BranchId",
                table: "BranchReservationForms",
                column: "BranchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BranchSchedules_BranchId_DayOfWeek",
                table: "BranchSchedules",
                columns: new[] { "BranchId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BranchSpecialDays_BranchId_Date",
                table: "BranchSpecialDays",
                columns: new[] { "BranchId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CompanyId_IsDeleted_DisplayOrder",
                table: "Categories",
                columns: new[] { "CompanyId", "IsDeleted", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryTranslations_CategoryId_LanguageCode",
                table: "CategoryTranslations",
                columns: new[] { "CategoryId", "LanguageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_PlanId",
                table: "Companies",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Slug",
                table: "Companies",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyInfos_CompanyId",
                table: "CompanyInfos",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyLanguages_CompanyId_LanguageCode",
                table: "CompanyLanguages",
                columns: new[] { "CompanyId", "LanguageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyLanguages_LanguageCode",
                table: "CompanyLanguages",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyModules_CompanyId_IsActive",
                table: "CompanyModules",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyModules_CompanyId_PlatformModuleId",
                table: "CompanyModules",
                columns: new[] { "CompanyId", "PlatformModuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyModules_PlatformModuleId",
                table: "CompanyModules",
                column: "PlatformModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySeos_CompanyId",
                table: "CompanySeos",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyThemes_CompanyId",
                table: "CompanyThemes",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CompanyId_IsActive",
                table: "Customers",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FooterLinks_BranchId_IsDeleted_DisplayOrder",
                table: "FooterLinks",
                columns: new[] { "BranchId", "IsDeleted", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_FooterLinks_StandardIconId",
                table: "FooterLinks",
                column: "StandardIconId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationSessions_SuperAdminUserId_IssuedAt",
                table: "ImpersonationSessions",
                columns: new[] { "SuperAdminUserId", "IssuedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationSessions_TargetCompanyId",
                table: "ImpersonationSessions",
                column: "TargetCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationSessions_TargetUserId",
                table: "ImpersonationSessions",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationSessions_TokenHash",
                table: "ImpersonationSessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CompanyId_IsRead",
                table: "Notifications",
                columns: new[] { "CompanyId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CompanyId_TargetUserId",
                table: "Notifications",
                columns: new[] { "CompanyId", "TargetUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEmails_BranchId",
                table: "OutboxEmails",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEmails_CompanyId_EmailType_Status",
                table: "OutboxEmails",
                columns: new[] { "CompanyId", "EmailType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEmails_Status_NextRetryAt",
                table: "OutboxEmails",
                columns: new[] { "Status", "NextRetryAt" },
                filter: "[Status] IN (0, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetRequests_CompanyId",
                table: "PasswordResetRequests",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetRequests_Token",
                table: "PasswordResetRequests",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetRequests_UserId_IsUsed_ExpiresAt",
                table: "PasswordResetRequests",
                columns: new[] { "UserId", "IsUsed", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_CompanyId_PaidAt",
                table: "PaymentRecords",
                columns: new[] { "CompanyId", "PaidAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_CompanyId_Status",
                table: "PaymentRecords",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_RecordedByUserId",
                table: "PaymentRecords",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_SubscriptionId",
                table: "PaymentRecords",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_Code",
                table: "Plans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformModules_Code",
                table: "PlatformModules",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CompanyId_IsDeleted",
                table: "Products",
                columns: new[] { "CompanyId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductTags_TagsId",
                table: "ProductTags",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTranslations_ProductId_LanguageCode",
                table: "ProductTranslations",
                columns: new[] { "ProductId", "LanguageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_BranchId_IsDeleted_ReservationDate",
                table: "Reservations",
                columns: new[] { "BranchId", "IsDeleted", "ReservationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CompanyId",
                table: "Subscriptions",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlanId",
                table: "Subscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Status",
                table: "Subscriptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Status_EndDate",
                table: "Subscriptions",
                columns: new[] { "Status", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_CompanyId_IsDeleted",
                table: "Tags",
                columns: new[] { "CompanyId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TagTranslations_TagId_LanguageCode",
                table: "TagTranslations",
                columns: new[] { "TagId", "LanguageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_BranchId_IsDeleted",
                table: "Users",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_CompanyId_IsDeleted_IsActive",
                table: "Users",
                columns: new[] { "CompanyId", "IsDeleted", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountAuditEntries");

            migrationBuilder.DropTable(
                name: "AccountDiscounts");

            migrationBuilder.DropTable(
                name: "AccountSplitItems");

            migrationBuilder.DropTable(
                name: "BranchEvents");

            migrationBuilder.DropTable(
                name: "BranchLocales");

            migrationBuilder.DropTable(
                name: "BranchPromotions");

            migrationBuilder.DropTable(
                name: "BranchReservationForms");

            migrationBuilder.DropTable(
                name: "BranchSchedules");

            migrationBuilder.DropTable(
                name: "BranchSpecialDays");

            migrationBuilder.DropTable(
                name: "CategoryTranslations");

            migrationBuilder.DropTable(
                name: "CompanyInfos");

            migrationBuilder.DropTable(
                name: "CompanyLanguages");

            migrationBuilder.DropTable(
                name: "CompanyModules");

            migrationBuilder.DropTable(
                name: "CompanySeos");

            migrationBuilder.DropTable(
                name: "CompanyThemes");

            migrationBuilder.DropTable(
                name: "DecorativeFrames");

            migrationBuilder.DropTable(
                name: "FooterLinks");

            migrationBuilder.DropTable(
                name: "ImpersonationSessions");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "OutboxEmailBodies");

            migrationBuilder.DropTable(
                name: "PasswordResetRequests");

            migrationBuilder.DropTable(
                name: "PaymentRecords");

            migrationBuilder.DropTable(
                name: "ProductTags");

            migrationBuilder.DropTable(
                name: "ProductTranslations");

            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropTable(
                name: "TagTranslations");

            migrationBuilder.DropTable(
                name: "BranchDiscounts");

            migrationBuilder.DropTable(
                name: "AccountItems");

            migrationBuilder.DropTable(
                name: "AccountSplits");

            migrationBuilder.DropTable(
                name: "SupportedLanguages");

            migrationBuilder.DropTable(
                name: "PlatformModules");

            migrationBuilder.DropTable(
                name: "StandardIcons");

            migrationBuilder.DropTable(
                name: "OutboxEmails");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "BranchProducts");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "Plans");
        }
    }
}
