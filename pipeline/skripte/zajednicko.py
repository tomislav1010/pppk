"""Zajednicki pomocnici za sve korake pipelinea."""

import os
from pathlib import Path

from dotenv import load_dotenv
from pymongo import MongoClient

KORIJEN = Path(__file__).resolve().parents[2]


def ucitaj_okolinu() -> None:
    """Kredencijali se citaju iz .env u korijenu repozitorija, istog koji koristi docker compose."""
    load_dotenv(KORIJEN / ".env")


def mongo_klijent(config: dict) -> MongoClient:
    ucitaj_okolinu()

    korisnik = os.getenv("MONGO_USER", "root")
    lozinka = os.getenv("MONGO_PASSWORD", "root123")
    host = config["mongo"]["host"]
    port = config["mongo"]["port"]

    return MongoClient(
        f"mongodb://{korisnik}:{lozinka}@{host}:{port}/?authSource=admin",
        serverSelectionTimeoutMS=5000,
    )


def mongo_baza(config: dict):
    return mongo_klijent(config)[config["mongo"]["baza"]]


def kolekcija(config: dict, naziv: str):
    return mongo_baza(config)[config["mongo"]["kolekcije"][naziv]]


def osiguraj_direktorij(putanja: str | Path) -> Path:
    p = Path(putanja)
    p.parent.mkdir(parents=True, exist_ok=True)
    return p
