using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigiMenuAPI.Migrations
{

    public partial class CreateViewsProductSubcategoryCategory : Migration
    {

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eliminar y crear la vista vwCategoryVisibleList
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'vwCategoryVisibleList'))
                    DROP VIEW vwCategoryVisibleList;
            ");

            migrationBuilder.Sql(@"
                CREATE VIEW vwCategoryVisibleList
                AS
                SELECT 
                    Id AS CategoryId, 
                    Label AS CategoryLabel, 
                    Position AS CategoryPosition 
                FROM 
                    Category
                WHERE 
                    Alive = 1 
                    AND IsVisible = 1;
            ");

            // Eliminar y crear la vista vwSubcategoryVisibleList
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'vwSubcategoryVisibleList'))
                    DROP VIEW vwSubcategoryVisibleList;
            ");

            migrationBuilder.Sql(@"
                CREATE VIEW vwSubcategoryVisibleList
                AS
                SELECT 
                    Id AS SubcategoryId, 
                    Label AS SubcategoryLabel, 
                    Position AS SubcategoryPosition,
                    CategoryId
                FROM 
                    Subcategory
                WHERE 
                    Alive = 1 
                    AND IsVisible = 1;
            ");

            // Eliminar y crear la vista ProductVisibleList
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'vwProductVisibleList'))
                    DROP VIEW vwProductVisibleList;
            ");

            migrationBuilder.Sql(@"
                CREATE VIEW vwProductVisibleList
                AS
                SELECT 
                    P.Id AS ProductId, 
                    P.Label AS ProductLabel, 
                    P.Position AS ProductPosition,

                    SVL.SubcategoryId,
                    SVL.SubcategoryLabel,
                    SVL.SubcategoryPosition,

                    CVL.CategoryId,
                    CVL.CategoryLabel,
                    CVL.CategoryPosition
                FROM 
                    Product P
                    INNER JOIN vwSubcategoryVisibleList SVL ON SVL.SubcategoryId = P.SubcategoryId
                    INNER JOIN vwCategoryVisibleList CVL ON CVL.CategoryId = SVL.CategoryId
                WHERE 
                    Alive = 1 
                    AND IsVisible = 1;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Eliminar las vistas si existe la migración reversa
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'vwCategoryVisibleList'))
                    DROP VIEW vwCategoryVisibleList;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'vwSubcategoryVisibleList'))
                    DROP VIEW vwSubcategoryVisibleList;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'vwProductVisibleList'))
                    DROP VIEW vwProductVisibleList;
            ");
        }
    }
}
