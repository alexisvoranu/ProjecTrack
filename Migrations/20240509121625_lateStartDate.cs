using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Licenta3.Migrations
{
    public partial class lateStartDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LateStart",
                table: "Tasks");

            migrationBuilder.AddColumn<DateTime>(
                name: "LateStartDate",
                table: "Tasks",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LateStartDate",
                table: "Tasks");

            migrationBuilder.AddColumn<decimal>(
                name: "LateStart",
                table: "Tasks",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
