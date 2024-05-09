using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Licenta3.Migrations
{
    public partial class lateStart : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LateStart",
                table: "Tasks",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LateStart",
                table: "Tasks");
        }
    }
}
