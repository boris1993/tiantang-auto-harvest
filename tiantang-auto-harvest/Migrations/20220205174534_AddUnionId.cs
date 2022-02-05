using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tiantang_auto_harvest.Migrations
{
    public partial class AddUnionId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UnionId",
                table: "TiantangLoginInfo",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnionId",
                table: "TiantangLoginInfo");
        }
    }
}
