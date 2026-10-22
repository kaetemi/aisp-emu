using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aisp.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddGachaMachines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GachaMachineId",
                table: "Npcs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GachaMachines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    VisualId = table.Column<long>(type: "INTEGER", nullable: false),
                    AiPrice = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    NicoPrice = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    TicketItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    TicketsPerPull = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GachaMachines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GachaPrizes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GachaMachineId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    HitType = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GachaPrizes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GachaPrizes_GachaMachines_GachaMachineId",
                        column: x => x.GachaMachineId,
                        principalTable: "GachaMachines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Npcs_GachaMachineId",
                table: "Npcs",
                column: "GachaMachineId");

            migrationBuilder.CreateIndex(
                name: "IX_GachaMachines_Code",
                table: "GachaMachines",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GachaPrizes_GachaMachineId_ItemId",
                table: "GachaPrizes",
                columns: new[] { "GachaMachineId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GachaPrizes_GachaMachineId_SortOrder",
                table: "GachaPrizes",
                columns: new[] { "GachaMachineId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_Npcs_GachaMachines_GachaMachineId",
                table: "Npcs",
                column: "GachaMachineId",
                principalTable: "GachaMachines",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Npcs_GachaMachines_GachaMachineId",
                table: "Npcs");

            migrationBuilder.DropTable(
                name: "GachaPrizes");

            migrationBuilder.DropTable(
                name: "GachaMachines");

            migrationBuilder.DropIndex(
                name: "IX_Npcs_GachaMachineId",
                table: "Npcs");

            migrationBuilder.DropColumn(
                name: "GachaMachineId",
                table: "Npcs");
        }
    }
}
