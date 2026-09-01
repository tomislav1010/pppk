# MedSustav

Projektni zadatak iz kolegija **Pristup podacima iz programskog koda** (Algebra).

Repozitorij sadrži dva neovisna projekta koji dijele samo `docker-compose.yml` i `.env`:

| **Projekt 1** — `Med.*` | Medicinski sustav nad PostgreSQL bazom, C# konzolna aplikacija, code-first, EF Core 
| **Projekt 2** — `pipeline/` | Pipeline za taksonomiju i opažanja ptica, Python + Snakemake, MongoDB, MinIO, Kafka 

---

## Preduvjeti

- Docker Desktop (WSL2 backend na Windowsu) — za oba projekta
- .NET 8 SDK — za Projekt 1
- [uv](https://docs.astral.sh/uv/) — za Projekt 2 (sam dohvaća Python 3.12)

## Servisi

Svi servisi za pohranu podignuti su kao Docker kontejneri.

```bash
docker compose up -d
```

Uz opcionalne alate za pregled baza:

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

---

# Projekt 1 — medicinski sustav

Upravljanje pacijentima, poviješću bolesti, terapijama i specijalističkim
pregledima.

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

# Projekt 2 — pipeline za opažanja ptica

Generira skup podataka o pticama i njihovim opažanjima iz dva izvora: audio
snimaka i vanjskih ornitoloških servisa. Živi u `pipeline/`.

## Instalacija

```bash
docker compose up -d mongo minio kafka
```

Pričekaj da MongoDB i MinIO budu `(healthy)`:

```bash
docker compose ps
```

Okruženje se stvara kroz `uv`, koji sam dohvaća Python 3.12 neovisno o sistemskom:

```bash
cd pipeline && uv venv .venv --python 3.12
```

```bash
cd pipeline && VIRTUAL_ENV="$PWD/.venv" uv pip install -r pyproject.toml
```

Dalje se koristi Python iz tog okruženja. Na Windowsu je to
`.venv\Scripts\`, na Linuxu i macOS-u `.venv/bin/`.

## Prvo pokretanje

**Redoslijed je bitan.** Simulator opažanja bira vrste iz taksonomije i iz već
dobivenih klasifikacija, pa na praznom sustavu ta dva koraka moraju proći prije
njega.

Testne snimke — `audio/` nije u repozitoriju, pa se generiraju:

```bash
cd pipeline && ./.venv/Scripts/python.exe skripte/pripremi_test_audio.py 5
```

Taksonomija, prijenos u MinIO i klasifikacija:

```bash
cd pipeline && ./.venv/Scripts/snakemake.exe --cores 1 rezultati/klasifikacija.json
```

Objava opažanja na Kafku:

```bash
cd pipeline && ./.venv/Scripts/python.exe skripte/proizvodac_opazanja.py 60
```

Ostatak lanca:

```bash
cd pipeline && ./.venv/Scripts/snakemake.exe --cores 1
```

## Ponovno pokretanje

Kad su podaci već u bazi, dovoljna je jedna naredba — svaki korak sam prepoznaje
da je posao odrađen i preskače ga:

```bash
cd pipeline && ./.venv/Scripts/snakemake.exe --cores 1
```

## Parametri u izvođenju

Opcionalni fuzzy filtar po nazivu vrste, i prag podudaranja:

```bash
cd pipeline && ./.venv/Scripts/snakemake.exe --cores 1 --config filtar="antthrush" fuzzy_prag=80
```

Filtar traži po znanstvenom i po običnom nazivu i podnosi tipfelere. Prazna
vrijednost znači bez filtriranja. Zadane vrijednosti su u `config.yaml`.

## Koraci

```
taksonomija ─┐
datoteke ────┼─→ klasifikacija ─┐
             │                  ├─→ izvjestaj ─→ vizualizacija
opazanja ────┴──────────────────┘
```

| Korak | Što radi |
|---|---|
| `taksonomija` | 1000 GBIF vrsta s `aves.regoch.net/aves.json` u MongoDB |
| `datoteke` | snimke iz `audio/` u MinIO, metapodaci u MongoDB |
| `klasifikacija` | `POST /api/classify` po snimci, log zahtjeva u MinIO, rezultati u MongoDB |
| `opazanja` | Kafka consumer, poruke prisutne u trenutku izvođenja |
| `izvjestaj` | CSV o vrstama s barem jednom pozitivnom klasifikacijom |
| `vizualizacija` | tri grafa iz CSV-a |

Svaki korak je idempotentan — ponovno pokretanje ne stvara duplikate.

## Gdje završavaju podaci

**MongoDB**, baza `aves`:

| Kolekcija | Sadržaj |
|---|---|
| `taksonomija` | vrste ptica, jedinstveni indeks nad GBIF ključem |
| `datoteke` | metapodaci snimaka: naziv, object key, veličina, geopozicija |
| `klasifikacije` | rezultati po snimci, poveznica na vrstu, putanja do loga |
| `opazanja` | poruke s Kafke, biološka svojstva kakva jesu |
| `ucitavanja` | oznake završenih učitavanja |

**MinIO:** spremnik `audio` (snimke, ključ je SHA-256 sažetak sadržaja) i
`logovi` (`klasifikacija/GGGG/MM/DD/<id>.json`).

**Lokalno:** `pipeline/rezultati/` — sažetak svakog koraka u JSON-u, `vrste.csv` i
`vrste.png`. Nije u repozitoriju.

## GitHub Actions

Pipeline se može pokrenuti i ručno, kroz Actions → *Aves pipeline* → Run workflow,
s istim opcionalnim parametrima. Workflow diže servise iz ovog
`docker-compose.yml`, prolazi cijeli lanac i sprema `rezultati/` kao artifact.

---

# Donesene pretpostavke

**Projekt 1**

- Liječnici se, prema tekstu zadatka, unose samo pri prvom pokretanju i nemaju CRUD.
- Adresa je izdvojena u zasebnu tablicu umjesto da bude skup stupaca na pacijentu,
  čime pacijent dobiva dvije N:1 veze (boravište i prebivalište).
- Pregled uz liječnika specijalista ima i neobaveznog liječnika uputitelja.
- Preklapanje termina istog liječnika provjerava se pri zakazivanju.
- Cloud konfiguracija (Supabase) je opcionalna; zadatak dopušta rad isključivo
  kroz Docker kontejnere.

**Projekt 2**

- Taksonomija nije dostupna kroz API. `openapi.json` na `aves.regoch.net`
  dokumentira samo `POST /api/classify`, a stranica dohvaća statični `aves.json`,
  pa se podaci uzimaju odande.
- Korak taksonomije preskače se tek kad postoji **oznaka završenog učitavanja**, a
  ne čim kolekcija nije prazna. Inače bi prekinut dohvat trajno ostavio kolekciju
  nedovršenom.
- Sve snimke u istoj mapi dijele jedan geografski položaj, kako zadatak dopušta
  radi jednostavnosti. Mapiranje mape na koordinate je u `config.yaml`.
- Jedinstveni identifikator snimke je SHA-256 sažetak sadržaja, ne ime datoteke.
- Opažanje se prihvaća ako nosi taksonomski kod **ili** znanstveni naziv, jer
  izvori ne prijavljuju jednako potpune podatke.
- Klasifikator poznaje znatno više vrsta nego što ih ima u uzorku GBIF taksonomije
  (1000 zapisa, oko 9 % svjetskih ptica). Zato se rezultat uvijek sprema, a
  poveznica na vrstu upisuje kad postoji, uz razlog kad ne postoji. Da se
  nepovezani rezultati odbacuju, izvještaj bi često bio prazan.

# Poznate zamke

**Npgsql i vremenski tipovi**

- `timestamp without time zone` ne prima `DateTime` s `Kind = Local` — koristi
  `DateTimeKind.Unspecified`.
- `timestamp with time zone` prima **samo** `DateTimeOffset` s offsetom 0 (UTC).
  Lokalno uneseno vrijeme pretvara se u UTC pri unosu, a natrag u lokalno pri
  prikazu.

**Pipeline**

- MongoDB i `pipeline/rezultati/` čiste se **zajedno**. Obriše li se samo baza,
  Snakemake vidi postojeće izlazne datoteke i preskoči sve korake, pa baza ostane
  prazna.
- Klasifikacijski model uz ptice vraća i klase za ne-ptičje zvukove (npr. `Siren`).
  Trenutno prolaze kroz izvještaj kao vrste ako im pouzdanost prijeđe prag.
