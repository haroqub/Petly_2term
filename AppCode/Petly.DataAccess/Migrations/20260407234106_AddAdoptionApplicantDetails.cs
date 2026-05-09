using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Petly.DataAccess.Migrations
{
    public partial class AddAdoptionApplicantDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "applicantAge",
                table: "adoptionapplication",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "applicantName",
                table: "adoptionapplication",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "applicantSurname",
                table: "adoptionapplication",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "contactInfo",
                table: "adoptionapplication",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "applicantAge",
                table: "adoptionapplication");

            migrationBuilder.DropColumn(
                name: "applicantName",
                table: "adoptionapplication");

            migrationBuilder.DropColumn(
                name: "applicantSurname",
                table: "adoptionapplication");

            migrationBuilder.DropColumn(
                name: "contactInfo",
                table: "adoptionapplication");
        }
    }
}
