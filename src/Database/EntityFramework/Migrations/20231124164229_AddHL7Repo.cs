using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monai.Deploy.InformaticsGateway.Database.Migrations
{
    public partial class AddHL7Repo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "idx_source_all1",
                table: "SourceApplicationEntities",
                newName: "idx_source_all2");

            migrationBuilder.CreateTable(
                name: "HL7DestinationEntities",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    DateTimeCreated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AeTitle = table.Column<string>(type: "TEXT", nullable: false),
                    HostIp = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    DateTimeUpdated = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HL7DestinationEntities", x => x.Name);
                });

            migrationBuilder.CreateIndex(
                name: "idx_destination_name1",
                table: "HL7DestinationEntities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_source_all1",
                table: "HL7DestinationEntities",
                columns: new[] { "Name", "AeTitle", "HostIp", "Port" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HL7DestinationEntities");

            migrationBuilder.RenameIndex(
                name: "idx_source_all2",
                table: "SourceApplicationEntities",
                newName: "idx_source_all1");
        }
    }
}
