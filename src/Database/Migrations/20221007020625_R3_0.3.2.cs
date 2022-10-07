using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monai.Deploy.InformaticsGateway.Database.Migrations
{
    public partial class R3_032 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "idx_storagemetadata_correlation",
                table: "StorageMetadataWrapper",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_storagemetadata_ids",
                table: "StorageMetadataWrapper",
                columns: new[] { "CorrelationId", "Identity" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_storagemetadata_uploaded",
                table: "StorageMetadataWrapper",
                column: "IsUploaded");

            migrationBuilder.CreateIndex(
                name: "idx_source_all1",
                table: "SourceApplicationEntities",
                columns: new[] { "Name", "AeTitle", "HostIp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_source_name",
                table: "SourceApplicationEntities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_payload_ids",
                table: "Payload",
                columns: new[] { "CorrelationId", "Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_payload_state",
                table: "Payload",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "idx_monaiae_name",
                table: "MonaiApplicationEntities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_inferencerequest_inferencerequestid",
                table: "InferenceRequest",
                column: "InferenceRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_inferencerequest_state",
                table: "InferenceRequest",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "idx_inferencerequest_transactionid",
                table: "InferenceRequest",
                column: "TransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_destination_name",
                table: "DestinationApplicationEntities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_source_all",
                table: "DestinationApplicationEntities",
                columns: new[] { "Name", "AeTitle", "HostIp", "Port" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_storagemetadata_correlation",
                table: "StorageMetadataWrapper");

            migrationBuilder.DropIndex(
                name: "idx_storagemetadata_ids",
                table: "StorageMetadataWrapper");

            migrationBuilder.DropIndex(
                name: "idx_storagemetadata_uploaded",
                table: "StorageMetadataWrapper");

            migrationBuilder.DropIndex(
                name: "idx_source_all1",
                table: "SourceApplicationEntities");

            migrationBuilder.DropIndex(
                name: "idx_source_name",
                table: "SourceApplicationEntities");

            migrationBuilder.DropIndex(
                name: "idx_payload_ids",
                table: "Payload");

            migrationBuilder.DropIndex(
                name: "idx_payload_state",
                table: "Payload");

            migrationBuilder.DropIndex(
                name: "idx_monaiae_name",
                table: "MonaiApplicationEntities");

            migrationBuilder.DropIndex(
                name: "idx_inferencerequest_inferencerequestid",
                table: "InferenceRequest");

            migrationBuilder.DropIndex(
                name: "idx_inferencerequest_state",
                table: "InferenceRequest");

            migrationBuilder.DropIndex(
                name: "idx_inferencerequest_transactionid",
                table: "InferenceRequest");

            migrationBuilder.DropIndex(
                name: "idx_destination_name",
                table: "DestinationApplicationEntities");

            migrationBuilder.DropIndex(
                name: "idx_source_all",
                table: "DestinationApplicationEntities");
        }
    }
}
