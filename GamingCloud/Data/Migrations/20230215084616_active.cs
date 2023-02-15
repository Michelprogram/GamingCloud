using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamingCloud.Data
{
    /// <inheritdoc />
    public partial class active : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DbSet",
                table: "DbSet");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "DbSet",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<bool>(
                name: "active",
                table: "DbSet",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DbSet",
                table: "DbSet",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DbSet",
                table: "DbSet");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "DbSet");

            migrationBuilder.DropColumn(
                name: "active",
                table: "DbSet");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DbSet",
                table: "DbSet",
                column: "name");
        }
    }
}
