using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monai.Deploy.InformaticsGateway.Database.Migrations
{
    public partial class R3_0315 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateTimeUpdated",
                table: "MonaiApplicationEntities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "MonaiApplicationEntities",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateTimeUpdated",
                table: "MonaiApplicationEntities");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "MonaiApplicationEntities");
        }
    }
}
