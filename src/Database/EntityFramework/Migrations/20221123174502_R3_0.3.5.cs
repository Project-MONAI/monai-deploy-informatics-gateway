using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monai.Deploy.InformaticsGateway.Database.Migrations
{
    public partial class R3_035 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DicomAssociationHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DateTimeDisconnected = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: false),
                    FileCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CallingAeTitle = table.Column<string>(type: "TEXT", nullable: true),
                    CalledAeTitle = table.Column<string>(type: "TEXT", nullable: false),
                    RemoteHost = table.Column<string>(type: "TEXT", nullable: false),
                    RemotePort = table.Column<int>(type: "INTEGER", nullable: false),
                    Errors = table.Column<string>(type: "TEXT", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    DateTimeCreated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DicomAssociationHistories", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DicomAssociationHistories");
        }
    }
}
