"""Korak 2: audio datoteke iz ciljnog direktorija u MinIO, metapodaci u MongoDB.

Jedinstveni identifikator datoteke je SHA-256 sazetak njezinog sadrzaja. Time je
upload idempotentan: ista datoteka pod drugim imenom, ili ponovno pokretanje nad
istim direktorijem, ne stvara novi objekt.

Svaka mapa unutar ciljnog direktorija odgovara jednom geografskom polozaju,
kako zadatak dopusta radi jednostavnosti.
"""

import hashlib
import json
import sys
from datetime import datetime, timezone
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from zajednicko import (  # noqa: E402
    PIPELINE,
    kolekcija,
    minio_klijent,
    osiguraj_direktorij,
    osiguraj_spremnik,
)


def sazetak_datoteke(putanja: Path) -> str:
    h = hashlib.sha256()
    with putanja.open("rb") as f:
        for blok in iter(lambda: f.read(1024 * 1024), b""):
            h.update(blok)
    return h.hexdigest()


def pronadi(config: dict) -> list[tuple[Path, str]]:
    """Vraca parove (putanja, kljuc_lokacije) za sve audio datoteke."""
    korijen = PIPELINE / config["audio"]["direktorij"]
    nastavci = {n.lower() for n in config["audio"]["nastavci"]}
    lokacije = config["audio"]["lokacije"]

    if not korijen.exists():
        return []

    nadeno = []
    for mapa in sorted(p for p in korijen.iterdir() if p.is_dir()):
        if mapa.name not in lokacije:
            print(f"Preskacem mapu '{mapa.name}' - nema definirane lokacije u configu.")
            continue

        for datoteka in sorted(mapa.rglob("*")):
            if datoteka.is_file() and datoteka.suffix.lower() in nastavci:
                nadeno.append((datoteka, mapa.name))

    return nadeno


def pokreni(config: dict, izvjestaj: str) -> dict:
    klijent = minio_klijent(config)
    spremnik = osiguraj_spremnik(klijent, config["minio"]["spremnik_audio"])
    kol = kolekcija(config, "datoteke")
    kol.create_index("objekt", unique=True, name="jedinstveni_objekt")

    datoteke = pronadi(config)
    if not datoteke:
        print("Nema audio datoteka u ciljnom direktoriju.")

    preneseno = 0
    preskoceno = 0

    for putanja, kljuc_lokacije in datoteke:
        sazetak = sazetak_datoteke(putanja)
        objekt = f"{kljuc_lokacije}/{sazetak}{putanja.suffix.lower()}"
        lokacija = config["audio"]["lokacije"][kljuc_lokacije]

        vec_postoji = kol.find_one({"_id": sazetak}) is not None
        if vec_postoji:
            preskoceno += 1
        else:
            klijent.fput_object(spremnik, objekt, str(putanja))
            preneseno += 1
            print(f"Preneseno: {putanja.name} -> {spremnik}/{objekt}")

        kol.update_one(
            {"_id": sazetak},
            {
                "$set": {
                    "naziv": putanja.name,
                    "izvorna_putanja": str(putanja.relative_to(PIPELINE)),
                    "spremnik": spremnik,
                    "objekt": objekt,
                    "velicina": putanja.stat().st_size,
                    "lokacija": {
                        "kljuc": kljuc_lokacije,
                        "naziv": lokacija["naziv"],
                        "sirina": lokacija["sirina"],
                        "duzina": lokacija["duzina"],
                    },
                },
                "$setOnInsert": {"preneseno_na": datetime.now(timezone.utc)},
            },
            upsert=True,
        )

    sazetak_koraka = {
        "korak": "datoteke",
        "pronadeno": len(datoteke),
        "preneseno": preneseno,
        "vec_postojalo": preskoceno,
        "u_kolekciji": kol.count_documents({}),
    }

    print(
        f"Pronadeno {len(datoteke)}, preneseno {preneseno}, "
        f"vec postojalo {preskoceno}."
    )

    cilj = osiguraj_direktorij(izvjestaj)
    cilj.write_text(
        json.dumps(sazetak_koraka, indent=2, ensure_ascii=False), encoding="utf-8"
    )
    return sazetak_koraka


if __name__ == "__main__":
    pokreni(snakemake.config, snakemake.output[0])  # noqa: F821
