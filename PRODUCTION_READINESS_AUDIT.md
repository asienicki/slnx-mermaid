# Audyt gotowości produkcyjnej — slnx-mermaid

Data audytu: 2026-08-10
Zakres: `master` @ `d349ce0` (najnowszy scalony commit, "chore: upgrade to .NET 10 and update packages (#109)")

## Kontekst

`slnx-mermaid` to narzędzie deweloperskie (dotnet tool CLI + rozszerzenie VSIX dla Visual Studio) generujące diagramy Mermaid z zależności projektów w plikach `.sln`/`.slnx`. To **nie jest usługa sieciowa/wieloużytkownikowa** — jest to lokalne narzędzie CLI oraz wtyczka IDE, publikowane jako pakiet NuGet i rozszerzenie na Visual Studio Marketplace. Klasyczne kryteria "production readiness" dla serwisów (skalowanie, SLA, obserwowalność, rate limiting) nie mają tu zastosowania. Właściwe pytanie brzmi: *czy narzędzie instaluje się i działa przewidywalnie na realnych rozwiązaniach, czy failuje w sposób czytelny, i czy pipeline wydawniczy jest bezpieczny i powtarzalny* — i z tej perspektywy przeprowadzono audyt.

**Ważny fakt wstępny:** projekt już jest w produkcji. Historia tagów sięga `v1.0.3` aż po `v17.3.5-rc`, pakiet jest opublikowany na NuGet (`v17.3.4`) i Visual Studio Marketplace (stabilny `17.3.4.0`, RC `17.3.5.0`). To nie jest pytanie "czy da się to wypuścić", tylko "czy obecny stan `master` utrzymuje jakość dotychczasowych wydań".

## Werdykt

