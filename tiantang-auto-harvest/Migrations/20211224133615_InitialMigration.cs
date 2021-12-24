using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tiantang_auto_harvest.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PushChannelKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServiceName = table.Column<string>(type: "TEXT", nullable: true),
                    Token = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushChannelKeys", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "UnsendNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Content = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnsendNotifications", x => x.Id);
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
                name: "PushChannelKeys");

            migrationBuilder.DropTable(
                name: "TiantangLoginInfo");

            migrationBuilder.DropTable(
                name: "UnsendNotifications");
        }
    }
}
