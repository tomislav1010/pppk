"""Pomocna skripta: generira sintetske audio zapise za testiranje pipelinea.

Direktorij s pravim snimkama nije u repozitoriju, pa bez ovoga na cistom
klonu i u CI-u nema sto obradivati. Zapisi su obicni tonovi sa sumom - sluze
da se provjeri tok podataka, ne da daju smislenu klasifikaciju.

  python skripte/pripremi_test_audio.py [broj_po_lokaciji]
"""

import math
import random
import struct
import sys
import wave
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from zajednicko import PIPELINE  # noqa: E402

FREKVENCIJA_UZORKOVANJA = 22050
TRAJANJE_S = 3


def napisi(putanja: Path, frekvencije: list[int], tempo: float, sjeme: int) -> None:
    r = random.Random(sjeme)
    okviri = []

    for t in range(FREKVENCIJA_UZORKOVANJA * TRAJANJE_S):
        v = sum(
            math.sin(2 * math.pi * f * t / FREKVENCIJA_UZORKOVANJA)
            for f in frekvencije
        ) / len(frekvencije)
        v *= 0.5 + 0.5 * math.sin(2 * math.pi * tempo * t / FREKVENCIJA_UZORKOVANJA)
        v += r.uniform(-0.08, 0.08)
        okviri.append(struct.pack("<h", int(max(-1.0, min(1.0, v)) * 14000)))

    putanja.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(putanja), "w") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(FREKVENCIJA_UZORKOVANJA)
        w.writeframes(b"".join(okviri))


def pokreni(config: dict, po_lokaciji: int = 5, sjeme: int = 99) -> int:
    korijen = PIPELINE / config["audio"]["direktorij"]
    r = random.Random(sjeme)
    napravljeno = 0

    for kljuc in config["audio"]["lokacije"]:
        for i in range(1, po_lokaciji + 1):
            cilj = korijen / kljuc / f"snimka-{i:02d}.wav"
            if cilj.exists():
                continue

            frekvencije = sorted(
                r.sample(range(900, 6500, 100), r.choice([2, 3, 4]))
            )
            napisi(cilj, frekvencije, r.choice([3, 5, 7, 9]), sjeme + napravljeno)
            napravljeno += 1

    print(f"Napravljeno {napravljeno} testnih snimaka u {korijen}.")
    return napravljeno


if __name__ == "__main__":
    import yaml

    c = yaml.safe_load((PIPELINE / "config.yaml").read_text(encoding="utf-8"))
    pokreni(c, int(sys.argv[1]) if len(sys.argv) > 1 else 5)
