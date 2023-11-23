using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monai.Deploy.InformaticsGateway.Database.Migrations
{
    public partial class addHl7Config : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataKeyValuePair",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataKeyValuePair", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Hl7ApplicationConfig",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SendingIdKey = table.Column<string>(type: "TEXT", nullable: false),
                    DataLinkKey = table.Column<string>(type: "TEXT", nullable: false),
                    DateTimeCreated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hl7ApplicationConfig", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hl7ApplicationConfig_DataKeyValuePair_DataLinkKey",
                        column: x => x.DataLinkKey,
                        principalTable: "DataKeyValuePair",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StringKeyValuePair",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    Hl7ApplicationConfigEntityId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StringKeyValuePair", x => x.Key);
                    table.ForeignKey(
                        name: "FK_StringKeyValuePair_Hl7ApplicationConfig_Hl7ApplicationConfigEntityId",
                        column: x => x.Hl7ApplicationConfigEntityId,
                        principalTable: "Hl7ApplicationConfig",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Hl7ApplicationConfig_DataLinkKey",
                table: "Hl7ApplicationConfig",
                column: "DataLinkKey");

            migrationBuilder.CreateIndex(
                name: "IX_Hl7ApplicationConfig_SendingIdKey",
                table: "Hl7ApplicationConfig",
                column: "SendingIdKey");

            migrationBuilder.CreateIndex(
                name: "IX_StringKeyValuePair_Hl7ApplicationConfigEntityId",
                table: "StringKeyValuePair",
                column: "Hl7ApplicationConfigEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Hl7ApplicationConfig_StringKeyValuePair_SendingIdKey",
                table: "Hl7ApplicationConfig",
                column: "SendingIdKey",
                principalTable: "StringKeyValuePair",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Hl7ApplicationConfig_DataKeyValuePair_DataLinkKey",
                table: "Hl7ApplicationConfig");

            migrationBuilder.DropForeignKey(
                name: "FK_Hl7ApplicationConfig_StringKeyValuePair_SendingIdKey",
                table: "Hl7ApplicationConfig");

            migrationBuilder.DropTable(
                name: "DataKeyValuePair");

            migrationBuilder.DropTable(
                name: "StringKeyValuePair");

            migrationBuilder.DropTable(
                name: "Hl7ApplicationConfig");
        }
    }
}
