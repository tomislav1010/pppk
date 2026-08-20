using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Med.Data.Ef.Migrations
{
    /// <inheritdoc />
    public partial class Pocetna : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "adrese",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ulica = table.Column<string>(type: "varchar(120)", nullable: false),
                    kucni_broj = table.Column<string>(type: "varchar(10)", nullable: true),
                    grad = table.Column<string>(type: "varchar(80)", nullable: false),
                    postanski_broj = table.Column<string>(type: "char(5)", nullable: true),
                    drzava = table.Column<string>(type: "varchar(60)", nullable: false, defaultValue: "Hrvatska")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adrese", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dijagnoze",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sifra = table.Column<string>(type: "varchar(10)", nullable: false),
                    naziv = table.Column<string>(type: "varchar(200)", nullable: false),
                    opis = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dijagnoze", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lijecnici",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ime = table.Column<string>(type: "varchar(60)", nullable: false),
                    prezime = table.Column<string>(type: "varchar(80)", nullable: false),
                    specijalizacija = table.Column<string>(type: "varchar(100)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lijecnici", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lijekovi",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    naziv = table.Column<string>(type: "varchar(150)", nullable: false),
                    atc_kod = table.Column<string>(type: "varchar(10)", nullable: true),
                    oblik = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lijekovi", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pacijenti",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ime = table.Column<string>(type: "varchar(60)", nullable: false),
                    prezime = table.Column<string>(type: "varchar(80)", nullable: false),
                    oib = table.Column<string>(type: "char(11)", nullable: false),
                    datum_rodenja = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    spol = table.Column<char>(type: "char(1)", nullable: false),
                    adresa_boravista_id = table.Column<int>(type: "integer", nullable: false),
                    adresa_prebivalista_id = table.Column<int>(type: "integer", nullable: true),
                    kreirano_na = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pacijenti", x => x.id);
                    table.ForeignKey(
                        name: "FK_pacijenti_adrese_adresa_boravista_id",
                        column: x => x.adresa_boravista_id,
                        principalTable: "adrese",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pacijenti_adrese_adresa_prebivalista_id",
                        column: x => x.adresa_prebivalista_id,
                        principalTable: "adrese",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "kartoni_pacijenata",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pacijent_id = table.Column<int>(type: "integer", nullable: false),
                    krvna_grupa = table.Column<string>(type: "char(3)", nullable: true),
                    visina_cm = table.Column<double>(type: "double precision", nullable: true),
                    tezina_kg = table.Column<double>(type: "double precision", nullable: true),
                    alergije = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kartoni_pacijenata", x => x.id);
                    table.ForeignKey(
                        name: "FK_kartoni_pacijenata_pacijenti_pacijent_id",
                        column: x => x.pacijent_id,
                        principalTable: "pacijenti",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "povijest_bolesti",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pacijent_id = table.Column<int>(type: "integer", nullable: false),
                    dijagnoza_id = table.Column<int>(type: "integer", nullable: false),
                    lijecnik_id = table.Column<int>(type: "integer", nullable: false),
                    datum_od = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    datum_do = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    napomena = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_povijest_bolesti", x => x.id);
                    table.ForeignKey(
                        name: "FK_povijest_bolesti_dijagnoze_dijagnoza_id",
                        column: x => x.dijagnoza_id,
                        principalTable: "dijagnoze",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_povijest_bolesti_lijecnici_lijecnik_id",
                        column: x => x.lijecnik_id,
                        principalTable: "lijecnici",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_povijest_bolesti_pacijenti_pacijent_id",
                        column: x => x.pacijent_id,
                        principalTable: "pacijenti",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pregledi",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pacijent_id = table.Column<int>(type: "integer", nullable: false),
                    lijecnik_id = table.Column<int>(type: "integer", nullable: false),
                    uputitelj_id = table.Column<int>(type: "integer", nullable: true),
                    tip = table.Column<string>(type: "varchar(10)", nullable: false),
                    termin = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    trajanje_minuta = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    status = table.Column<string>(type: "varchar(20)", nullable: false, defaultValueSql: "'Zakazan'"),
                    nalaz = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pregledi", x => x.id);
                    table.ForeignKey(
                        name: "FK_pregledi_lijecnici_lijecnik_id",
                        column: x => x.lijecnik_id,
                        principalTable: "lijecnici",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pregledi_lijecnici_uputitelj_id",
                        column: x => x.uputitelj_id,
                        principalTable: "lijecnici",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_pregledi_pacijenti_pacijent_id",
                        column: x => x.pacijent_id,
                        principalTable: "pacijenti",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "terapije",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pacijent_id = table.Column<int>(type: "integer", nullable: false),
                    lijek_id = table.Column<int>(type: "integer", nullable: false),
                    povijest_bolesti_id = table.Column<int>(type: "integer", nullable: true),
                    lijecnik_id = table.Column<int>(type: "integer", nullable: false),
                    doza = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    jedinica_doze = table.Column<string>(type: "varchar(20)", nullable: false),
                    ucestalost = table.Column<string>(type: "varchar(60)", nullable: false),
                    datum_od = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    datum_do = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    aktivna = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_terapije", x => x.id);
                    table.ForeignKey(
                        name: "FK_terapije_lijecnici_lijecnik_id",
                        column: x => x.lijecnik_id,
                        principalTable: "lijecnici",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_terapije_lijekovi_lijek_id",
                        column: x => x.lijek_id,
                        principalTable: "lijekovi",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_terapije_pacijenti_pacijent_id",
                        column: x => x.pacijent_id,
                        principalTable: "pacijenti",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_terapije_povijest_bolesti_povijest_bolesti_id",
                        column: x => x.povijest_bolesti_id,
                        principalTable: "povijest_bolesti",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dijagnoze_sifra",
                table: "dijagnoze",
                column: "sifra",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_kartoni_pacijenata_pacijent_id",
                table: "kartoni_pacijenata",
                column: "pacijent_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pacijenti_adresa_boravista_id",
                table: "pacijenti",
                column: "adresa_boravista_id");

            migrationBuilder.CreateIndex(
                name: "IX_pacijenti_adresa_prebivalista_id",
                table: "pacijenti",
                column: "adresa_prebivalista_id");

            migrationBuilder.CreateIndex(
                name: "IX_pacijenti_oib",
                table: "pacijenti",
                column: "oib",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_povijest_bolesti_dijagnoza_id",
                table: "povijest_bolesti",
                column: "dijagnoza_id");

            migrationBuilder.CreateIndex(
                name: "IX_povijest_bolesti_lijecnik_id",
                table: "povijest_bolesti",
                column: "lijecnik_id");

            migrationBuilder.CreateIndex(
                name: "IX_povijest_bolesti_pacijent_id",
                table: "povijest_bolesti",
                column: "pacijent_id");

            migrationBuilder.CreateIndex(
                name: "IX_pregledi_lijecnik_id",
                table: "pregledi",
                column: "lijecnik_id");

            migrationBuilder.CreateIndex(
                name: "IX_pregledi_pacijent_id",
                table: "pregledi",
                column: "pacijent_id");

            migrationBuilder.CreateIndex(
                name: "IX_pregledi_uputitelj_id",
                table: "pregledi",
                column: "uputitelj_id");

            migrationBuilder.CreateIndex(
                name: "IX_terapije_lijecnik_id",
                table: "terapije",
                column: "lijecnik_id");

            migrationBuilder.CreateIndex(
                name: "IX_terapije_lijek_id",
                table: "terapije",
                column: "lijek_id");

            migrationBuilder.CreateIndex(
                name: "IX_terapije_pacijent_id",
                table: "terapije",
                column: "pacijent_id");

            migrationBuilder.CreateIndex(
                name: "IX_terapije_povijest_bolesti_id",
                table: "terapije",
                column: "povijest_bolesti_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "kartoni_pacijenata");

            migrationBuilder.DropTable(
                name: "pregledi");

            migrationBuilder.DropTable(
                name: "terapije");

            migrationBuilder.DropTable(
                name: "lijekovi");

            migrationBuilder.DropTable(
                name: "povijest_bolesti");

            migrationBuilder.DropTable(
                name: "dijagnoze");

            migrationBuilder.DropTable(
                name: "lijecnici");

            migrationBuilder.DropTable(
                name: "pacijenti");

            migrationBuilder.DropTable(
                name: "adrese");
        }
    }
}
