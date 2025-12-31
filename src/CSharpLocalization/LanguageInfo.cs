namespace CSharpLocalization
{
    /// <summary>
    /// Represents information about an available language.
    /// Used for populating language selection dropdowns.
    /// </summary>
    public class LanguageInfo
    {
        /// <summary>
        /// The language code (e.g., "en", "de", "fr").
        /// This is derived from the filename (e.g., "en.json" -> "en").
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// The display name of the language (e.g., "English", "Deutsch").
        /// This is read from the "_meta_.language_name" field in the JSON file.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Creates a new LanguageInfo instance.
        /// </summary>
        public LanguageInfo()
        {
        }

        /// <summary>
        /// Creates a new LanguageInfo instance with the specified code and name.
        /// </summary>
        /// <param name="code">The language code.</param>
        /// <param name="name">The display name.</param>
        public LanguageInfo(string code, string name)
        {
            Code = code;
            Name = name;
        }

        /// <summary>
        /// Returns the display name for string representation.
        /// </summary>
        public override string ToString()
        {
            return Name ?? Code;
        }
    }
}
