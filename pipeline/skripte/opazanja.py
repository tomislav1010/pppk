"""Korak 4: opazanja ptica s Kafke u MongoDB.

Zadatak trazi da se procitaju sve poruke prisutne na brokeru u trenutku
izvrsavanja. Zato se na pocetku ocitaju krajnji offseti i cita se tocno do njih,
umjesto da consumer ostane visjeti i cekati nove poruke.

Biolska svojstva se razlikuju medu izvorima, pa se spremaju kakva jesu, bez
hardkodiranog popisa polja. Uz njih se biljezi i popis videnih kljuceva, sto
kasnije olaksava izvjestaj.
"""

import json
import sys
from datetime import datetime, timezone
from pathlib import Path

from kafka import KafkaConsumer, TopicPartition

sys.path.insert(0, str(Path(__file__).resolve().parent))
from zajednicko import kolekcija, osiguraj_direktorij  # noqa: E402


def procitaj_prisutne(config: dict) -> list[dict]:
    tema = config["kafka"]["tema"]

    consumer = KafkaConsumer(
        bootstrap_servers=config["kafka"]["bootstrap"],
        value_deserializer=lambda v: json.loads(v.decode("utf-8")),
        enable_auto_commit=False,
        consumer_timeout_ms=10000,
        auto_offset_reset="earliest",
    )

    particije = consumer.partitions_for_topic(tema)
    if not particije:
        print(f"Tema '{tema}' ne postoji ili je prazna.")
        consumer.close()
        return []

    tp = [TopicPartition(tema, p) for p in particije]
    consumer.assign(tp)

    krajevi = consumer.end_offsets(tp)
    for p in tp:
        consumer.seek_to_beginning(p)

    ukupno = sum(krajevi.values())
    if ukupno == 0:
        consumer.close()
        return []

    poruke = []
    for zapis in consumer:
        poruke.append(zapis.value)
        if all(
            consumer.position(p) >= krajevi[p] for p in tp
        ):
            break

    consumer.close()
    return poruke


def pokreni(config: dict, izvjestaj: str) -> dict:
    kol = kolekcija(config, "opazanja")
    kol.create_index("taksonomski_kod", name="po_vrsti")

    poruke = procitaj_prisutne(config)
    print(f"Procitano {len(poruke)} poruka s Kafke.")

    novih = 0
    neidentificiranih = 0
    bez_koda = 0
    svojstva_videna: set[str] = set()

    for p in poruke:
        kod = p.get("taksonomski_kod")
        naziv = p.get("znanstveni_naziv")

        # Izvori ne prijavljuju jednako potpune podatke: neki salju taksonomski
        # kod, neki samo znanstveni naziv. Odbacuje se samo ono bez oboje.
        if kod is None and not naziv:
            neidentificiranih += 1
            continue
        if kod is None:
            bez_koda += 1

        svojstva = p.get("svojstva") or {}
        svojstva_videna.update(svojstva.keys())

        rezultat = kol.update_one(
            {"_id": p.get("id_opazanja")},
            {
                "$set": {
                    "izvor": p.get("izvor"),
                    "taksonomski_kod": kod,
                    "znanstveni_naziv": naziv,
                    "polozaj": p.get("polozaj"),
                    "zabiljezeno_na": p.get("zabiljezeno_na"),
                    "svojstva": svojstva,
                    "svojstva_kljucevi": sorted(svojstva.keys()),
                },
                "$setOnInsert": {"ucitano_na": datetime.now(timezone.utc)},
            },
            upsert=True,
        )
        if rezultat.upserted_id is not None:
            novih += 1

    sazetak = {
        "korak": "opazanja",
        "procitano_poruka": len(poruke),
        "novih": novih,
        "vec_postojalo": len(poruke) - novih - neidentificiranih,
        "samo_naziv_bez_koda": bez_koda,
        "neidentificiranih": neidentificiranih,
        "razlicitih_bioloskih_svojstava": sorted(svojstva_videna),
        "u_kolekciji": kol.count_documents({}),
    }

    print(
        f"Novih {novih}, vec postojalo {sazetak['vec_postojalo']}, "
        f"razlicitih bioloskih polja {len(svojstva_videna)}."
    )

    cilj = osiguraj_direktorij(izvjestaj)
    cilj.write_text(json.dumps(sazetak, indent=2, ensure_ascii=False), encoding="utf-8")
    return sazetak


if __name__ == "__main__":
    pokreni(snakemake.config, snakemake.output[0])  # noqa: F821
