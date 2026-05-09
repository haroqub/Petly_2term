using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Petly.DataAccess.Data;

#nullable disable

namespace Petly.DataAccess.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260504120000_AddNeedFulfillmentStatus")]
    public partial class AddNeedFulfillmentStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "fulfilledAt",
                table: "shelterneed",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isFulfilled",
                table: "shelterneed",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fulfilledAt",
                table: "shelterneed");

            migrationBuilder.DropColumn(
                name: "isFulfilled",
                table: "shelterneed");
        }
    }
}
