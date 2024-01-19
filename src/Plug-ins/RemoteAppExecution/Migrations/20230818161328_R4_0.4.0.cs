using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.Migrations
{
    public partial class R4_040 : Migration
    {
        private static readonly string[] Columns = ["WorkflowInstanceId", "ExportTaskId", "StudyInstanceUid"];
        private static readonly string[] StudyColumns = ["WorkflowInstanceId", "ExportTaskId", "StudyInstanceUid", "SeriesInstanceUid"];

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RemoteAppExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequestTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "TEXT", nullable: false),
                    ExportTaskId = table.Column<string>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: false),
                    StudyInstanceUid = table.Column<string>(type: "TEXT", nullable: false),
                    SeriesInstanceUid = table.Column<string>(type: "TEXT", nullable: false),
                    SopInstanceUid = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalValues = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemoteAppExecutions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_remoteapp_all",
                table: "RemoteAppExecutions",
                columns: StudyColumns);

            migrationBuilder.CreateIndex(
                name: "idx_remoteapp_instance",
                table: "RemoteAppExecutions",
                column: "SopInstanceUid");

            migrationBuilder.CreateIndex(
                name: "idx_remoteapp_study",
                table: "RemoteAppExecutions",
                columns: Columns);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RemoteAppExecutions");
        }
    }
}
