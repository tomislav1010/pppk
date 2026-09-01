# HANDOFF — Projektni zadatak "Pristup podacima iz programskog koda"

Dokument za nastavak rada. Sadrži kontekst, dosadašnji napredak, trenutno stanje i preostale milestone.

---

## 1. Kontekst

**Kolegij:** Pristup podacima iz programskog koda (Algebra, nastavnik Borna Skračić)
**Ukupno:** 100 bodova kroz 5 ishoda učenja
**Rok:** projekt se predaje **tri dana prije** roka definiranog na IE
**Predaja:** GitHub repozitorij, obavezna uredna Git povijest
**Obrana:** usmena, na ispitnom roku

### Dva projekta

| Projekt | Ishodi | Bodovi | Tema |
|---|---|---|---|
| Projekt 1 | I1, I4, I5 | 60 | Medicinski sustav + ORM nad PostgreSQL |
| Projekt 2 | I2, I3 | 40 | Pipeline za opažanja i taksonomiju ptica |

### Obavezni uvjeti iz zadatka

- Postgres, MongoDB i MinIO **moraju** biti Docker kontejneri (cloud konfiguracija se smije ignorirati)
- Liječnici se **ne** uređuju kroz CRUD — unose se samo pri prvom pokretanju aplikacije
- CRUD nad svim ostalim entitetima
- Code-first pristup

---

## 2. Strateška odluka

**Prvo EF Core, vlastiti ORM kasnije.**

Gotov ORM otključava samo minimalne ishode → strop 70 bodova. Vlastiti ORM otvara raspon do 100. Odluka je: prvo zaključati 70 bodova s EF Coreom i Projektom 2, pa tek onda pisati vlastiti ORM s preostalim vremenom.

Zbog toga arhitektura mora dopustiti zamjenu sloja pristupa podacima bez diranja aplikacije:

```
Med.Domain      entiteti + IRepozitorij<T>       (ne zna za EF)
Med.Data.Ef     MedDbContext + EfRepozitorij     (EF implementacija)
Med.Data.Orm    ista sučelja nad vlastitim ORM-om (NASTAJE KASNIJE, dan 12)
Med.App         vidi samo IRepozitorij
```

**Pravila koja iz toga slijede — ne kršiti:**

1. Entitetske klase nose **isključivo vlastite atribute** iz `Med.ORM.Mapping`. Nijedna EF anotacija ne smije na klase.
2. Sva EF konfiguracija ide Fluent API-jem u `Konfiguracije.cs`.
3. `Med.Domain` **ne smije** referencirati `Microsoft.EntityFrameworkCore`.
4. Navigacijska svojstva ostaju `virtual` — bez toga lazy loading proxy ne radi.

**Izlaz u nuždi:** ako oko dana 10 vlastiti ORM ne stoji na nogama, EF verzija je u gitu netaknuta i predaje se ona. Nula bačenog posla.

---

## 3. Tehnologije

**Projekt 1**
- .NET 8, C#
- PostgreSQL 16 (Docker) + Supabase (cloud demonstracija)
- Npgsql, EF Core 8.0.30, Npgsql.EntityFrameworkCore.PostgreSQL 8.0.11
- Spectre.Console (konzolni izbornici)
- Castle.Core (planiran za lazy loading u vlastitom ORM-u)

**Projekt 2 (još nije započet)**
- MongoDB, MinIO, Kafka (sve u Dockeru)
- Jezik nije konačno odlučen — Python je preporuka zadatka, C# je moguć uz Snakemake omotač

**Okruženje:** Windows, Visual Studio 2022, Docker Desktop preko WSL2

---

## 4. Struktura repozitorija

```
MedSustav/
├── docker-compose.yml
├── .env                      (u .gitignore)
├── .gitignore
├── README.md
├── MedSustav.slnx            (novi .slnx format)
├── Med.ORM/
│   └── Mapping/
│       ├── Attributes.cs
│       └── SqlTypes.cs
├── Med.Domain/
│   ├── Entities/             9 entiteta + enumi
│   └── IRepo.cs              → treba preimenovati u IRepozitorij.cs
├── Med.Data.Ef/
│   ├── MedDbContext.cs
│   ├── Konfiguracije.cs
│   ├── EfRepozitorij.cs
│   ├── MedDbContextFactory.cs
│   └── Migrations/
└── Med.App/
    ├── Program.cs
    ├── appsettings.json
    └── appsettings.Local.json  (u .gitignore, sadrži Supabase lozinku)
```

