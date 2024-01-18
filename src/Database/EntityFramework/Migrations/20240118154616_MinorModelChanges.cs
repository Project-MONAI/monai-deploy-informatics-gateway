using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monai.Deploy.InformaticsGateway.Database.Migrations
{
    /// <inheritdoc />
    public partial class MinorModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_source_all_HL7Destination",
                table: "HL7DestinationEntities");

            migrationBuilder.DropColumn(
                name: "DestinationFolder",
                table: "Payloads");

            migrationBuilder.DropColumn(
                name: "AeTitle",
                table: "HL7DestinationEntities");

            migrationBuilder.CreateIndex(
                name: "idx_source_all_HL7Destination",
                table: "HL7DestinationEntities",
                columns: new[] { "Name", "HostIp", "Port" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_source_all_HL7Destination",
                table: "HL7DestinationEntities");

            migrationBuilder.AddColumn<string>(
                name: "DestinationFolder",
                table: "Payloads",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AeTitle",
                table: "HL7DestinationEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "idx_source_all_HL7Destination",
                table: "HL7DestinationEntities",
                columns: new[] { "Name", "AeTitle", "HostIp", "Port" },
                unique: true);
        }
    }
}
