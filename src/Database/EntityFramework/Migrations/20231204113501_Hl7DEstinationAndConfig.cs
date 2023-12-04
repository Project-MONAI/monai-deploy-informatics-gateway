using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monai.Deploy.InformaticsGateway.Database.Migrations
{
    public partial class Hl7DEstinationAndConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Hl7ApplicationConfig",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    SendingId = table.Column<string>(type: "TEXT", nullable: false),
                    DataLink = table.Column<string>(type: "TEXT", nullable: false),
                    DataMapping = table.Column<string>(type: "TEXT", nullable: false),
                    PlugInAssemblies = table.Column<string>(type: "TEXT", nullable: false),
                    DateTimeCreated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hl7ApplicationConfig", x => x.Name);
                });

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
                name: "idx_hl7_name",
                table: "Hl7ApplicationConfig",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_destination_name1",
                table: "HL7DestinationEntities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_source_all_HL7Destination",
                table: "HL7DestinationEntities",
                columns: new[] { "Name", "AeTitle", "HostIp", "Port" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Hl7ApplicationConfig");

            migrationBuilder.DropTable(
                name: "HL7DestinationEntities");

            migrationBuilder.DropIndex(
                name: "idx_source_all_HL7Destination",
                table: "HL7DestinationEntities");
        }
    }
}
