# MedSustav

Projektni zadatak iz kolegija **Pristup podacima iz programskog koda** (Algebra).

Medicinski sustav za upravljanje pacijentima, poviješću bolesti, terapijama i
specijalističkim pregledima. Konzolna aplikacija nad PostgreSQL bazom, code-first
pristup, EF Core kao ORM.

---

## Preduvjeti

- .NET 8 SDK
- Docker Desktop (WSL2 backend na Windowsu)

## Pokretanje

```bash
docker compose up -d postgres
```

```bash
dotnet run --project Med.App
```

Aplikacija pri pokretanju provjerava dostupnost baze i neprimijenjene migracije, a
liječnike, dijagnoze i lijekove unosi automatski pri prvom pokretanju. Izbornik
traži interaktivni terminal — ne radi s preusmjerenim ulazom.

Za demonstraciju s ispisom svakog generiranog SQL upita:

```bash
dotnet run --project Med.App -- --sql
```

Iz izbornika je dostupna stavka **Ucitaj demo podatke** koja puni bazu s tri
pacijenta, poviješću bolesti, terapijama i pregledima — korisno za demonstraciju
navigacijskih svojstava i usporedbe eager/lazy učitavanja.

## Servisi

```bash
docker compose up -d
```

```bash
docker compose --profile tools up -d
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

Kredencijali se čitaju iz `.env` (nije u repozitoriju, ima zadane vrijednosti u
`docker-compose.yml`).

## Konfiguracija baze

`Med.App/appsettings.json` sadrži lokalni Docker connection string. Za
demonstraciju na Supabaseu kopiraj `appsettings.Local.example.json` u
`appsettings.Local.json`, upiši svoje podatke i postavi `UseConnection` na
`Supabase`. Ta datoteka **nije** u repozitoriju jer sadrži lozinku.

## Migracije

Package Manager Console, Default project `Med.Data.Ef`, startup projekt `Med.App`:

```powershell
Add-Migration NazivMigracije
```

```powershell
Update-Database
```

`MedDbContextFactory` ne čita `appsettings.json` — design-time alati koriste
varijablu okoline `MED_CONNECTION`, uz lokalni Docker string kao zadanu vrijednost.

---

## Arhitektura

```
Med.ORM        vlastiti atributi za mapiranje (Table, Column, PrimaryKey, ...)
Med.Domain     entiteti + IRepozitorij<T>          ne zna za EF
Med.Data.Ef    MedDbContext + EfRepozitorij<T>     EF implementacija
Med.App        konzolna aplikacija, servisi i izbornici
```

Sloj pristupa podacima odvojen je iza `IRepozitorij<T>` kako bi se EF mogao
zamijeniti vlastitom implementacijom ORM-a bez diranja aplikacije.

Iz toga slijede pravila kojih se držimo:

1. Entitetske klase nose **isključivo** vlastite atribute iz `Med.ORM.Mapping`.
   Nijedna EF anotacija ne ide na klase.
2. Sva EF konfiguracija je Fluent API u `Med.Data.Ef/Konfiguracije.cs`.
3. `Med.Domain` ne referencira `Microsoft.EntityFrameworkCore`.
4. Navigacijska svojstva su `virtual` — bez toga lazy loading proxy ne radi.

## Model podataka

Devet entiteta. Model je namjerno proširen izvan doslovnog teksta zadatka kako bi
pokrio sve tipove i ograničenja koje rubrika traži.

| Tablica | Klasa | Napomena |
|---|---|---|
| `adrese` | `Adresa` | izdvojena da Pacijent ima dvije N:1 veze |
| `pacijenti` | `Pacijent` | OIB je UNIQUE, CHAR(11) |
| `kartoni_pacijenata` | `KartonPacijenta` | veza 1:1, FK + UNIQUE |
| `lijecnici` | `Lijecnik` | ne uređuje se kroz CRUD |
| `dijagnoze` | `Dijagnoza` | šifrarnik, MKB-10 |
| `lijekovi` | `Lijek` | šifrarnik |
| `povijest_bolesti` | `PovijestBolesti` | razdoblje bolovanja |
| `terapije` | `Terapija` | doza DECIMAL(10,2), jedinica, učestalost |
| `pregledi` | `Pregled` | tip iz enuma, termin TIMESTAMPTZ |

**Veze:** 1:1 Pacijent–Karton; 1:N Pacijent prema povijesti, terapijama i
pregledima; N:1 Pacijent–Adresa (dvaput), Terapija–Lijek, Pregled–Liječnik
(specijalist i uputitelj), PovijestBolesti–Dijagnoza.

**Pokriveni SQL tipovi:** INT (ključevi), DECIMAL (doza), FLOAT (visina, težina),
VARCHAR (nazivi), CHAR (OIB, spol, krvna grupa), TEXT (nalaz, alergije, opis),
TIMESTAMPTZ (termin, kreirano_na), TIMESTAMP (datum rođenja, razdoblja).

**Pokrivena ograničenja:** PRIMARY KEY s identity, UNIQUE (`oib`, `sifra`,
`pacijent_id`), NOT NULL, DEFAULT (`drzava`, `kreirano_na`, `aktivna`,
`trajanje_minuta`, `status`).

## Eager vs lazy

Izbornik ima stavku koja isti skup podataka dohvaća na oba načina i broji stvarno
izvršene SQL naredbe. Na demo podacima:

| Pristup | Upita | Nedostatak |
|---|---|---|
| Eager (`Include`) | 1 | jedan veliki JOIN dohvaća i nepotrebne stupce; kod više 1:N grana redci se umnažaju |
| Lazy (proxy) | 11 | mali početni upit, ali svaki pristup navigaciji okida novi SELECT — u petlji daje N+1 |

---

## Donesene pretpostavke

- Liječnici se, prema tekstu zadatka, unose samo pri prvom pokretanju i nemaju CRUD.
- Adresa je izdvojena u zasebnu tablicu umjesto da bude skup stupaca na pacijentu,
  čime pacijent dobiva dvije N:1 veze (boravište i prebivalište).
- Pregled uz liječnika specijalista ima i neobaveznog liječnika uputitelja.
- Preklapanje termina istog liječnika provjerava se pri zakazivanju.
- Cloud konfiguracija (Supabase) je opcionalna; zadatak dopušta rad isključivo
  kroz Docker kontejnere.

## Poznate zamke

Npgsql je strog oko vremenskih tipova:

- `timestamp without time zone` ne prima `DateTime` s `Kind = Local` — koristi
  `DateTimeKind.Unspecified`.
- `timestamp with time zone` prima **samo** `DateTimeOffset` s offsetom 0 (UTC).
  Lokalno uneseno vrijeme pretvara se u UTC pri unosu, a natrag u lokalno pri
  prikazu.
