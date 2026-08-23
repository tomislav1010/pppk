"""Korak 3: klasifikacija ptice po audio zapisu.

Za svaku prenesenu datoteku salje se zahtjev klasifikacijskom modelu. Log
svakog zahtjeva - i uspjesnog i neuspjesnog - sprema se u MinIO, a rezultati u
MongoDB.

Klasifikator poznaje znatno vise vrsta nego sto ih ima u nasoj taksonomskoj
kolekciji, koja je uzorak GBIF-a. Zato se poveznica na taksonomiju upisuje kad
vrsta postoji, a inace se rezultat i dalje sprema uz oznaku da poveznice nema.
"""

import io
import json
import sys
from datetime import datetime, timezone
from pathlib import Path

import requests

sys.path.insert(0, str(Path(__file__).resolve().parent))
from zajednicko import (  # noqa: E402
    kolekcija,
    minio_klijent,
    osiguraj_direktorij,
    osiguraj_spremnik,
)


def posalji(url: str, naziv: str, sadrzaj: bytes) -> tuple[int, dict | str, float]:
    poceo = datetime.now(timezone.utc)
    odgovor = requests.post(url, files={"file": (naziv, sadrzaj)}, timeout=180)
    trajanje = (datetime.now(timezone.utc) - poceo).total_seconds()

    try:
        tijelo = odgovor.json()
    except ValueError:
        tijelo = odgovor.text[:2000]

    return odgovor.status_code, tijelo, trajanje


def povezi(taksonomija, znanstveni_naziv: str | None) -> dict:
    if not znanstveni_naziv:
        return {"povezano": False, "razlog": "klasifikator nije vratio naziv"}

    vrsta = taksonomija.find_one(
        {"canonicalName": znanstveni_naziv},
        {"canonicalName": 1, "family": 1, "order": 1, "genus": 1},
    )
    if vrsta is None:
        return {"povezano": False, "razlog": "vrste nema u taksonomskoj kolekciji"}

    return {
        "povezano": True,
        "vrsta_id": vrsta["_id"],
        "porodica": vrsta.get("family"),
        "red": vrsta.get("order"),
        "rod": vrsta.get("genus"),
    }


def pokreni(config: dict, izvjestaj: str) -> dict:
    url = config["izvori"]["klasifikacija_url"]
    prag = config["klasifikacija"]["prag_pouzdanosti"]

    klijent = minio_klijent(config)
    spremnik_logova = osiguraj_spremnik(klijent, config["minio"]["spremnik_logovi"])

    datoteke = kolekcija(config, "datoteke")
    klasifikacije = kolekcija(config, "klasifikacije")
    taksonomija = kolekcija(config, "taksonomija")
    klasifikacije.create_index("datoteka_id", name="po_datoteci")

    obradeno = 0
    preskoceno = 0
    neuspjeha = 0
    ukupno_pogodaka = 0

    for zapis in datoteke.find({}):
        if klasifikacije.find_one({"_id": zapis["_id"]}) is not None:
            preskoceno += 1
            continue

        sadrzaj = klijent.get_object(zapis["spremnik"], zapis["objekt"]).read()
        trenutak = datetime.now(timezone.utc)

        try:
            status, tijelo, trajanje = posalji(url, zapis["naziv"], sadrzaj)
            greska = None
        except requests.RequestException as e:
            status, tijelo, trajanje, greska = 0, None, 0.0, str(e)

        log = {
            "trenutak": trenutak.isoformat(),
            "datoteka_id": zapis["_id"],
            "naziv": zapis["naziv"],
            "objekt": f"{zapis['spremnik']}/{zapis['objekt']}",
            "zahtjev": {"url": url, "metoda": "POST", "velicina_bajtova": len(sadrzaj)},
            "odgovor": {
                "status": status,
                "trajanje_s": round(trajanje, 3),
                "tijelo": tijelo,
            },
            "greska": greska,
        }

        # Log ide u MinIO prije obrade rezultata, pa se biljezi i neuspjeh.
        kljuc_loga = f"klasifikacija/{trenutak:%Y/%m/%d}/{zapis['_id']}.json"
        podaci = json.dumps(log, indent=2, ensure_ascii=False).encode("utf-8")
        klijent.put_object(
            spremnik_logova,
            kljuc_loga,
            io.BytesIO(podaci),
            length=len(podaci),
            content_type="application/json",
        )

        if greska is not None or status != 200:
            neuspjeha += 1
            print(f"Neuspjeh za {zapis['naziv']}: status {status} {greska or ''}".strip())
            continue

        pogoci = []
        for r in (tijelo or {}).get("results", []):
            pogodak = {
                "znanstveni_naziv": r.get("scientific_name"),
                "obicni_naziv": r.get("common_name"),
                "pouzdanost": r.get("confidence"),
                "pocetak_s": r.get("start_time"),
                "kraj_s": r.get("end_time"),
                "pozitivna": (r.get("confidence") or 0) >= prag,
            }
            pogodak.update(povezi(taksonomija, pogodak["znanstveni_naziv"]))
            pogoci.append(pogodak)

        klasifikacije.update_one(
            {"_id": zapis["_id"]},
            {
                "$set": {
                    "datoteka_id": zapis["_id"],
                    "naziv": zapis["naziv"],
                    "lokacija": zapis["lokacija"],
                    "log_objekt": f"{spremnik_logova}/{kljuc_loga}",
                    "prag_pouzdanosti": prag,
                    "pogoci": pogoci,
                    "klasificirano_na": trenutak,
                }
            },
            upsert=True,
        )

        obradeno += 1
        ukupno_pogodaka += len(pogoci)
        pozitivnih = sum(1 for p in pogoci if p["pozitivna"])
        print(f"{zapis['naziv']}: {len(pogoci)} pogodaka, {pozitivnih} iznad praga.")

    sazetak = {
        "korak": "klasifikacija",
        "obradeno": obradeno,
        "vec_klasificirano": preskoceno,
        "neuspjeha": neuspjeha,
        "ukupno_pogodaka": ukupno_pogodaka,
        "povezanih_s_taksonomijom": klasifikacije.count_documents(
            {"pogoci.povezano": True}
        ),
        "u_kolekciji": klasifikacije.count_documents({}),
    }

    print(
        f"Obradeno {obradeno}, vec klasificirano {preskoceno}, neuspjeha {neuspjeha}."
    )

    cilj = osiguraj_direktorij(izvjestaj)
    cilj.write_text(json.dumps(sazetak, indent=2, ensure_ascii=False), encoding="utf-8")
    return sazetak


if __name__ == "__main__":
    pokreni(snakemake.config, snakemake.output[0])  # noqa: F821
