# slnx-mermaid Avalonia configuration editor

This project is a desktop editor and verifier for the shared `SlnxMermaidConfig` model.
It intentionally does not define a second UI-specific copy of the configuration structure.
The configuration model remains the source of truth and is imported from `SlnxMermaid.Configuration`.

## Architecture

- `SlnxMermaid.Configuration` owns configuration models, metadata attributes, YAML/JSON serialization, and validation services.
- `SlnxMermaid.Gui.Avalonia` owns only presentation concerns: Avalonia views, MVVM view models, commands, and dynamic form building.
- `ConfigurationFormBuilder` is the only place where reflection over configuration properties is performed.
- Field view models keep a reference to the source object/property so user edits are written back to the shared model instance.
- Avalonia `DataTemplate`s map dynamic field view models to controls, so no XAML is generated per configuration property.

## Dynamic field mapping

| Model type | Field view model | Avalonia view |
| --- | --- | --- |
| `string` | `TextFieldViewModel` | `TextFieldView` |
| `bool` | `BooleanFieldViewModel` | `BooleanFieldView` |
| `enum` | `EnumFieldViewModel` | `EnumFieldView` |
| `int`, `double`, `decimal` | `NumericFieldViewModel` | `NumericFieldView` |
| `IList` | `ListFieldViewModel` | `ListFieldView` |
| `IDictionary` | `DictionaryFieldViewModel` | `DictionaryFieldView` |
| nested object | `ObjectFieldViewModel` | `ObjectFieldView` |

Optional metadata attributes such as `ConfigurationDisplayNameAttribute` and
`ConfigurationDescriptionAttribute` improve labels and descriptions, but the UI works without them.
When attributes are absent, the builder derives a display label from the property name.

## Running

```bash
dotnet run --project src/SlnxMermaid.Gui.Avalonia/SlnxMermaid.Gui.Avalonia.csproj
```

The editor loads and saves the path in the path textbox, defaulting to `slnx-mermaid.yml`.

## Testing

```bash
dotnet test SlnxMermaid.slnx
```

The GUI tests cover dynamic field selection, enum value discovery, nested objects, source model updates,
YAML serialization after UI edits, and the absence of duplicated configuration models in the GUI assembly.