**Napomena o nazivima:** projekt je `Med.ORM` (velika slova), namespace `Med.ORM.Mapping`. Zadržati dosljedno.

---

## 5. Model podataka

Devet entiteta. Model je namjerno proširen izvan doslovnog teksta zadatka kako bi pokrio sve što rubrika traži.

| Tablica | Klasa | Napomena |
|---|---|---|
| `adrese` | `Adresa` | izdvojena da Pacijent ima dvije N:1 veze |
| `pacijenti` | `Pacijent` | OIB je UNIQUE, CHAR(11) |
| `kartoni_pacijenata` | `KartonPacijenta` | veza 1:1, FK + UNIQUE |
| `lijecnici` | `Lijecnik` | **datoteka se zove `Ljecnik.cs` — typo, preimenovati** |
| `dijagnoze` | `Dijagnoza` | šifrarnik, MKB-10 |
| `lijekovi` | `Lijek` | šifrarnik |
| `povijest_bolesti` | `PovijestBolesti` | razdoblje bolovanja |
| `terapije` | `Terapija` | doza DECIMAL(10,2), jedinica, učestalost |
| `pregledi` | `Pregled` | tip iz enuma, termin TIMESTAMPTZ |

**Veze:** 1:1 (Pacijent–Karton), 1:N (Pacijent–PovijestBolesti/Terapije/Pregledi), N:1 (Pacijent–Adresa dvaput, Terapija–Lijek).

**Pokrivenost tipova** (zadatak traži svih osam):
INT (ključevi), DECIMAL (doza), FLOAT (visina/težina), VARCHAR (nazivi), CHAR (OIB, spol, krvna grupa), TEXT (nalaz, alergije, opis), TIMESTAMPTZ (termin, kreirano_na), TIMESTAMP (datum rođenja, datumi razdoblja).

**Pokrivenost ograničenja:** PRIMARY KEY s identity, UNIQUE (oib, sifra, pacijent_id), NOT NULL, DEFAULT (drzava, kreirano_na, aktivna, trajanje_minuta, status).

---

## 6. Napravljeno

### Dan 1 — zatvoren

- [x] Alati: Git, .NET 8 SDK, WSL2, Docker Desktop
- [x] Solution s četiri projekta, reference, NuGet paketi
- [x] GitHub repozitorij (privatni), commitovi
- [x] `docker-compose.yml` — postgres, mongo, minio, kafka + profil `tools` (pgadmin, mongo-express)
- [x] `.env` s kredencijalima, u gitignoreu
- [x] Postgres kontejner radi
- [x] `Med.ORM/Mapping/Attributes.cs` — Table, Column, PrimaryKey, NotNull, NullableColumn, Unique, Default, ForeignKey, NotMapped, Navigation, InverseNavigation
- [x] `Med.ORM/Mapping/SqlTypes.cs` — enum `SqlType` s `Inferred = 0`
- [x] Svih 9 entiteta s punim mapiranjem
- [x] Supabase projekt, session pooler connection string

### Dan 2 — zatvoren

- [x] `IRepozitorij<T>` sučelje (trenutno u datoteci `IRepo.cs`)
- [x] `MedDbContext` s 9 DbSet-ova, `ApplyConfigurationsFromAssembly`
- [x] `Konfiguracije.cs` — Fluent API za svih 9 entiteta (nazivi stupaca, tipovi, UNIQUE indeksi, DEFAULT, veze, OnDelete, enum → string konverzija)
- [x] `EfRepozitorij<T>`
- [x] `MedDbContextFactory` (design-time)
- [x] Migracija `Pocetna` generirana i izvršena — `Update-Database` prošao
- [x] Tablice postoje u bazi

### Dan 3 — u tijeku

Isporučen kod, **integracija nije potvrđena**:

