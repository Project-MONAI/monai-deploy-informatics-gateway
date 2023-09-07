using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monai.Deploy.InformaticsGateway.Database.Migrations
{
    public partial class R4_041 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayloadIds",
                table: "DicomAssociationHistories",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayloadIds",
                table: "DicomAssociationHistories");
        }
    }
}
