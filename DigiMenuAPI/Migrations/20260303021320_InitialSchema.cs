using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DigiMenuAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
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
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReservationDate = table.Column<DateTime>(type: "date", nullable: false),
                    ReservationTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    PeopleCount = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TableNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Allergies = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
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
                    IsDarkMode = table.Column<bool>(type: "bit", nullable: false),
                    PageBackgroundColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false, defaultValue: "#FFFFFF"),
                    HeaderBackgroundColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false, defaultValue: "#FFFFFF"),
                    HeaderTextColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false, defaultValue: "#000000"),
                    TabBackgroundColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false, defaultValue: "#000000"),
                    TabTextColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false, defaultValue: "#FFFFFF"),
                    PrimaryColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false, defaultValue: "#E63946"),
                    PrimaryTextColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false, defaultValue: "#FFFFFF"),
                    SecondaryColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false, defaultValue: "#457B9D"),
                    TitlesColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false, defaultValue: "#000000"),
                    TextColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false, defaultValue: "#1D3557"),
                    BrowserThemeColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false, defaultValue: "#FFFFFF"),
                    HeaderStyle = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    MenuLayout = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    ProductDisplay = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    ShowProductDetails = table.Column<bool>(type: "bit", nullable: false),
                    ShowSearchButton = table.Column<bool>(type: "bit", nullable: false),
                    ShowContactButton = table.Column<bool>(type: "bit", nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    PhoneCode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    CurrencyLocale = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    TimeZone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Decimals = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)2),
                    MetaTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    GoogleAnalyticsId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FacebookPixelId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Settings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false, defaultValue: "#ffffff"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
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
                name: "FooterLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Label = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StandardIconId = table.Column<int>(type: "int", nullable: true),
                    CustomSvgContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FooterLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FooterLinks_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
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
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LongDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OfferPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MainImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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

            migrationBuilder.InsertData(
                table: "Companies",
                columns: new[] { "Id", "CountryCode", "CreatedAt", "Email", "IsActive", "ModifiedAt", "Name", "Phone", "Slug" },
                values: new object[] { 1, "CR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@digimenu.com", true, null, "DigiMenu Platform", null, "digimenu-platform" });

            migrationBuilder.InsertData(
                table: "PlatformModules",
                columns: new[] { "Id", "Code", "Description", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "RESERVATIONS", "Gestión de reservas de mesas y eventos.", 1, true, "Reservas" },
                    { 2, "TABLE_MANAGEMENT", "Vista de plano de mesas en tiempo real.", 2, true, "Gestión de Mesas" },
                    { 3, "ANALYTICS", "Reportes de visitas, productos más vistos y conversiones.", 3, true, "Analytics Avanzados" },
                    { 4, "ONLINE_ORDERS", "Delivery y take away con integración de pagos.", 4, true, "Pedidos en Línea" }
                });

            migrationBuilder.InsertData(
                table: "StandardIcons",
                columns: new[] { "Id", "Name", "SvgContent" },
                values: new object[,]
                {
                    { 1, "Facebook", "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z'></path></svg>" },
                    { 2, "Instagram", "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><rect x='2' y='2' width='20' height='20' rx='5' ry='5'></rect><path d='M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z'></path><line x1='17.5' y1='6.5' x2='17.51' y2='6.5'></line></svg>" },
                    { 3, "WhatsApp", "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 1 1-7.6-14h.1c4.3 0 7.9 3.5 8.4 7.7z'></path><path d='M17 16l-4-4 4-4'></path></svg>" },
                    { 4, "TikTok", "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M9 12a4 4 0 1 0 4 4V4a5 5 0 0 0 5 5'></path></svg>" },
                    { 5, "YouTube", "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M22.54 6.42a2.78 2.78 0 0 0-1.95-1.96C18.88 4 12 4 12 4s-6.88 0-8.59.46A2.78 2.78 0 0 0 1.46 6.42 29 29 0 0 0 1 12a29 29 0 0 0 .46 5.58 2.78 2.78 0 0 0 1.95 1.96C5.12 20 12 20 12 20s6.88 0 8.59-.46a2.78 2.78 0 0 0 1.95-1.96A29 29 0 0 0 23 12a29 29 0 0 0-.46-5.58z'></path><polygon points='9.75 15.02 15.5 12 9.75 8.98 9.75 15.02'></polygon></svg>" },
                    { 6, "X (Twitter)", "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M4 4l16 16M4 20L20 4'/></svg>" },
                    { 7, "Web", "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><circle cx='12' cy='12' r='10'></circle><line x1='2' y1='12' x2='22' y2='12'></line><path d='M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z'></path></svg>" }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CompanyId", "CreatedAt", "DisplayOrder", "IsDeleted", "IsVisible", "ModifiedAt", "Name" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, true, null, "Entradas" },
                    { 2, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, false, true, null, "Platos Fuertes" },
                    { 3, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, false, true, null, "Postres" },
                    { 4, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, false, true, null, "Bebidas" },
                    { 5, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, false, true, null, "Bebidas Alcohólicas" }
                });

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
                table: "FooterLinks",
                columns: new[] { "Id", "CompanyId", "CreatedAt", "CustomSvgContent", "DisplayOrder", "IsDeleted", "IsVisible", "Label", "ModifiedAt", "StandardIconId", "Url" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, false, true, "Instagram", null, 2, "https://instagram.com/digimenu" },
                    { 2, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, false, true, "WhatsApp", null, 3, "https://wa.me/50612345678" }
                });

            migrationBuilder.InsertData(
                table: "Settings",
                columns: new[] { "Id", "BackgroundImageUrl", "BrowserThemeColor", "BusinessName", "CompanyId", "CountryCode", "CreatedAt", "Currency", "CurrencyLocale", "FacebookPixelId", "FaviconUrl", "FormRequireAllergies", "FormRequireBirthday", "FormRequireComments", "FormRequireName", "FormRequirePersons", "FormRequirePhone", "FormRequireTable", "FormShowAllergies", "FormShowBirthday", "FormShowComments", "FormShowName", "FormShowPersons", "FormShowPhone", "FormShowTable", "GoogleAnalyticsId", "HeaderBackgroundColor", "HeaderStyle", "HeaderTextColor", "IsDarkMode", "Language", "LogoUrl", "MenuLayout", "MetaDescription", "MetaTitle", "ModifiedAt", "PageBackgroundColor", "PhoneCode", "PrimaryColor", "PrimaryTextColor", "ProductDisplay", "SecondaryColor", "ShowContactButton", "ShowProductDetails", "ShowSearchButton", "TabBackgroundColor", "TabTextColor", "Tagline", "TextColor", "TimeZone", "TitlesColor" },
                values: new object[] { 1, null, "#1D3557", "DigiMenu Demo", 1, "CR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "CRC", "es-CR", null, null, false, false, false, true, true, true, false, false, false, true, true, true, true, false, null, "#1D3557", (byte)1, "#FFFFFF", false, "es", null, (byte)1, "El mejor menú digital para tu restaurante", "DigiMenu Demo", null, "#F1FAEE", "+506", "#E63946", "#FFFFFF", (byte)1, "#457B9D", true, true, true, "#457B9D", "#FFFFFF", "Tu menú digital, siempre disponible", "#1D3557", "America/Costa_Rica", "#1D3557" });

            migrationBuilder.InsertData(
                table: "Tags",
                columns: new[] { "Id", "Color", "CompanyId", "CreatedAt", "IsDeleted", "ModifiedAt", "Name" },
                values: new object[,]
                {
                    { 1, "#4CAF50", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, null, "Vegano" },
                    { 2, "#F44336", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, null, "Picante" },
                    { 3, "#9C27B0", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, null, "Alcohólico" },
                    { 4, "#FF9800", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, null, "Sin Gluten" },
                    { 5, "#F50057", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, null, "Popular" },
                    { 6, "#2196F3", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, null, "Nuevo" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CompanyId", "CreatedAt", "Email", "FullName", "IsActive", "LastLoginAt", "ModifiedAt", "PasswordHash", "Role" },
                values: new object[] { 1, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@digimenu.app", "Super Admin", true, null, null, "$2y$12$5JPWX19o/4yLCIfshv2Nq.9EGh/UOfw.wiCaiyI2rYxvu19/LU.tW", (byte)255 });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "BasePrice", "CategoryId", "CompanyId", "CreatedAt", "DisplayOrder", "IsDeleted", "IsVisible", "LongDescription", "MainImageUrl", "ModifiedAt", "Name", "OfferPrice", "ShortDescription" },
                values: new object[,]
                {
                    { 1, 5500m, 1, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, true, null, null, null, "Ceviche Clásico", null, "Fresco ceviche de corvina con limón y culantro." },
                    { 2, 3500m, 1, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, false, true, null, null, null, "Patacones con Guacamole", 3000m, "Patacones crocantes con guacamole casero." },
                    { 3, 7500m, 2, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, true, null, null, null, "Casado Tradicional", null, "Arroz, frijoles, ensalada, maduro y carne a elegir." },
                    { 4, 12500m, 2, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, false, true, null, null, null, "Lomo al Chimichurri", 10900m, "Lomo de res al punto con salsa chimichurri." },
                    { 5, 8500m, 2, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, false, true, null, null, null, "Bowl Vegano", null, "Quinoa, vegetales asados, hummus y tahini." },
                    { 6, 3200m, 3, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, true, null, null, null, "Tres Leches", null, "Bizcocho esponjoso bañado en tres tipos de leche." },
                    { 7, 3800m, 3, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, false, true, null, null, null, "Brownies con Helado", null, "Brownie de chocolate caliente con helado de vainilla." },
                    { 8, 1500m, 4, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, true, null, null, null, "Café Americano", null, "Café negro de tueste medio." },
                    { 9, 1800m, 4, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, false, true, null, null, null, "Refresco Natural", null, "Cas, tamarindo o guanábana. A elegir." },
                    { 10, 2200m, 5, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, false, true, null, null, null, "Imperial", null, "Cerveza nacional 355ml bien fría." },
                    { 11, 3500m, 5, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, false, true, null, null, null, "Guaro Sour", null, "Guaro Cacique, limón, azúcar y hielo." }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CompanyId_IsDeleted_DisplayOrder",
                table: "Categories",
                columns: new[] { "CompanyId", "IsDeleted", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Slug",
                table: "Companies",
                column: "Slug",
                unique: true);

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
                name: "IX_FooterLinks_CompanyId_IsDeleted_DisplayOrder",
                table: "FooterLinks",
                columns: new[] { "CompanyId", "IsDeleted", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_FooterLinks_StandardIconId",
                table: "FooterLinks",
                column: "StandardIconId");

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
                name: "IX_Products_CompanyId_IsDeleted_DisplayOrder",
                table: "Products",
                columns: new[] { "CompanyId", "IsDeleted", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductTags_TagsId",
                table: "ProductTags",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CompanyId_IsDeleted_ReservationDate",
                table: "Reservations",
                columns: new[] { "CompanyId", "IsDeleted", "ReservationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Settings_CompanyId",
                table: "Settings",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_CompanyId_IsDeleted",
                table: "Tags",
                columns: new[] { "CompanyId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_CompanyId",
                table: "Users",
                column: "CompanyId");

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
                name: "CompanyModules");

            migrationBuilder.DropTable(
                name: "FooterLinks");

            migrationBuilder.DropTable(
                name: "ProductTags");

            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "PlatformModules");

            migrationBuilder.DropTable(
                name: "StandardIcons");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Companies");
        }
    }
}
