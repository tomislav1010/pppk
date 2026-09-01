"""Korak 5: CSV izvjestaj o vrstama s barem jednom pozitivnom klasifikacijom.

Prije sastavljanja izvjestaja podaci se ciste i transformiraju: nazivi se
normaliziraju, pouzdanosti se svode na broj, dupli pogoci iste vrste u istoj
snimci se sazimaju na najbolji, a opazanja s Kafke se agregiraju po vrsti.

Opcionalni fuzzy filtar po nazivu vrste zadaje se preko
  --config filtar="cisticola"
i koristi rapidfuzz, pa hvata i priblizne pogotke i tipfelere.
"""

import json
import re
import sys
import unicodedata
from pathlib import Path

import pandas as pd
from rapidfuzz import fuzz

sys.path.insert(0, str(Path(__file__).resolve().parent))
from zajednicko import kolekcija, osiguraj_direktorij  # noqa: E402


def normaliziraj(naziv: str | None) -> str:
    """Bez dijakritike, bez viska razmaka, konzistentna velicina slova."""
    if not naziv:
        return ""
    bez_dijakritike = "".join(
        z
        for z in unicodedata.normalize("NFKD", str(naziv))
        if not unicodedata.combining(z)
    )
    return re.sub(r"\s+", " ", bez_dijakritike).strip()


def u_broj(vrijednost) -> float | None:
    try:
        broj = float(vrijednost)
    except (TypeError, ValueError):
        return None
    return broj if broj == broj else None  # odbacuje NaN


def pozitivni_pogoci(config: dict) -> pd.DataFrame:
    redci = []
    for zapis in kolekcija(config, "klasifikacije").find({}):
        for pogodak in zapis.get("pogoci", []):
            if not pogodak.get("pozitivna"):
                continue

            pouzdanost = u_broj(pogodak.get("pouzdanost"))
            if pouzdanost is None:
                continue

            redci.append(
                {
                    "vrsta": normaliziraj(pogodak.get("znanstveni_naziv")),
                    "obicni_naziv": normaliziraj(pogodak.get("obicni_naziv")),
                    "datoteka": zapis.get("_id"),
                    "snimka": zapis.get("naziv"),
                    "lokacija": (zapis.get("lokacija") or {}).get("naziv"),
                    "pouzdanost": pouzdanost,
                    "povezano": bool(pogodak.get("povezano")),
                    "porodica": pogodak.get("porodica"),
                    "red": pogodak.get("red"),
                }
            )

    okvir = pd.DataFrame(redci)
    if okvir.empty:
        return okvir

    okvir = okvir[okvir["vrsta"] != ""]

    # Ista vrsta moze biti prepoznata vise puta u istoj snimci (razliciti odsjecci);
    # za statistiku se broji jedna klasifikacija po snimci, s najboljom pouzdanoscu.
    okvir = (
        okvir.sort_values("pouzdanost", ascending=False)
        .drop_duplicates(subset=["vrsta", "datoteka"], keep="first")
        .reset_index(drop=True)
    )
    return okvir


def opazanja_po_vrsti(config: dict) -> dict[str, dict]:
    sazeto: dict[str, dict] = {}

    for o in kolekcija(config, "opazanja").find({}):
        naziv = normaliziraj(o.get("znanstveni_naziv"))
        if not naziv:
            continue

        stavka = sazeto.setdefault(
            naziv,
            {
                "broj_opazanja": 0,
                "izvori": set(),
                "svojstva": set(),
                "sirine": [],
                "duzine": [],
            },
        )
        stavka["broj_opazanja"] += 1
        if o.get("izvor"):
            stavka["izvori"].add(o["izvor"])
        stavka["svojstva"].update(o.get("svojstva_kljucevi") or [])

        polozaj = o.get("polozaj") or {}
        sirina, duzina = u_broj(polozaj.get("sirina")), u_broj(polozaj.get("duzina"))
        if sirina is not None and duzina is not None:
            stavka["sirine"].append(sirina)
            stavka["duzine"].append(duzina)

    return sazeto


def primijeni_filtar(okvir: pd.DataFrame, filtar: str, prag: int) -> pd.DataFrame:
    filtar = (filtar or "").strip()
    if not filtar or okvir.empty:
        return okvir

    # Trazi se i po znanstvenom i po obicnom nazivu, jer je korisniku prirodno
    # upisati "antthrush" jednako kao "Chamaeza". Registar se spusta jer je
    # WRatio osjetljiv na velika slova.
    upit = normaliziraj(filtar).lower()
    kandidati: dict[str, list[str]] = {}
    for _, red in okvir[["vrsta", "obicni_naziv"]].drop_duplicates().iterrows():
        nazivi = [red["vrsta"]]
        if red["obicni_naziv"]:
            nazivi.append(red["obicni_naziv"])
        kandidati.setdefault(red["vrsta"], []).extend(nazivi)

    ocjene: dict[str, float] = {}
    for vrsta, nazivi in kandidati.items():
        najbolja = max(
            (fuzz.WRatio(upit, n.lower()) for n in nazivi if n),
            default=0.0,
        )
        if najbolja >= prag:
            ocjene[vrsta] = najbolja

    print(
        f"Fuzzy filtar '{filtar}' (prag {prag}): "
        f"{len(ocjene)} od {len(kandidati)} vrsta."
    )
    for vrsta, ocjena in sorted(ocjene.items(), key=lambda x: -x[1])[:10]:
        print(f"  {vrsta}  ({ocjena:.0f})")

    return okvir[okvir["vrsta"].isin(ocjene)]