- [ ] `SeedPodataka.cs` — liječnici (7), dijagnoze (8), lijekovi (8), svi uz provjeru `AnyAsync()`
- [ ] `Baza.cs` — učitavanje konfiguracije, `Otvori(config, lazyLoading)`, prekidač `IspisiSql`
- [ ] `Servisi/PacijentService.cs` — CRUD, validacija OIB-a, `DohvatiPunAsync` s Include lancima, `PostaviKartonAsync`
- [ ] `Servisi/TerapijaService.cs`
- [ ] `Servisi/PregledService.cs` — uključuje provjeru preklapanja termina
- [ ] NuGet `Microsoft.EntityFrameworkCore.Proxies` u `Med.App`

---

## 7. Trenutno stanje i poznati problemi

**Gdje smo stali:** kraj dana 2 potvrđen, prva polovica dana 3 isporučena ali nije verificirana buildom.

### Otvorene stavke čišćenja

1. `Med.Domain/IRepo.cs` → preimenovati u `IRepozitorij.cs`, sučelje `IRepo<T>` → `IRepozitorij<T>`, uskladiti s `EfRepozitorij`
2. `Med.Domain/Entities/Ljecnik.cs` → `Lijecnik.cs`
3. Provjeriti da `Med.Domain.csproj` **nema** `Microsoft.EntityFrameworkCore` paket
4. Obrisati zaostale `Class1.cs` ako ih ima
5. Obrisati staru mapu `Med_App` ako postoji
6. `Seeder.cs` zamijeniti sa `SeedPodataka.cs`

### Riješeni problemi koje treba imati na umu

- **Lokalni Postgres (`PostgreSQL_For_Odoo`) otimao je port 5432.** Servis je zaustavljen. Ako se greška `28P01 password authentication failed` vrati, prvo provjeriti `Get-Service *postgres*`.
- **`POSTGRES_PASSWORD` djeluje samo pri prvoj inicijalizaciji volumena.** Promjena lozinke traži `docker compose down -v`.
- **`MedDbContextFactory` ima connection string tvrdo zapisan u kodu.** Design-time alati ne čitaju `appsettings.json`. Ako se mijenja lozinka ili port, mora se promijeniti i tamo.
- **`Add-Migration` traži paket `Microsoft.EntityFrameworkCore.Tools`**, ne samo `.Design`. Nakon instalacije treba ponovno otvoriti Package Manager Console.

### Zamke za dalje

- **Npgsql i `DateTime.Kind`** — `timestamp without time zone` ne prima `DateTime` s `Kind = Local`. Koristiti literale tipa `new DateTime(1985, 3, 12)` ili `DateTime.SpecifyKind(..., DateTimeKind.Unspecified)`. Za `DateTimeOffset` nema problema.
- Migracije se pokreću iz Package Manager Console, Default project `Med.Data.Ef`, startup projekt `Med.App`.

---

## 8. Preostali milestone

### Dan 3 (druga polovica) — CRUD sučelje

- [ ] Glavni izbornik (Spectre.Console, navigacija strelicama)
- [ ] CRUD: pacijenti (uz adresu i karton)
- [ ] CRUD: povijest bolesti
- [ ] CRUD: terapije
- [ ] CRUD: pregledi (tip iz enuma, provjera termina)
- [ ] Listanje dijagnoza i lijekova
- [ ] Prikaz punog kartona pacijenta (demonstrira 1:1, 1:N, N:1 odjednom)
- [ ] Demonstracija eager vs lazy s ispisom generiranog SQL-a
- [ ] Seed demo pacijenata za obranu

**Nakon ovoga: ~30 bodova zaključano.**

### Dan 4 — Projekt 2, dio 1 (Ishod 3 minimalni)

- [ ] Dizanje mongo i minio kontejnera
- [ ] Odabir jezika i orkestratora (Python + Snakemake preporučeno zadatkom)
- [ ] Dohvat taksonomskih podataka o vrstama s `https://aves.regoch.net`
- [ ] Pohrana u MongoDB kolekciju, **bez duplikata** (unique index + upsert)
- [ ] Korak se preskače ako podaci već postoje

