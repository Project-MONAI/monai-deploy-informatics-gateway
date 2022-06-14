using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monai.Deploy.InformaticsGateway.Database.Migrations
{
    public partial class R2_020 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UploadedFiles",
                table: "Payload");

            migrationBuilder.DropColumn(
                name: "Workflows",
                table: "Payload");

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "Payload",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllowedSopClasses",
                table: "MonaiApplicationEntities",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedSopClasses",
                table: "MonaiApplicationEntities");

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "Payload",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "UploadedFiles",
                table: "Payload",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Workflows",
                table: "Payload",
                type: "TEXT",
                nullable: true);
        }
    }
}
