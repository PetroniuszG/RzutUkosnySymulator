# RzutUkosnySymulator

## Symulator rzutu ukośnego

Interaktywna aplikacja do wizualizacji i symulacji rzutu ukośnego w środowisku 2D. Projekt umożliwia eksperymentowanie z różnymi parametrami fizycznymi wpływającymi na tor lotu obiektu.

## Opis

Aplikacja RzutUkosnySymulator pozwala na obserwację wpływu różnych parametrów fizycznych na tor rzutu ukośnego. Użytkownik może modyfikować prędkość początkową, kąt rzutu, wysokość początkową, opór powietrza oraz inne parametry, obserwując w czasie rzeczywistym zmiany w zachowaniu obiektu.

## Funkcje

- Interaktywna wizualizacja toru rzutu ukośnego
- Regulacja parametrów fizycznych:
  - Prędkość początkowa
  - Kąt rzutu
  - Wysokość początkowa
  - Współczynnik oporu powietrza
  - Przyspieszenie grawitacyjne
- Wyświetlanie danych w czasie rzeczywistym:
  - Maksymalna wysokość
  - Zasięg rzutu
  - Czas lotu
- Możliwość zapisywania i porównywania różnych symulacji
- Animacja ruchu obiektu

## Wymagania

- Python 3.x
- PyQt5
- NumPy
- Matplotlib

## Instalacja

```bash
# Klonowanie repozytorium
git clone https://github.com/PetroniuszG/RzutUkosnySymulator.git
cd RzutUkosnySymulator

# Instalacja zależności
pip install -r requirements.txt

# Uruchomienie aplikacji
python main.py
```

## Używanie

1. Uruchom aplikację
2. Dostosuj parametry początkowe za pomocą suwaków lub pól tekstowych
3. Kliknij przycisk "Start" aby rozpocząć symulację
4. Obserwuj trajektorię i dane w czasie rzeczywistym
5. Użyj przycisku "Reset" aby rozpocząć nową symulację
6. Opcjonalnie, zapisz wyniki aby później je porównać

## Struktura projektu

```
RzutUkosnySymulator/
├── main.py              # Główny plik uruchomieniowy
├── simulator.py         # Silnik symulacji fizycznej
├── ui/
│   ├── main_window.py   # Interfejs użytkownika
│   └── plot_widget.py   # Komponent do wizualizacji
├── physics/
│   ├── equations.py     # Równania ruchu
│   └── constants.py     # Stałe fizyczne
└── assets/              # Pliki graficzne i zasoby
```

## Podstawy fizyczne

Symulator opiera się na równaniach rzutu ukośnego z uwzględnieniem oporu powietrza:

- Bez oporu powietrza:
  - x(t) = v₀·cos(α)·t
  - y(t) = h₀ + v₀·sin(α)·t - (g·t²)/2

- Z oporem powietrza:
  - Równania różniczkowe uwzględniające współczynnik oporu powietrza i masę obiektu

## Autorzy

- [PetroniuszG](https://github.com/PetroniuszG)

## Licencja

Ten projekt jest udostępniony na licencji MIT. Szczegóły w pliku LICENSE.

## Zgłaszanie błędów

Jeśli znajdziesz błąd lub masz propozycję ulepszenia, prosimy o utworzenie nowego zgłoszenia w zakładce "Issues" na GitHubie.
