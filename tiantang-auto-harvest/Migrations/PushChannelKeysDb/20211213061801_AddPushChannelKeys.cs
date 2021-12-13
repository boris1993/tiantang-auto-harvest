using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tiantang_auto_harvest.Migrations.PushChannelKeysDb
{
    public partial class AddPushChannelKeys : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PushChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServerChanSendKey = table.Column<string>(type: "TEXT", nullable: true),
                    TelegramBotToken = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushChannels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PushChannels_ServerChanSendKey",
                table: "PushChannels",
                column: "ServerChanSendKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushChannels_TelegramBotToken",
                table: "PushChannels",
                column: "TelegramBotToken",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PushChannels");
        }
    }
}
