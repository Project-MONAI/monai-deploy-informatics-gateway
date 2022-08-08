using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monai.Deploy.InformaticsGateway.Database.Migrations
{
    public partial class R3_030 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StoragePath",
                table: "InferenceRequest");

            migrationBuilder.CreateTable(
                name: "StorageMetadataWrapper",
                columns: table => new
                {
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: false),
                    Identity = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    TypeName = table.Column<string>(type: "TEXT", nullable: true),
                    IsUploaded = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageMetadataWrapper", x => new { x.CorrelationId, x.Identity });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StorageMetadataWrapper");

            migrationBuilder.AddColumn<string>(
                name: "StoragePath",
                table: "InferenceRequest",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
