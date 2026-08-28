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

### WinForms ComboBox Binding

When binding `GetAvailableLanguages()` to a WinForms ComboBox with `DataSource`, set the selection in the `Form.Shown` event, not the constructor. The ComboBox isn't fully initialized until the form is displayed.

```csharp
private string _pendingLanguageSelection;

public Form1()
{
    InitializeComponent();

    // Setup ComboBox - set DisplayMember/ValueMember BEFORE DataSource
    cmbLanguage.DisplayMember = "Name";
    cmbLanguage.ValueMember = "Code";
    cmbLanguage.DataSource = _localization.GetAvailableLanguages();

    // Store saved language for later
    _pendingLanguageSelection = savedLanguageCode ?? _localization.CurrentLanguage;

    this.Shown += Form1_Shown;
}

private void Form1_Shown(object sender, EventArgs e)
{
    // Now ComboBox is ready - set selection
    var languages = cmbLanguage.DataSource as System.Collections.IList;
    if (languages != null)
    {
        for (int i = 0; i < languages.Count; i++)
        {
            dynamic lang = languages[i];
            if (lang?.Code == _pendingLanguageSelection)
            {
                cmbLanguage.SelectedIndex = i;
                break;
            }
        }
    }

    // Attach change handler AFTER setting initial selection
    cmbLanguage.SelectedIndexChanged += CmbLanguage_SelectedIndexChanged;
}
```

**Note:** Attach the `SelectedIndexChanged` handler after setting the initial selection to avoid triggering saves during initialization.

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
