# CSharpLocalization

A C# localization library.
Provides JSON-based translations with dot notation, placeholder replacement, and automatic language detection.

## Features

- **JSON file storage**: One file per language (`lang/en.json`, `lang/de.json`)
- **Dot notation**: Access nested keys with `Lang("settings.block_completely")`
- **Placeholder replacement**: `:name` syntax with case-awareness
- **Auto-detect system language**: Uses Windows UI culture
- **Fallback language support**: Falls back to configured language if translation missing
- **Multiple path support**: `AddPath()` to overlay translations
- **Thread-safe caching**: Efficient for multi-threaded applications

## Installation

Add the project as a reference to your solution, or copy the source files.

Requires: **Newtonsoft.Json** (Json.NET)

## Usage

### Basic Setup

```csharp
using CSharpLocalization;

// Auto-detect language from Windows
var localization = new Localization(new LocalizationConfig
{
    LangDir = @".\lang\",
    DefaultLang = null,        // null = auto-detect
    FallbackLang = "en"
});

// Or specify explicit language
var localization = new Localization(new LocalizationConfig
{
    LangDir = @".\lang\",
    DefaultLang = "de",
    FallbackLang = "en"
});
```

### Getting Translations

```csharp
// Simple key
string title = localization.Lang("app.title");

// Nested key
string error = localization.Lang("messages.errors.not_found");

// With placeholder replacement
string greeting = localization.Lang("messages.welcome", new Dictionary<string, string>
{
    { ":name", "John" }
});
```

### Placeholder Case Awareness

The library automatically adjusts replacement case based on placeholder format:

- `:name` (lowercase) -> replacement in lowercase
- `:NAME` (uppercase) -> replacement in UPPERCASE
- `:Name` (pascal case) -> Replacement capitalized

### Language Switching

```csharp
// Change language at runtime
localization.SetLanguage("de");

// Get current language
string currentLang = localization.CurrentLanguage;
```

### Available Languages

```csharp
// Get list of available languages (for dropdown)
List<LanguageInfo> languages = localization.GetAvailableLanguages();

// Each LanguageInfo has:
// - Code: "en", "de", etc.
// - Name: "English", "Deutsch", etc. (from _meta_.language_name in JSON)
```

### Multiple Translation Paths

```csharp
// Add additional paths that override base translations
localization
    .AddPath(@".\lang\custom\")
    .AddPath(@".\lang\overrides\");
```

## JSON File Structure

### lang/en.json

```json
{
    "_meta_": {
        "language_name": "English"
    },
    "app": {
        "title": "My Application"
    },
    "messages": {
        "welcome": "Hello, :name!",
        "errors": {
            "not_found": "Item not found"
        }
    }
}
```

### lang/de.json

```json
{
    "_meta_": {
        "language_name": "Deutsch"
    },
    "app": {
        "title": "Meine Anwendung"
    },
    "messages": {
        "welcome": "Hallo, :name!",
        "errors": {
            "not_found": "Element nicht gefunden"
        }
    }
}
```

## Requirements

- .NET Framework 4.7.2 or later
- Newtonsoft.Json 13.0.3

## License

MIT
