using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tiantang_auto_harvest.Migrations
{
    public partial class CreatedTianTangLoginInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TiantangLoginInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: false),
                    AccessToken = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiantangLoginInfo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TiantangLoginInfo_PhoneNumber",
                table: "TiantangLoginInfo",
                column: "PhoneNumber",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TiantangLoginInfo");
        }
    }
}