### Dan 5 — Projekt 2, dio 2 (Ishod 2)

- [ ] Obilazak ciljnog direktorija s audio datotekama
- [ ] Upload u MinIO s jedinstvenim identifikatorom
- [ ] Metapodaci (naziv, lokacija, object key) u MongoDB
- [ ] `POST https://aves.regoch.net/api/classify` po datoteci
- [ ] **Log svakog zahtjeva u MinIO** (lako se zaboravi, nosi bodove)
- [ ] Rezultati klasifikacije u MongoDB, povezani s taksonomijom

### Dan 6 — Projekt 2, dio 3 (Ishod 3 željeni)

- [ ] Kafka consumer — pročita sve poruke prisutne u trenutku izvođenja
- [ ] Fleksibilna shema za biološka svojstva (različita među izvorima — ne hardkodirati polja)
- [ ] CSV izvještaj: vrste s barem jednom pozitivnom klasifikacijom
- [ ] Čišćenje i transformacije podataka
- [ ] Fuzzy filter po nazivu vrste kao opcionalni parametar
- [ ] Orkestracija: više skripti, jedna ulazna točka
- [ ] GitHub Actions workflow s `workflow_dispatch` (dodatni bodovi)
- [ ] Vizualizacija izvještaja (dodatni bodovi)

**Nakon ovoga: ~70 bodova zaključano. Kritična točka plana.**

### Dani 7–11 — Vlastiti ORM

- [ ] **Dan 7:** `MetadataBuilder` — refleksija nad entitetima, mapiranje tipova, generiranje `CREATE TABLE`
- [ ] **Dan 8:** CRUD — INSERT s `RETURNING id`, materijalizacija DataReadera u objekte, UPDATE/DELETE po ključu, parametrizirani upiti
- [ ] **Dan 9:** Parsiranje ekspresija — `ExpressionVisitor` za `Expression<Func<T,bool>>` → WHERE, operatori `==`, `!=`, `>`, `<`, `&&`, `||`, `Contains` → LIKE, `OrderBy` → ORDER BY
- [ ] **Dan 10:** Navigacijska svojstva, eager loading (`Include`), lazy loading (Castle DynamicProxy)
- [ ] **Dan 11a:** Change tracking — identity map, snapshot originalnih vrijednosti, stanja Added/Unchanged/Modified/Deleted, `SaveChanges()` koji ažurira samo promijenjene stupce, sve u transakciji
- [ ] **Dan 11b:** Migracije — čitanje sheme iz `information_schema`, diff prema metapodacima, generiranje Up/Down, tablica `__migracije`, runner naprijed/unazad

### Dan 12 — Integracija

- [ ] Novi projekt `Med.Data.Orm` s implementacijom `IRepozitorij<T>` nad vlastitim ORM-om
- [ ] Prebacivanje `Med.App` na novu implementaciju (promjena jedne linije)
- [ ] Testiranje svih CRUD tokova

### Dan 13 — Predaja

- [ ] README: upute za pokretanje, arhitektura, donesene pretpostavke
- [ ] Provjera svake stavke rubrike
- [ ] Čišćenje repozitorija
- [ ] **Slanje projekta**

### Dani 14–15 — Priprema obrane

- [ ] Postgres arhitektura: WAL, vacuum, checkpointer
- [ ] ACID i MVCC u Postgresu
- [ ] Indeksi: kad planner koristi index a kad seq scan, `EXPLAIN ANALYZE`
- [ ] Upravljanje konekcijama, connection pooling, zašto Supabase ima pooler
- [ ] Demonstracija Postgresa u Dockeru **i** na Supabaseu
- [ ] Eager vs lazy, N+1 problem
- [ ] Kad migraciju nije moguće izvršiti
- [ ] Uvježbati redoslijed demonstracije

---

## 9. Mapiranje bodova na rubriku

### Ishod 1 (20)

