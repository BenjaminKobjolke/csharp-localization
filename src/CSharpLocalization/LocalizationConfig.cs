using System;
using System.IO;
using System.Reflection;

namespace CSharpLocalization
{
    /// <summary>
    /// Configuration for the localization system.
    /// </summary>
    public class LocalizationConfig
    {
        /// <summary>
        /// Base directory for language files. Must end with a directory separator.
        /// Example: "C:\MyApp\lang\"
        /// </summary>
        public string LangDir { get; set; }

        /// <summary>
        /// Default language code (e.g., "en", "de").
        /// If null, the system will auto-detect from Windows UI culture.
        /// </summary>
        public string DefaultLang { get; set; }

        /// <summary>
        /// Fallback language code to use when a translation is not found
        /// in the default language. Typically "en".
        /// </summary>
        public string FallbackLang { get; set; } = "en";

        /// <summary>
        /// Optional base translations directory for merging.
        /// Translations from this directory are loaded first, then overridden
        /// by translations from LangDir.
        /// </summary>
        public string DefaultLangDir { get; set; }

        /// <summary>
        /// When true, loads translations from embedded resources instead of files.
        /// Requires ResourceAssembly and ResourcePrefix to be set.
        /// </summary>
        public bool UseEmbeddedResources { get; set; } = false;

        /// <summary>
        /// The assembly containing embedded language resources.
        /// Required when UseEmbeddedResources is true.
        /// </summary>
        public Assembly ResourceAssembly { get; set; }

        /// <summary>
        /// Prefix for embedded resource names, e.g., "MyApp.lang."
        /// The full resource name will be: ResourcePrefix + langCode + ".json"
        /// </summary>
        public string ResourcePrefix { get; set; }

        /// <summary>
        /// Validates the configuration and throws exceptions if invalid.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when required configuration is missing or invalid.</exception>
        public void Validate()
        {
            if (UseEmbeddedResources)
            {
                // Embedded resource mode - require assembly
                if (ResourceAssembly == null)
                {
                    throw new ArgumentException("ResourceAssembly is required when UseEmbeddedResources is true.", nameof(ResourceAssembly));
                }
            }
            else
            {
                // File-based mode - require LangDir
                if (string.IsNullOrWhiteSpace(LangDir))
                {
                    throw new ArgumentException("LangDir is required when not using embedded resources.", nameof(LangDir));
                }

                // Ensure LangDir ends with directory separator
                if (!LangDir.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
                    !LangDir.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                {
                    LangDir = LangDir + Path.DirectorySeparatorChar;
                }

                // Normalize DefaultLangDir if provided
                if (!string.IsNullOrWhiteSpace(DefaultLangDir))
                {
                    if (!DefaultLangDir.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
                        !DefaultLangDir.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                    {
                        DefaultLangDir = DefaultLangDir + Path.DirectorySeparatorChar;
                    }
                }
            }
        }
    }
}
