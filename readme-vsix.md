# Rozszerzenie VSIX: **Generate diagram**

To rozszerzenie dodaje komendę kontekstową w **Solution Explorer**:

- **Generate diagram**

Komenda jest przeznaczona dla kliknięcia prawym przyciskiem myszy na solucję (`.sln` / `.slnx`) lub odpowiadający jej element w drzewie Solution Explorer.

## Co robi komenda

Po uruchomieniu komendy rozszerzenie:

1. Pobiera ścieżkę do aktywnej/zaznaczonej solucji.
2. Sprawdza, czy obok solucji istnieje plik `slnx-mermaid.yml`.
3. Jeśli pliku nie ma, tworzy domyślną konfigurację.
4. Ładuje konfigurację i generuje diagram zależności projektów przez `SlnxMermaid.Core`.
5. Zapisuje wynik do pliku markdown (domyślnie: `docs/dependencies.md`).

## Domyślna konfiguracja tworzona automatycznie

Tworzony plik `slnx-mermaid.yml` zawiera m.in.:

- `solution: <nazwa_pliku_sln_lub_slnx>`
- `diagram.direction: TD`
- podstawowe `filters.exclude`
- `output.file: docs/dependencies.md`

## Co powinieneś zobaczyć w Visual Studio

1. Uruchom instancję Experimental (`/rootsuffix Exp`) i zainstaluj/odpal rozszerzenie.
2. Otwórz solucję (`.sln` albo `.slnx`).
3. W **Solution Explorer** kliknij prawym na węzeł solucji.
4. Powinna być widoczna opcja: **Generate diagram**.
5. Po kliknięciu powinna pojawić się informacja o sukcesie, a plik diagramu powinien zostać zapisany na dysku.

## Gdzie szukać efektu

- konfiguracja: `slnx-mermaid.yml` (obok pliku solucji)
- diagram: `docs/dependencies.md` (chyba że konfiguracja wskazuje inny `output.file`)

## Uwagi praktyczne

- Komenda jest podpięta do menu kontekstowych Solution Explorer (węzeł solucji i konteksty pokrewne).
- Jeżeli komendy nie widać, upewnij się, że rozszerzenie jest załadowane w tej samej instancji VS, w której otwarta jest solucja.


## Dodatkowe miejsce w UI

Rozszerzenie dodaje również menu górne **Tools -> Mermaid -> Generate diagram**.
To ułatwia weryfikację, że rozszerzenie jest poprawnie załadowane, nawet jeśli kontekst w Solution Explorer jest nietypowy.
