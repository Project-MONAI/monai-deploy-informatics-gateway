/*
 * Copyright 2022 MONAI Consortium
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

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