**W dużej mierze tak — z jednym konkretnym błędem do naprawienia przed kolejnym wydaniem.** Infrastruktura CI/CD, bezpieczeństwo łańcucha dostaw i pokrycie testami są dojrzałe jak na projekt tej wielkości. Znaleziono jednak realny, odtwarzalny błąd w ścieżce wypisywania wyniku CLI (opisany niżej jako #1), który może spowodować, że narzędzie **nie zapisze diagramu i zakończy się wyjątkiem** dla w pełni legalnych danych wejściowych (nazwy plików/aliasy zawierające `[` lub `]`). To defekt jakościowy klasy "P1 dla CLI", nie luka bezpieczeństwa.

## Mocne strony

1. **Dojrzały CI/CD** — 14 workflowów: build+test (Linux, .NET 10), `dotnet format`, walidacja diagramu architektury (self-check), build VSIX, CodeQL, SonarQube, license check, skan podatności NuGet, walidacja przykładowego YAML wobec schematu, branch merge guard, publikacja GitHub Pages. Ostatni run na `master` — **zielony** (sprawdzone przez GitHub Actions API).
2. **Bezpieczeństwo łańcucha dostaw** — CodeQL, `CI-nuget-vulnerability-check` (fail buildu + komentarz na PR przy CVE w zależnościach, w tym tranzytywnych), license check, Dependabot skonfigurowany (`.github/dependabot.yml`). Brak sekretów w repo; workflowy mają domyślnie `permissions: contents: read` i podnoszą uprawnienia tylko tam, gdzie potrzeba.
3. **Spójność schematu konfiguracji wymuszana w CI** — `slnx-mermaid.schema.json` jest generowany z modelu C# i CI failuje, jeśli build zmienia wygenerowany plik bez commitu zmiany (`CI-configuration-schema.yml`), plus dedykowana walidacja przykładowego `slnx-mermaid.yml` wobec schematu.
4. **Bezpieczne parsowanie YAML konfiguracji** — `YamlConfigLoader`/`SlnxMermaidYamlSerializer` deserializują do silnie typowanego `SlnxMermaidConfig` (nie `object`/dynamic), więc nie ma powierzchni ataku typu "arbitrary type instantiation" znanej z niebezpiecznego użycia YamlDotNet.
5. **Czysta architektura warstwowa** — rozdzielone projekty `Configuration` / `Core` / `CLI` / `Gui.Avalonia` / `Vsix`, konsekwentne guardy `ArgumentNullException` w `Core`/`Emit`, `Core` celowo ograniczony do C# 7.3 / `net48`+`net10.0` dla kompatybilności z VS.
6. **Testy** — 24 pliki testów xUnit pokrywające emitter, analizator grafu, filtrowanie, naming, ładowanie/walidację/serializację konfiguracji, exceptions, extension methods.
7. **Brak otwartych bugów** — 1 otwarty issue w repo, i to feature request (#67), nie defekt.

## Znalezione problemy

### 1. [WYSOKI] Niesanitowana treść wynikowa przechodzi przez Spectre.Console Markup — realny crash na legalnych danych wejściowych

`src/SlnxMermaid.CLI/Commands/GenerateCommand.cs:46,78`:

```csharp
await HandleResult(config.Output?.File, mermaid, AnsiConsole.MarkupLine, cancellationToken);
...
var output = mermaid.WrapCodeForMarkdown();
markupLine(output);              // <-- drukuje PRZED zapisem pliku
...
await File.WriteAllTextAsync(configOutputFile, output, cancellationToken);
```

`AnsiConsole.MarkupLine` interpretuje `[...]` w tekście jako znaczniki Spectre.Console (`[red]...[/]`). Jeśli przekazany tekst zawiera nawias kwadratowy, który nie pasuje do rozpoznawanego stylu, Spectre.Console rzuca `MarkupException` **zanim plik z diagramem zostanie zapisany** — użytkownik nie dostaje ani diagramu na wyjściu, ani pliku.

Zweryfikowane wektory:

- **`naming.aliases` w konfiguracji YAML** — `NameTransformer.Transform` (`src/SlnxMermaid.Core/Naming/NameTransformer.cs:29-32`) wstawia wartość aliasu wprost, **bez sanityzacji**, jako identyfikator węzła w wygenerowanym Mermaidzie (`AppendEdge`, `MermaidEmitter.cs:196,207`). Dla porównania, zwykłe nazwy projektów przechodzą przez `ConvertToAllowedMermaidString` (`MermaidStringExtension.cs`), które usuwa wszystko poza literami/cyframi/`_`/`.`/spacją — ale **aliasy tej sanityzacji nie przechodzą**.
- **`ConfigurationValidator.ValidateDictionaryKeys`** (`src/SlnxMermaid.Configuration/Config/ConfigurationValidator.cs:63-74`) waliduje tylko, że **klucze** słownika aliasów nie są puste — **nigdy nie waliduje wartości**. Czyli konfiguracja z `naming.aliases: { MyProject: "[Legacy]" }` przechodzi walidację bez ostrzeżenia, trafia do wygenerowanego Mermaida jako złamana linia (`[Legacy] --> Foo` to niepoprawna składnia Mermaid), a następnie wysadza `AnsiConsole.MarkupLine`.
- **Ścieżka pliku konfiguracyjnego** — `GenerateCommand.cs:33`: `AnsiConsole.MarkupLine($"[green]Using config[/]: [grey]{Path.GetFileName(configPath)}[/]")`. Nazwa pliku podana przez `--config` nie jest escapowana. Nawiasy kwadratowe są w pełni legalnym znakiem w nazwach plików na Linuksie i Windows, więc samo `slnx-mermaid --config "moj[config].yml"` wywali się na tej linii, zanim jakikolwiek diagram zostanie wygenerowany.
- Ten sam wzorzec (niesanitowana interpolacja do `MarkupLine`) powtarza się w obsłudze wyjątków w `Program.cs:48,49,53,57` — ścieżka pliku w komunikacie błędu (`cfg.FilePath`, `snfe.FilePath`) też nie jest escapowana, więc narzędzie może wysypać się nawet **podczas raportowania błędu**.

**Dlaczego testy tego nie łapią:** `GenerateCommand.HandleResultTests.cs` wstrzykuje do `HandleResult` atrapę `Action<string> markupLine = _ => {}` zamiast prawdziwego `AnsiConsole.MarkupLine` — test sprawdza logikę zapisu pliku, ale nigdy nie przechodzi przez prawdziwy parser markupu Spectre.Console, więc błąd jest niewidoczny mimo 24 plików testowych w repo.

**Rekomendacja:** escapować przez `.EscapeMarkup()` każdy fragment tekstu pochodzący od użytkownika/systemu plików przed wstawieniem do wywołania `Markup`/`MarkupLine` (nazwy plików, komunikaty wyjątków, a zwłaszcza cały wygenerowany diagram), albo w ogóle nie przepuszczać wygenerowanego Mermaida przez parser markupu (np. `Console.WriteLine` albo `AnsiConsole.Write(new Text(output))`). Dodatkowo: `ConfigurationValidator` powinien walidować też **wartości** `naming.aliases` pod kątem znaków, które łamią składnię Mermaid.

### 2. [ŚREDNI] Brak progu pokrycia testami wymuszanego w CI na PR-ach

`CI-build-linux-net10.yml` uruchamia `dotnet test` bez zbierania/wymuszania pokrycia (Coverlet jest udokumentowany w `AGENTS.md` jako komenda ręczna / dla przepływu Sonara). `CI-sonar` liczy pokrycie, ale wg `CI.yml:52-57` uruchamia się tylko na `push` do `master` (`if: github.event_name == 'push' && github.ref == 'refs/heads/master'`) — nie na pull requestach. Oznacza to, że regresja pokrycia testami wchodzi do `master` zanim ktokolwiek ją zobaczy, zamiast być złapana przed mergem.

### 3. [NISKI] Brak `SECURITY.md`

Publiczny projekt open source, dystrybuowany przez NuGet i Visual Studio Marketplace, z aktywnym CodeQL/Dependabot/skanem podatności NuGet — ale bez udokumentowanego procesu/kontaktu do zgłaszania podatności. Tani do dodania, oczekiwany element dojrzałego repo.

### 4. [NISKI] Brak zbiorczego `CHANGELOG.md` dla CLI/NuGet

Istnieje `docs/vsix/CHANGELOG.md`, ale jest specyficzny dla VSIX. Mimo ~18 wydanych tagów nie ma changeloga dla pakietu NuGet/CLI — konsumenci muszą przekopywać historię gita, żeby zobaczyć, co zmieniło się między wersjami.

### 5. [NISKI] Wersjonowanie niewidoczne lokalnie

Pliki `.csproj` nie mają wpisanego `<Version>` — wersja (`17.3.x`) jest liczona wyłącznie w ręcznie odpalanym workflow `CD.yml` na podstawie tagów git. To działa, ale nie jest udokumentowane poza sekcją CD w README, a lokalny `dotnet pack`/`dotnet build` nie odtworzy realnego numeru wersji pakietu — może to mylić kontrybutorów.

## Priorytety naprawy

1. Napraw #1 przed kolejnym wydaniem — to jedyny znaleziony defekt, który realnie psuje podstawową funkcję narzędzia (wygenerowanie i zapisanie diagramu) na w pełni legalnych danych wejściowych, i to zarówno na ścieżce sukcesu, jak i na ścieżce raportowania błędów.
2. #2–#5 to porządkowanie jakościowe/procesowe — warte zrobienia, ale nieblokujące.
