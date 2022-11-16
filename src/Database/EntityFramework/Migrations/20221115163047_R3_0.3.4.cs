using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monai.Deploy.InformaticsGateway.Database.Migrations
{
    public partial class R3_034 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StorageMetadataWrapper");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Payload",
                table: "Payload");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InferenceRequest",
                table: "InferenceRequest");

            migrationBuilder.RenameTable(
                name: "Payload",
                newName: "Payloads");

            migrationBuilder.RenameTable(
                name: "InferenceRequest",
                newName: "InferenceRequests");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Payloads",
                newName: "PayloadId");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateTimeCreated",
                table: "SourceApplicationEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DateTimeCreated",
                table: "MonaiApplicationEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DateTimeCreated",
                table: "DestinationApplicationEntities",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DateTimeCreated",
                table: "InferenceRequests",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_Payloads",
                table: "Payloads",
                column: "PayloadId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InferenceRequests",
                table: "InferenceRequests",
                column: "InferenceRequestId");

            migrationBuilder.CreateTable(
                name: "StorageMetadataWrapperEntities",
                columns: table => new
                {
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: false),
                    Identity = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    TypeName = table.Column<string>(type: "TEXT", nullable: false),
                    IsUploaded = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateTimeCreated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageMetadataWrapperEntities", x => new { x.CorrelationId, x.Identity });
                });

            migrationBuilder.CreateIndex(
                name: "idx_storagemetadata_correlation",
                table: "StorageMetadataWrapperEntities",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "idx_storagemetadata_ids",
                table: "StorageMetadataWrapperEntities",
                columns: new[] { "CorrelationId", "Identity" });

            migrationBuilder.CreateIndex(
                name: "idx_storagemetadata_uploaded",
                table: "StorageMetadataWrapperEntities",
                column: "IsUploaded");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StorageMetadataWrapperEntities");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Payloads",
                table: "Payloads");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InferenceRequests",
                table: "InferenceRequests");

            migrationBuilder.DropColumn(
                name: "DateTimeCreated",
                table: "SourceApplicationEntities");

            migrationBuilder.DropColumn(
                name: "DateTimeCreated",
                table: "MonaiApplicationEntities");

            migrationBuilder.DropColumn(
                name: "DateTimeCreated",
                table: "DestinationApplicationEntities");

            migrationBuilder.DropColumn(
                name: "DateTimeCreated",
                table: "InferenceRequests");

            migrationBuilder.RenameTable(
                name: "Payloads",
                newName: "Payload");

            migrationBuilder.RenameTable(
                name: "InferenceRequests",
                newName: "InferenceRequest");

            migrationBuilder.RenameColumn(
                name: "PayloadId",
                table: "Payload",
                newName: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Payload",
                table: "Payload",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InferenceRequest",
                table: "InferenceRequest",
                column: "InferenceRequestId");

            migrationBuilder.CreateTable(
                name: "StorageMetadataWrapper",
                columns: table => new
                {
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: false),
                    Identity = table.Column<string>(type: "TEXT", nullable: false),
                    IsUploaded = table.Column<bool>(type: "INTEGER", nullable: false),
                    TypeName = table.Column<string>(type: "TEXT", nullable: true),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageMetadataWrapper", x => new { x.CorrelationId, x.Identity });
                });

            migrationBuilder.CreateIndex(
                name: "idx_storagemetadata_correlation",
                table: "StorageMetadataWrapper",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "idx_storagemetadata_ids",
                table: "StorageMetadataWrapper",
                columns: new[] { "CorrelationId", "Identity" });

            migrationBuilder.CreateIndex(
                name: "idx_storagemetadata_uploaded",
                table: "StorageMetadataWrapper",
                column: "IsUploaded");
        }
    }
}
