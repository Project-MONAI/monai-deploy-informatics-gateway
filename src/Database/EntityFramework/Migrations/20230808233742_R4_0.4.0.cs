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
                name: "PluginAssemblies",
                table: "MonaiApplicationEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "Payloads");

            migrationBuilder.DropColumn(
                name: "WorkflowInstanceId",
                table: "Payloads");

            migrationBuilder.DropColumn(
                name: "PluginAssemblies",
                table: "MonaiApplicationEntities");
        }
    }
}
