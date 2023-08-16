using System;
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

            migrationBuilder.CreateTable(
                name: "RemoteAppExecutions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    RequestTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExportTaskId = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: false),
                    StudyUid = table.Column<string>(type: "TEXT", nullable: true),
                    OutgoingUid = table.Column<string>(type: "TEXT", nullable: false),
                    Files = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalValues = table.Column<string>(type: "TEXT", nullable: false),
                    ProxyValues = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemoteAppExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VirtualApplicationEntities",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    VirtualAeTitle = table.Column<string>(type: "TEXT", nullable: false),
                    Workflows = table.Column<string>(type: "TEXT", nullable: false),
                    PluginAssemblies = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    DateTimeUpdated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateTimeCreated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirtualApplicationEntities", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "DestinationApplicationEntityRemoteAppExecution",
                columns: table => new
                {
                    ExportDetailsName = table.Column<string>(type: "TEXT", nullable: false),
                    RemoteAppExecutionsId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestinationApplicationEntityRemoteAppExecution", x => new { x.ExportDetailsName, x.RemoteAppExecutionsId });
                    table.ForeignKey(
                        name: "FK_DestinationApplicationEntityRemoteAppExecution_DestinationApplicationEntities_ExportDetailsName",
                        column: x => x.ExportDetailsName,
                        principalTable: "DestinationApplicationEntities",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DestinationApplicationEntityRemoteAppExecution_RemoteAppExecutions_RemoteAppExecutionsId",
                        column: x => x.RemoteAppExecutionsId,
                        principalTable: "RemoteAppExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RemtoeAppExecutionDestinations",
                columns: table => new
                {
                    DestinationApplicationEntityName = table.Column<string>(type: "TEXT", nullable: false),
                    RemoteAppExecutionId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemtoeAppExecutionDestinations", x => new { x.DestinationApplicationEntityName, x.RemoteAppExecutionId });
                    table.ForeignKey(
                        name: "FK_RemtoeAppExecutionDestinations_DestinationApplicationEntities_DestinationApplicationEntityName",
                        column: x => x.DestinationApplicationEntityName,
                        principalTable: "DestinationApplicationEntities",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RemtoeAppExecutionDestinations_RemoteAppExecutions_RemoteAppExecutionId",
                        column: x => x.RemoteAppExecutionId,
                        principalTable: "RemoteAppExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DestinationApplicationEntityRemoteAppExecution_RemoteAppExecutionsId",
                table: "DestinationApplicationEntityRemoteAppExecution",
                column: "RemoteAppExecutionsId");

            migrationBuilder.CreateIndex(
                name: "idx_outgoing_key",
                table: "RemoteAppExecutions",
                column: "OutgoingUid");

            migrationBuilder.CreateIndex(
                name: "IX_RemtoeAppExecutionDestinations_RemoteAppExecutionId",
                table: "RemtoeAppExecutionDestinations",
                column: "RemoteAppExecutionId");

            migrationBuilder.CreateIndex(
                name: "idx_virtualae_name",
                table: "VirtualApplicationEntities",
                column: "Name",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DestinationApplicationEntityRemoteAppExecution");

            migrationBuilder.DropTable(
                name: "RemtoeAppExecutionDestinations");

            migrationBuilder.DropTable(
                name: "VirtualApplicationEntities");

            migrationBuilder.DropTable(
                name: "RemoteAppExecutions");

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
