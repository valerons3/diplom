using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBackend.Migrations
{
    /// <inheritdoc />
    public partial class ChandgeProccessDataTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AtWork",
                table: "ProccesedDatas");

            migrationBuilder.AddColumn<string>(
                name: "CommentResult",
                table: "ProccesedDatas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessingTime",
                table: "ProccesedDatas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ProccesedDatas",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommentResult",
                table: "ProccesedDatas");

            migrationBuilder.DropColumn(
                name: "ProcessingTime",
                table: "ProccesedDatas");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ProccesedDatas");

            migrationBuilder.AddColumn<bool>(
                name: "AtWork",
                table: "ProccesedDatas",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
