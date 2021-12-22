using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tiantang_auto_harvest.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProxySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Protocol = table.Column<string>(type: "TEXT", nullable: false),
                    Host = table.Column<string>(type: "TEXT", nullable: false),
                    Port = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProxySettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PushChannelKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServerChanSendKey = table.Column<string>(type: "TEXT", nullable: true),
                    TelegramBotToken = table.Column<string>(type: "TEXT", nullable: true),
                    IsProxyNeeded = table.Column<bool>(type: "INTEGER", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_PushChannelKeys_ServerChanSendKey",
                table: "PushChannelKeys",
                column: "ServerChanSendKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushChannelKeys_TelegramBotToken",
                table: "PushChannelKeys",
                column: "TelegramBotToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TiantangLoginInfo_PhoneNumber",
                table: "TiantangLoginInfo",
                column: "PhoneNumber",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProxySettings");

            migrationBuilder.DropTable(
                name: "PushChannelKeys");

            migrationBuilder.DropTable(
                name: "TiantangLoginInfo");
        }
    }
}
