using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigiMenuAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateViewProductVisibleList : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

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
	            P.ImagePath AS ProductImage,
	            P.Price AS ProductPrice,

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
    }
}
