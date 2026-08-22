"""Korak 1: taksonomija vrsta ptica s aves.regoch.net u MongoDB, bez duplikata.

Kolekcija ima jedinstveni indeks nad GBIF kljucem, a upis ide kroz upsert, pa
ponovno pokretanje nikad ne stvara duplikate. Ako kolekcija vec sadrzi podatke,
dohvat s mreze se preskace u cijelosti.
"""

import json
import sys
from datetime import datetime, timezone
from pathlib import Path

import requests
from pymongo import UpdateOne

sys.path.insert(0, str(Path(__file__).resolve().parent))
from zajednicko import kolekcija, mongo_baza, osiguraj_direktorij  # noqa: E402

KLJUC = "key"


def dohvati(url: str) -> list[dict]:
    odgovor = requests.get(url, timeout=120)
    odgovor.raise_for_status()

    podaci = odgovor.json()
    if not isinstance(podaci, list):
        raise ValueError(f"Ocekivana lista zapisa, dobiveno {type(podaci).__name__}.")

    return podaci


def pripremi(zapis: dict) -> dict:
    """GBIF kljuc postaje _id kako bi jedinstvenost jamcio sam Mongo."""
    pripremljen = dict(zapis)
    pripremljen["_id"] = zapis[KLJUC]
    pripremljen["ucitano_na"] = datetime.now(timezone.utc)
    return pripremljen


def pokreni(config: dict, izvjestaj: str) -> dict:
    kol = kolekcija(config, "taksonomija")
    kol.create_index(KLJUC, unique=True, name="jedinstveni_kljuc")

    # Sam broj dokumenata nije dovoljan uvjet za preskakanje: prekinut dohvat
    # ostavlja kolekciju djelomicno punom i takvo bi stanje ostalo zauvijek.
    # Zato se preskace tek kad postoji oznaka zavrsenog ucitavanja.
    oznake = mongo_baza(config)["ucitavanja"]
    oznaka = oznake.find_one({"_id": "taksonomija"})
    postojecih = kol.count_documents({})

    if oznaka is not None and postojecih >= oznaka.get("broj", 0):
        sazetak = {
            "korak": "taksonomija",
            "preskoceno": True,
            "razlog": "ucitavanje je vec zavrseno",
            "u_kolekciji": postojecih,
        }
        print(f"Preskacem dohvat: kolekcija vec ima {postojecih} vrsta.")
        _zapisi(izvjestaj, sazetak)
        return sazetak

    if postojecih > 0:
        print(f"Kolekcija ima {postojecih} vrsta bez oznake zavrsetka - dovrsavam ucitavanje.")

    url = config["izvori"]["taksonomija_url"]
    print(f"Dohvacam {url} ...")
    zapisi = dohvati(url)
    print(f"Dohvaceno {len(zapisi)} zapisa.")

    naredbe = [
        UpdateOne({"_id": z[KLJUC]}, {"$set": pripremi(z)}, upsert=True)
        for z in zapisi
        if z.get(KLJUC) is not None
    ]

    bez_kljuca = len(zapisi) - len(naredbe)
    if bez_kljuca:
        print(f"Preskaceno {bez_kljuca} zapisa bez polja '{KLJUC}'.")

    rezultat = kol.bulk_write(naredbe, ordered=False)

    ukupno = kol.count_documents({})
    oznake.update_one(
        {"_id": "taksonomija"},
        {"$set": {"broj": ukupno, "zavrseno_na": datetime.now(timezone.utc)}},
        upsert=True,
    )

    sazetak = {
        "korak": "taksonomija",
        "preskoceno": False,
        "dohvaceno": len(zapisi),
        "umetnuto": rezultat.upserted_count,
        "azurirano": rezultat.modified_count,
        "bez_kljuca": bez_kljuca,
        "u_kolekciji": ukupno,
    }

    print(
        f"Umetnuto {sazetak['umetnuto']}, azurirano {sazetak['azurirano']}, "
        f"ukupno u kolekciji {sazetak['u_kolekciji']}."
    )
    _zapisi(izvjestaj, sazetak)
    return sazetak


def _zapisi(putanja: str, sazetak: dict) -> None:
    cilj = osiguraj_direktorij(putanja)
    cilj.write_text(json.dumps(sazetak, indent=2, ensure_ascii=False), encoding="utf-8")


if __name__ == "__main__":
    pokreni(snakemake.config, snakemake.output[0])  # noqa: F821
