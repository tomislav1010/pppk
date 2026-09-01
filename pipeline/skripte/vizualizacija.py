"""Korak 6: vizualizacija generiranog izvjestaja.

Cita CSV koji je proizveo prethodni korak i crta tri prikaza:
broj klasifikacija po vrsti, raspodjelu pouzdanosti i zastupljenost
bioloskih svojstava po izvorima opazanja.

Koristi se Agg backend jer se crta bez grafickog sucelja.
"""

import json
import sys
from pathlib import Path

import matplotlib

matplotlib.use("Agg")

import matplotlib.pyplot as plt  # noqa: E402
import pandas as pd  # noqa: E402

sys.path.insert(0, str(Path(__file__).resolve().parent))
from zajednicko import osiguraj_direktorij  # noqa: E402

BOJA = "#2c3e50"
BOJA2 = "#3498db"


def _prazan_prikaz(os_, poruka: str) -> None:
    os_.text(0.5, 0.5, poruka, ha="center", va="center", fontsize=11, color="grey")
    os_.set_xticks([])
    os_.set_yticks([])


def pokreni(config: dict, csv_ulaz: str, slika_izlaz: str, izvjestaj: str) -> dict:
    tablica = pd.read_csv(csv_ulaz)

    slika, osi = plt.subplots(1, 3, figsize=(18, 6))
    slika.suptitle(
        "Vrste ptica s barem jednom pozitivnom klasifikacijom", fontsize=14, y=0.98
    )

    # 1) broj klasifikacija po vrsti
    if tablica.empty:
        _prazan_prikaz(osi[0], "nema podataka")
    else:
        vrh = tablica.nlargest(12, "broj_klasificiranih_opazanja")
        osi[0].barh(vrh["vrsta"], vrh["broj_klasificiranih_opazanja"], color=BOJA)
        osi[0].invert_yaxis()
        osi[0].set_xlabel("broj klasificiranih snimaka")
        osi[0].xaxis.get_major_locator().set_params(integer=True)
    osi[0].set_title("Klasifikacije po vrsti")

    # 2) pouzdanost
    if tablica.empty:
        _prazan_prikaz(osi[1], "nema podataka")
    else:
        vrh = tablica.nlargest(12, "najveca_pouzdanost")
        polozaji = range(len(vrh))
        osi[1].barh(polozaji, vrh["najveca_pouzdanost"], color=BOJA2, label="najveca")
        osi[1].barh(
            polozaji, vrh["prosjecna_pouzdanost"], color=BOJA, height=0.45,
            label="prosjecna",
        )
        osi[1].set_yticks(list(polozaji))
        osi[1].set_yticklabels(vrh["vrsta"])
        osi[1].invert_yaxis()
        osi[1].set_xlim(0, 1)
        osi[1].set_xlabel("pouzdanost")
        osi[1].legend(loc="lower right", fontsize=8)
    osi[1].set_title("Pouzdanost klasifikacije")

    # 3) zastupljenost bioloskih svojstava iz opazanja
    brojac: dict[str, int] = {}
    if not tablica.empty:
        for zapis in tablica["bioloska_svojstva"].dropna():
            for svojstvo in str(zapis).split(";"):
                svojstvo = svojstvo.strip()
                if svojstvo:
                    brojac[svojstvo] = brojac.get(svojstvo, 0) + 1

    if not brojac:
        _prazan_prikaz(osi[2], "nema opazanja s Kafke")
    else:
        poredano = sorted(brojac.items(), key=lambda x: x[1], reverse=True)[:12]
        osi[2].barh([p[0] for p in poredano], [p[1] for p in poredano], color=BOJA2)
        osi[2].invert_yaxis()
        osi[2].set_xlabel("broj vrsta koje nose svojstvo")
        osi[2].xaxis.get_major_locator().set_params(integer=True)
    osi[2].set_title("Bioloska svojstva iz opazanja")

    slika.tight_layout(rect=(0, 0, 1, 0.95))

    cilj_slika = osiguraj_direktorij(slika_izlaz)
    slika.savefig(cilj_slika, dpi=130)
    plt.close(slika)

    sazetak = {
        "korak": "vizualizacija",
        "vrsta_prikazano": int(len(tablica)),
        "razlicitih_bioloskih_svojstava": len(brojac),
        "slika": str(cilj_slika),
    }

    print(f"Vizualizacija spremljena: {cilj_slika} ({len(tablica)} vrsta).")

    cilj = osiguraj_direktorij(izvjestaj)
    cilj.write_text(json.dumps(sazetak, indent=2, ensure_ascii=False), encoding="utf-8")
    return sazetak


if __name__ == "__main__":
    pokreni(
        snakemake.config,  # noqa: F821
        snakemake.input[0],  # noqa: F821
        snakemake.output[0],  # noqa: F821
        snakemake.output[1],  # noqa: F821
    )
