using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CSharpLocalization
{
    /// <summary>
    /// Main localization class providing translation functionality.
    /// Similar to the PHP localization library.
    /// </summary>
    public sealed class Localization
    {
        private readonly LocalizationConfig _config;
        private readonly ILocalizationProvider _provider;
        private readonly TranslationCache _cache;
        private readonly PlaceholderReplacer _replacer;
        private readonly List<string> _additionalPaths;
        private readonly object _lock = new object();

        private string _currentLanguage;
        private Dictionary<string, object> _mergedTranslations;

        /// <summary>
        /// Gets the current language code.
        /// </summary>
        public string CurrentLanguage => _currentLanguage;

        /// <summary>
        /// Creates a new Localization instance.
        /// </summary>
        /// <param name="config">The localization configuration.</param>
        public Localization(LocalizationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _config.Validate();

            // Select provider based on configuration
            if (config.UseEmbeddedResources && config.ResourceAssembly != null)
            {
                _provider = new EmbeddedResourceLocalizationProvider(
                    config.ResourceAssembly,
                    config.ResourcePrefix ?? ""
                );
            }
            else
            {
                _provider = new JsonLocalizationProvider();
            }

            _cache = new TranslationCache();
            _replacer = new PlaceholderReplacer();
            _additionalPaths = new List<string>();

            // Determine the language to use
            _currentLanguage = DetermineLanguage(config.DefaultLang);

            // Load initial translations
            _mergedTranslations = LoadMergedTranslations();
        }

        /// <summary>
        /// Gets a translation by key with optional placeholder replacement.
        /// </summary>
        /// <param name="key">The dot-notation key (e.g., "site.title").</param>
        /// <param name="replacements">Optional dictionary of placeholder replacements.</param>
        /// <returns>The translated string, or empty string if not found.</returns>
        public string Lang(string key, Dictionary<string, string> replacements = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            var value = DotNotationParser.GetValue(_mergedTranslations, key);

            if (value == null)
            {
                // Try fallback language if different from current
                if (!string.IsNullOrEmpty(_config.FallbackLang) &&
                    !string.Equals(_config.FallbackLang, _currentLanguage, StringComparison.OrdinalIgnoreCase))
                {
                    var fallbackTranslations = LoadTranslationsForLanguage(_config.FallbackLang);
                    value = DotNotationParser.GetValue(fallbackTranslations, key);
                }
            }

            if (value == null)
            {
                return string.Empty;
            }

            string result = value.ToString();

            if (replacements != null && replacements.Count > 0)
            {
                result = _replacer.Replace(result, replacements);
            }

            return result;
        }

        /// <summary>
        /// Adds an additional translation path. Translations from this path
        /// will override translations from the base path.
        /// </summary>
        /// <param name="path">The path to add (must end with directory separator).</param>
        /// <returns>This instance for method chaining.</returns>
        public Localization AddPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return this;
            }

            // Ensure path ends with directory separator
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
                !path.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                path = path + Path.DirectorySeparatorChar;
            }

            lock (_lock)
            {
                _additionalPaths.Add(path);
                _cache.Invalidate();
                _mergedTranslations = LoadMergedTranslations();
            }

            return this;
        }

        /// <summary>
        /// Changes the current language and reloads translations.
        /// </summary>
        /// <param name="lang">The language code to switch to.</param>
        public void SetLanguage(string lang)
        {
            if (string.IsNullOrWhiteSpace(lang))
            {
                return;
            }

            lock (_lock)
            {
                _currentLanguage = lang;
                _cache.Invalidate();
                _mergedTranslations = LoadMergedTranslations();
            }
        }

        /// <summary>
        /// Clears the translation cache, forcing a reload on next access.
        /// </summary>
        public void ClearCache()
        {
            lock (_lock)
            {
                _cache.Invalidate();
                _mergedTranslations = LoadMergedTranslations();
            }
        }

        /// <summary>
        /// Gets a list of available languages based on JSON files in the lang directory
        /// or embedded resources.
        /// </summary>
        /// <returns>A list of available languages with their codes and display names.</returns>
        public List<LanguageInfo> GetAvailableLanguages()
        {
            var languages = new List<LanguageInfo>();

            // Try to load language names from languages.json
            Dictionary<string, string> languageNames = null;
            if (_config.UseEmbeddedResources)
            {
                // Try to load languages.json from embedded resources
                if (_provider.FileExists("languages"))
                {
                    var langData = _provider.LoadAll("languages");
                    languageNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var kvp in langData)
                    {
                        if (kvp.Value is string name)
                        {
                            languageNames[kvp.Key] = name;
                        }
                    }
                }
            }
            else if (!string.IsNullOrEmpty(_config.LangDir))
            {
                // Try to load languages.json from file
                string languagesFile = Path.Combine(_config.LangDir, "languages.json");
                if (_provider.FileExists(languagesFile))
                {
                    var langData = _provider.LoadAll(languagesFile);
                    languageNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var kvp in langData)
                    {
                        if (kvp.Value is string name)
                        {
                            languageNames[kvp.Key] = name;
                        }
                    }
                }
            }

            // If using embedded resources, get languages from embedded provider
            if (_config.UseEmbeddedResources && _provider is EmbeddedResourceLocalizationProvider embeddedProvider)
            {
                var codes = embeddedProvider.GetAvailableLanguageCodes();
                foreach (var code in codes)
                {
                    // Skip the languages file itself
                    if (code.Equals("languages", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string name = null;

                    // First try languages.json
                    if (languageNames != null && languageNames.TryGetValue(code, out string langName))
                    {
                        name = langName;
                    }

                    // Fallback to _meta_.language_name in the lang file
                    if (string.IsNullOrEmpty(name))
                    {
                        name = embeddedProvider.GetLanguageName(code);
                    }

                    // Fallback to CultureInfo
                    if (string.IsNullOrEmpty(name))
                    {
                        try
                        {
                            var culture = new CultureInfo(code);
                            name = culture.NativeName;
                        }
                        catch
                        {
                            name = code.ToUpperInvariant();
                        }
                    }

                    languages.Add(new LanguageInfo(code, name));
                }

                // Sort by name
                languages.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
                return languages;
            }

            // File-based mode
            if (string.IsNullOrEmpty(_config.LangDir) || !Directory.Exists(_config.LangDir))
            {
                return languages;
            }

            var jsonFiles = Directory.GetFiles(_config.LangDir, "*" + _provider.FileExtension);

            foreach (var file in jsonFiles)
            {
                string code = Path.GetFileNameWithoutExtension(file);

                // Skip metadata-only files, files starting with underscore, and languages.json
                if (code.StartsWith("_") || code.Equals("languages", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string name = null;

                // First try languages.json
                if (languageNames != null && languageNames.TryGetValue(code, out string langName))
                {
                    name = langName;
                }

                // Fallback to _meta_.language_name in the lang file
                if (string.IsNullOrEmpty(name))
                {
                    name = _provider.GetLanguageName(file);
                }

                // Fallback to CultureInfo
                if (string.IsNullOrEmpty(name))
                {
                    try
                    {
                        var culture = new CultureInfo(code);
                        name = culture.NativeName;
                    }
                    catch
                    {
                        name = code.ToUpperInvariant();
                    }
                }

                languages.Add(new LanguageInfo(code, name));
            }

            // Sort by name
            languages.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            return languages;
        }

        /// <summary>
        /// Determines the language to use based on configuration and system settings.
        /// </summary>
        private string DetermineLanguage(string configuredLang)
        {
            // If a language is explicitly configured, use it
            if (!string.IsNullOrWhiteSpace(configuredLang))
            {
                // Check if translation exists for this language
                if (_config.UseEmbeddedResources)
                {
                    // For embedded resources, pass just the lang code (provider adds extension)
                    if (_provider.FileExists(configuredLang))
                    {
                        return configuredLang;
                    }
                }
                else
                {
                    string filePath = Path.Combine(_config.LangDir, configuredLang + _provider.FileExtension);
                    if (_provider.FileExists(filePath))
                    {
                        return configuredLang;
                    }
                }
            }

            // Auto-detect from Windows UI culture
            var uiCulture = CultureInfo.CurrentUICulture;
            var culturesToTry = new List<string>
            {
                uiCulture.Name.Replace("-", "_"),      // e.g., "de_DE"
                uiCulture.TwoLetterISOLanguageName,    // e.g., "de"
                uiCulture.Name,                         // e.g., "de-DE"
            };

            foreach (var culture in culturesToTry)
            {
                if (_config.UseEmbeddedResources)
                {
                    // For embedded resources, pass just the lang code (provider adds extension)
                    if (_provider.FileExists(culture))
                    {
                        return culture;
                    }
                }
                else
                {
                    string filePath = Path.Combine(_config.LangDir, culture + _provider.FileExtension);
                    if (_provider.FileExists(filePath))
                    {
                        return culture;
                    }
                }
            }

            // Fall back to fallback language
            if (!string.IsNullOrWhiteSpace(_config.FallbackLang))
            {
                return _config.FallbackLang;
            }

            // Ultimate fallback
            return "en";
        }

        /// <summary>
        /// Loads and merges translations from all configured paths or embedded resources.
        /// </summary>
        private Dictionary<string, object> LoadMergedTranslations()
        {
            return _cache.GetOrLoad($"merged_{_currentLanguage}", () =>
            {
                Dictionary<string, object> result = new Dictionary<string, object>();

                // Embedded resource mode - simpler loading
                // Pass just the lang code (provider adds extension)
                if (_config.UseEmbeddedResources)
                {
                    if (_provider.FileExists(_currentLanguage))
                    {
                        result = _provider.LoadAll(_currentLanguage);
                    }
                    else if (!string.IsNullOrWhiteSpace(_config.FallbackLang))
                    {
                        // Try fallback
                        if (_provider.FileExists(_config.FallbackLang))
                        {
                            result = _provider.LoadAll(_config.FallbackLang);
                        }
                    }
                    return result;
                }

                // File-based mode
                // 1. Load from defaultLangDir if configured
                if (!string.IsNullOrWhiteSpace(_config.DefaultLangDir))
                {
                    var defaultTranslations = LoadFromPath(_config.DefaultLangDir);
                    result = TranslationMerger.DeepMerge(result, defaultTranslations);
                }

                // 2. Load from primary langDir
                var primaryTranslations = LoadFromPath(_config.LangDir);
                result = TranslationMerger.DeepMerge(result, primaryTranslations);

                // 3. Load from additional paths (in order, each overriding the previous)
                foreach (var path in _additionalPaths)
                {
                    var additionalTranslations = LoadFromPath(path);
                    result = TranslationMerger.DeepMerge(result, additionalTranslations);
                }

                return result;
            });
        }

        /// <summary>
        /// Loads translations from a specific path for the current language.
        /// </summary>
        private Dictionary<string, object> LoadFromPath(string path)
        {
            string filePath = Path.Combine(path, _currentLanguage + _provider.FileExtension);

            if (_provider.FileExists(filePath))
            {
                return _provider.LoadAll(filePath);
            }

            // Try fallback language
            if (!string.IsNullOrWhiteSpace(_config.FallbackLang) &&
                !string.Equals(_config.FallbackLang, _currentLanguage, StringComparison.OrdinalIgnoreCase))
            {
                string fallbackPath = Path.Combine(path, _config.FallbackLang + _provider.FileExtension);
                if (_provider.FileExists(fallbackPath))
                {
                    return _provider.LoadAll(fallbackPath);
                }
            }

            return new Dictionary<string, object>();
        }

        /// <summary>
        /// Loads translations for a specific language.
        /// </summary>
        private Dictionary<string, object> LoadTranslationsForLanguage(string lang)
        {
            return _cache.GetOrLoad($"lang_{lang}", () =>
            {
                if (_config.UseEmbeddedResources)
                {
                    // For embedded resources, pass just the lang code (provider adds extension)
                    if (_provider.FileExists(lang))
                    {
                        return _provider.LoadAll(lang);
                    }
                }
                else
                {
                    string filePath = Path.Combine(_config.LangDir, lang + _provider.FileExtension);
                    if (_provider.FileExists(filePath))
                    {
                        return _provider.LoadAll(filePath);
                    }
                }

                return new Dictionary<string, object>();
            });
        }
    }
}
