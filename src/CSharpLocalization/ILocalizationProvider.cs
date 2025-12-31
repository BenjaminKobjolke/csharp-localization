using System.Collections.Generic;

namespace CSharpLocalization
{
    /// <summary>
    /// Interface for localization providers that load translations from different sources.
    /// </summary>
    public interface ILocalizationProvider
    {
        /// <summary>
        /// Loads all translations from the specified file.
        /// </summary>
        /// <param name="filePath">The full path to the translation file.</param>
        /// <returns>A dictionary containing the translations, with nested dictionaries for grouped keys.</returns>
        Dictionary<string, object> LoadAll(string filePath);

        /// <summary>
        /// Checks if a translation file exists at the specified path.
        /// </summary>
        /// <param name="filePath">The full path to check.</param>
        /// <returns>True if the file exists, false otherwise.</returns>
        bool FileExists(string filePath);

        /// <summary>
        /// Gets the file extension used by this provider (e.g., ".json").
        /// </summary>
        string FileExtension { get; }

        /// <summary>
        /// Gets the language name from a translation file's metadata.
        /// </summary>
        /// <param name="filePath">The full path to the translation file.</param>
        /// <returns>The language name, or null if not found.</returns>
        string GetLanguageName(string filePath);
    }
}
