using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigiMenuAPI.Migrations
{
    /// <inheritdoc />
    public partial class CreateGetAllViewsCategory_Subcategory_Product : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eliminar y crear la vista vwGetAllCategories
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'vwGetAllCategories'))
                    DROP VIEW vwGetAllCategories;
            ");

            migrationBuilder.Sql(@"
                CREATE VIEW vwGetAllCategories
                AS
                SELECT 
                    C.Id,
                    C.Label,
                    C.Position,
                    C.IsVisible,
                    CASE 
                        WHEN S.SubcategoryId IS NOT NULL 
			                THEN CAST(1 AS BIT)
			                ELSE CAST(0 AS BIT)
                    END AS HasSubcategory
                FROM 
                    Category C
	                OUTER APPLY (
				                SELECT TOP 1 S.Id AS SubcategoryId
				                FROM Subcategory S
				                WHERE S.CategoryId = C.Id 
					                  AND S.Alive = 1
				                ) S
                WHERE 
                    C.Alive = 1;
            ");

            // Eliminar y crear la vista vwGetAllSubcategories
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'vwGetAllSubcategories'))
                    DROP VIEW vwGetAllSubcategories;
            ");

            migrationBuilder.Sql(@"
                CREATE VIEW vwGetAllSubcategories
                AS
                SELECT 
                    S.Id,
                    S.Label,
                    S.Position,
                    IIF(C.IsVisible = 0, CAST(0 AS BIT), S.IsVisible) AS IsVisible,
                    IIF(P.ProductId IS NOT NULL, CAST(1 AS BIT), CAST(0 AS BIT)) AS HasProduct,
                    --CategoryDto
	                C.Id AS CategoryId,
					C.Label AS CategoryLabel,
					C.Position AS CategoryPosition,
					C.IsVisible AS CategoryIsVisible
                FROM 
                    Subcategory S
					INNER JOIN Category C ON S.CategoryId = C.Id
                    OUTER APPLY (
                        SELECT TOP 1 P.Id AS ProductId
                        FROM Product P
                        WHERE P.SubcategoryId = S.Id 
                          AND P.Alive = 1
                    ) P
                WHERE 
                    S.Alive = 1;
            ");

            // Eliminar y crear la vista vwGetAllProducts
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'vwGetAllProducts'))
                    DROP VIEW vwGetAllProducts;
            ");

            migrationBuilder.Sql(@"
                CREATE VIEW vwGetAllProducts
                AS
                SELECT 
                    P.Id,
                    P.Label,
					P.Price,
					P.ImagePath AS [Image],
                    P.Position,
                    IIF((C.IsVisible = 0 OR S.IsVisible = 0), CAST(0 AS BIT), P.IsVisible) AS IsVisible,
					--SubcategoryDto
					S.Id		AS SubcategoryId,
					S.Label		AS SubcategoryLabel,
					S.Position	AS SubcategoryPosition,
					S.IsVisible AS SubcategoryIsVisible,
					--CategoryDto
	                C.Id		AS CategoryId,
					C.Label		AS CategoryLabel,
					C.Position	AS CategoryPosition,
					C.IsVisible AS CategoryIsVisible
                FROM 
					Product P
                    INNER JOIN Subcategory S ON P.SubcategoryId = S.Id
					INNER JOIN Category C ON S.CategoryId = C.Id
                WHERE 
                    P.Alive = 1;
            ");

            // Crear índice no clúster en Subcategory(CategoryId, Alive)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 
                    FROM sys.indexes 
                    WHERE name = 'IDX_Subcategory_CategoryId_Alive' 
                        AND object_id = OBJECT_ID('Subcategory')
                )
                BEGIN
                    CREATE NONCLUSTERED INDEX IDX_Subcategory_CategoryId_Alive
                    ON Subcategory (CategoryId, Alive);
                END
            ");

            // Crear índice no clúster en Product(SubcategoryId, Alive)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 
                    FROM sys.indexes 
                    WHERE name = 'IDX_Product_SubcategoryId_Alive' 
                      AND object_id = OBJECT_ID('Product')
                )
                BEGIN
                    CREATE NONCLUSTERED INDEX IDX_Product_SubcategoryId_Alive
                    ON Product (SubcategoryId, Alive);
                END
            ");
        }



        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Eliminar las vistas si existe la migración reversa
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'vwGetAllCategories'))
                    DROP VIEW vwGetAllCategories;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'vwGetAllSubcategories'))
                    DROP VIEW vwGetAllSubcategories;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'vwGetAllProducts'))
                    DROP VIEW vwGetAllProducts;
            ");

            // Eliminar índice si existe
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 
                    FROM sys.indexes 
                    WHERE name = 'IDX_Product_SubcategoryId_Alive' 
                      AND object_id = OBJECT_ID('Product')
                )
                BEGIN
                    DROP INDEX IDX_Product_SubcategoryId_Alive ON Product;
                END
            ");

            // Eliminar índice IDX_Subcategory_CategoryId_Alive si existe
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 
                    FROM sys.indexes 
                    WHERE name = 'IDX_Subcategory_CategoryId_Alive' 
                      AND object_id = OBJECT_ID('Subcategory')
                )
                BEGIN
                    DROP INDEX IDX_Subcategory_CategoryId_Alive ON Subcategory;
                END
            ");


        }
    }
}