def pokreni(config: dict, csv_izlaz: str, izvjestaj: str) -> dict:
    postavke = config["izvjestaj"]
    pogoci = pozitivni_pogoci(config)
    ukupno_vrsta = 0 if pogoci.empty else pogoci["vrsta"].nunique()

    pogoci = primijeni_filtar(pogoci, postavke.get("filtar", ""), postavke["fuzzy_prag"])
    opazanja = opazanja_po_vrsti(config)

    if pogoci.empty:
        stupci = [
            "vrsta", "obicni_naziv", "broj_klasificiranih_opazanja", "broj_snimaka",
            "prosjecna_pouzdanost", "najveca_pouzdanost", "lokacije_snimaka",
            "povezano_s_taksonomijom", "porodica", "red", "broj_opazanja_kafka",
            "izvori_opazanja", "bioloska_svojstva", "prosjecna_sirina", "prosjecna_duzina",
        ]
        tablica = pd.DataFrame(columns=stupci)
    else:
        skupine = pogoci.groupby("vrsta", as_index=False).agg(
            obicni_naziv=("obicni_naziv", "first"),
            broj_klasificiranih_opazanja=("datoteka", "count"),
            broj_snimaka=("datoteka", "nunique"),
            prosjecna_pouzdanost=("pouzdanost", "mean"),
            najveca_pouzdanost=("pouzdanost", "max"),
            lokacije_snimaka=("lokacija", lambda s: "; ".join(sorted({x for x in s if x}))),
            povezano_s_taksonomijom=("povezano", "max"),
            porodica=("porodica", "first"),
            red=("red", "first"),
        )

        def iz_opazanja(naziv: str, polje: str):
            return opazanja.get(naziv, {}).get(polje)

        skupine["broj_opazanja_kafka"] = skupine["vrsta"].map(
            lambda n: iz_opazanja(n, "broj_opazanja") or 0
        )
        skupine["izvori_opazanja"] = skupine["vrsta"].map(
            lambda n: "; ".join(sorted(iz_opazanja(n, "izvori") or []))
        )
        skupine["bioloska_svojstva"] = skupine["vrsta"].map(
            lambda n: "; ".join(sorted(iz_opazanja(n, "svojstva") or []))
        )
        skupine["prosjecna_sirina"] = skupine["vrsta"].map(
            lambda n: _prosjek(iz_opazanja(n, "sirine"))
        )
        skupine["prosjecna_duzina"] = skupine["vrsta"].map(
            lambda n: _prosjek(iz_opazanja(n, "duzine"))
        )

        skupine["prosjecna_pouzdanost"] = skupine["prosjecna_pouzdanost"].round(4)
        skupine["najveca_pouzdanost"] = skupine["najveca_pouzdanost"].round(4)

        tablica = skupine.sort_values(
            ["broj_klasificiranih_opazanja", "najveca_pouzdanost"], ascending=False
        ).reset_index(drop=True)

    cilj_csv = osiguraj_direktorij(csv_izlaz)
    tablica.to_csv(cilj_csv, index=False, encoding="utf-8")

    sazetak = {
        "korak": "izvjestaj",
        "vrsta_s_pozitivnom_klasifikacijom": ukupno_vrsta,
        "vrsta_u_izvjestaju": int(len(tablica)),
        "filtar": postavke.get("filtar", "") or None,
        "fuzzy_prag": postavke["fuzzy_prag"],
        "csv": str(cilj_csv),
    }

    print(
        f"Vrsta s pozitivnom klasifikacijom: {ukupno_vrsta}, "
        f"u izvjestaju: {len(tablica)}. CSV: {cilj_csv}"
    )

    cilj = osiguraj_direktorij(izvjestaj)
    cilj.write_text(json.dumps(sazetak, indent=2, ensure_ascii=False), encoding="utf-8")
    return sazetak


def _prosjek(vrijednosti) -> float | None:
    if not vrijednosti:
        return None
    return round(sum(vrijednosti) / len(vrijednosti), 5)


if __name__ == "__main__":
    pokreni(snakemake.config, snakemake.output[0], snakemake.output[1])  # noqa: F821
