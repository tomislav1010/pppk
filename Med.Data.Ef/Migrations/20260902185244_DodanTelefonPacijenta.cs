using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Med.Data.Ef.Migrations
{
    /// <inheritdoc />
    public partial class DodanTelefonPacijenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "telefon",
                table: "pacijenti",
                type: "varchar(20)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "telefon",
                table: "pacijenti");
        }
    }
}