| Stavka | Bodovi | Status |
|---|---|---|
| MIN: Postgres arhitektura (WAL, vacuum, checkpointer) | 2 | teorija za obranu |
| MIN: ACID i realizacija u Postgresu | 2 | teorija za obranu |
| MIN: Postgres kroz Docker i Supabase | 2 | infrastruktura gotova |
| MIN: indeksi i kad se koriste | 2 | teorija za obranu |
| MIN: povezivanje i upravljanje konekcijom | 2 | teorija za obranu |
| ŽELJENI: mapiranje klasa na tablice | 2,5 | dan 7 |
| ŽELJENI: dohvat i umetanje | 2,5 | dan 8 |
| ŽELJENI: filtriranje (WHERE) | 2,5 | dan 9 |
| ŽELJENI: ograničenja nad stupcima | 2,5 | dan 7 |

### Ishod 4 (20)

| Stavka | Bodovi | Status |
|---|---|---|
| MIN: programsko rješenje za scenarij | 7,5 | dan 3 |
| MIN: eager ili lazy + objašnjenje razlike | 2,5 | dan 3 |
| ŽELJENI: navigacijska svojstva (1:1, 1:N, N:1) | 5 | dan 10 |
| ŽELJENI: change tracking | 5 | dan 11 |

### Ishod 5 (20)

| Stavka | Bodovi | Status |
|---|---|---|
| MIN: code-first pristup | 5 | gotovo |
| MIN: migracije i kad nisu izvedive | 5 | gotovo + teorija |
| ŽELJENI: automatsko generiranje migracija iz diffa | 7 | dan 11 |
| ŽELJENI: izvršavanje naprijed/unazad + praćenje stanja | 3 | dan 11 |

### Ishod 2 (20)

| Stavka | Bodovi | Status |
|---|---|---|
| MIN: upload u MinIO, jedinstvena identifikacija, metapodaci u Mongo | 10 | dan 5 |
| ŽELJENI: klasifikacijski API + log zahtjeva u MinIO + rezultati u Mongo | 10 | dan 5 |

### Ishod 3 (20)

| Stavka | Bodovi | Status |
|---|---|---|
| MIN: taksonomija u Mongo bez duplikata + rezultati klasifikacije | 10 | dani 4–5 |
| ŽELJENI: Kafka opažanja u Mongo | 5 | dan 6 |
| ŽELJENI: fuzzy filter + čišćenje i transformacije | 5 | dan 6 |

---

## 10. Konvencije

- **Jezik koda:** hrvatski za nazive klasa, metoda i varijabli (`Pacijent`, `DohvatiSveAsync`, `SpremiPromjeneAsync`)
- **Nazivi u bazi:** snake_case, hrvatski (`pacijenti`, `datum_rodenja`, `povijest_bolesti`)
- **Bez dijakritike** u kodu i seed podacima — izbjegava probleme s kodnim stranicama u konzoli
- **Async svugdje** gdje se dira baza, sufiks `Async`
- **Commit poruke:** `feat:`, `fix:`, `chore:`, `docs:`
- **Commitati svaki dan** — Git povijest se ocjenjuje
- **Bez komentara u kodu** osim gdje objašnjavaju netrivijalnu odluku

---

## 11. Docker podsjetnik

```bash
docker compose up -d postgres              # samo Postgres
docker compose up -d                       # svi servisi
docker compose --profile tools up -d       # + pgAdmin i mongo-express
docker compose ps                          # status, tražiti (healthy)
docker compose logs postgres               # dijagnostika
docker compose down -v                     # briše i volumene (gubi podatke)
```

| Servis | Adresa | Kredencijali |
|---|---|---|
| PostgreSQL | `localhost:5432` | `med` / `med123`, baza `medsustav` |
| MongoDB | `localhost:27017` | `root` / `root123` |
| MinIO API | `localhost:9000` | `minioadmin` / `minioadmin` |
| MinIO konzola | `localhost:9001` | isto |
| Kafka | `localhost:9092` | bez autentikacije |
| pgAdmin | `localhost:5050` | `admin@local.dev` / `admin`, host baze `postgres` |
| mongo-express | `localhost:8081` | bez autentikacije |

Migracije (Package Manager Console, Default project `Med.Data.Ef`):

```powershell
Add-Migration NazivMigracije
Update-Database
Remove-Migration
```
