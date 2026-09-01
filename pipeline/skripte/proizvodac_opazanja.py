"""Pomocna skripta: objavljuje opazanja ptica na Kafku.

U stvarnom sustavu poruke objavljuju ornitolozi. Ovdje ih simuliramo kako bi
korak konzumiranja imao sto citati.

Tri izvora namjerno salju razlicit skup bioloskih svojstava, jer zadatak trazi
da shema bude fleksibilna i da se polja ne hardkodiraju.
"""

import json
import random
import sys
from datetime import datetime, timedelta, timezone
from pathlib import Path

from kafka import KafkaProducer

sys.path.insert(0, str(Path(__file__).resolve().parent))
from zajednicko import kolekcija  # noqa: E402

IZVORI = ["ornitoloska-udruga", "terenski-biolog", "gradanska-znanost"]

STANISTA = ["suma", "mocvara", "livada", "obala", "gradski park", "planinski pojas"]
OBRASCI_LETA = ["klizeci", "lepetajuci", "valoviti", "lebdeci"]
MIGRACIJE = ["stanarica", "selica", "djelomicna selica", "lutalica"]


def bioloska_svojstva(izvor: str, r: random.Random) -> dict:
    """Svaki izvor biljezi svoja polja - namjerno nepodudarne sheme."""
    if izvor == "ornitoloska-udruga":
        svojstva = {
            "velicina_tijela_cm": round(r.uniform(8, 95), 1),
            "raspon_krila_cm": round(r.uniform(15, 240), 1),
            "status_migracije": r.choice(MIGRACIJE),
        }
    elif izvor == "terenski-biolog":
        svojstva = {
            "tjelesna_temperatura_c": round(r.uniform(38.5, 43.5), 1),
            "masa_g": round(r.uniform(5, 4500), 1),
            "obrazac_leta": r.choice(OBRASCI_LETA),
            "staniste": r.choice(STANISTA),
        }
    else:
        svojstva = {
            "staniste": r.choice(STANISTA),
            "broj_jedinki": r.randint(1, 40),
            "vrijeme_promatranja_min": r.randint(2, 120),
        }

    # Dio zapisa nosi i dodatna, rijetka polja - shema nije fiksna ni unutar izvora.
    if r.random() < 0.3:
        svojstva["napomena_terena"] = r.choice(
            ["gnijezdo u blizini", "mladunci prisutni", "glasanje u sumrak", "par"]
        )
    if r.random() < 0.2:
        svojstva["vjetar_kmh"] = round(r.uniform(0, 45), 1)

    return svojstva


def pokreni(config: dict, broj: int, sjeme: int = 7) -> int:
    r = random.Random(sjeme)

    iz_taksonomije = [
        {"naziv": v.get("canonicalName"), "kod": v["_id"]}
        for v in kolekcija(config, "taksonomija")
        .find({}, {"canonicalName": 1})
        .limit(120)
    ]
    if not iz_taksonomije:
        raise RuntimeError("Taksonomija je prazna - prvo pokreni korak taksonomija.")

    # Dio opazanja namjerno pokriva vrste koje je klasifikator stvarno prepoznao
    # iz snimaka. Inace se skupovi ne bi preklapali i izvjestaj bi ostao prazan.
    # Te vrste nisu u nasem uzorku GBIF-a, pa dolaze bez taksonomskog koda -
    # sto odgovara stvarnosti da izvori ne prijavljuju jednako potpune podatke.
    prepoznate = {
        p.get("znanstveni_naziv")
        for zapis in kolekcija(config, "klasifikacije").find({}, {"pogoci": 1})
        for p in zapis.get("pogoci", [])
        if p.get("znanstveni_naziv")
    }
    iz_klasifikacija = [{"naziv": n, "kod": None} for n in sorted(prepoznate)]

    if iz_klasifikacija:
        print(
            f"Vrsta iz taksonomije: {len(iz_taksonomije)}, "
            f"iz klasifikacija: {len(iz_klasifikacija)}."
        )

    proizvodac = KafkaProducer(
        bootstrap_servers=config["kafka"]["bootstrap"],
        value_serializer=lambda v: json.dumps(v, ensure_ascii=False).encode("utf-8"),
    )
    tema = config["kafka"]["tema"]
    sada = datetime.now(timezone.utc)

    for i in range(broj):
        if iz_klasifikacija and r.random() < 0.6:
            vrsta = r.choice(iz_klasifikacija)
        else:
            vrsta = r.choice(iz_taksonomije)
        izvor = r.choice(IZVORI)

        poruka = {
            "id_opazanja": f"{izvor}-{i:05d}",
            "izvor": izvor,
            "taksonomski_kod": vrsta["kod"],
            "znanstveni_naziv": vrsta["naziv"],
            "polozaj": {
                "sirina": round(r.uniform(42.4, 46.5), 5),
                "duzina": round(r.uniform(13.5, 19.4), 5),
            },
            "zabiljezeno_na": (sada - timedelta(days=r.randint(0, 240))).isoformat(),
            "svojstva": bioloska_svojstva(izvor, r),
        }
        proizvodac.send(tema, poruka)

    proizvodac.flush()
    proizvodac.close()
    print(f"Objavljeno {broj} opazanja na temu '{tema}'.")
    return broj


if __name__ == "__main__":
    import yaml

    korijen = Path(__file__).resolve().parents[1]
    c = yaml.safe_load((korijen / "config.yaml").read_text(encoding="utf-8"))
    n = int(sys.argv[1]) if len(sys.argv) > 1 else c["kafka"]["simulirano_poruka"]
    pokreni(c, n)
