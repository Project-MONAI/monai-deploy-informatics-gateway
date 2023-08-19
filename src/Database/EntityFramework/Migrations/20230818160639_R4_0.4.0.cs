using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monai.Deploy.InformaticsGateway.Database.Migrations
{
    public partial class R4_040 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TaskId",
                table: "Payloads",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkflowInstanceId",
                table: "Payloads",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlugInAssemblies",
                table: "MonaiApplicationEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "VirtualApplicationEntities",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    VirtualAeTitle = table.Column<string>(type: "TEXT", nullable: false),
                    Workflows = table.Column<string>(type: "TEXT", nullable: false),
                    PlugInAssemblies = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    DateTimeUpdated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateTimeCreated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirtualApplicationEntities", x => x.Name);
                });

            migrationBuilder.CreateIndex(
                name: "idx_virtualae_name",
                table: "VirtualApplicationEntities",
                column: "Name",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VirtualApplicationEntities");

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "Payloads");

            migrationBuilder.DropColumn(
                name: "WorkflowInstanceId",
                table: "Payloads");

            migrationBuilder.DropColumn(
                name: "PlugInAssemblies",
                table: "MonaiApplicationEntities");
        }
    }
}
