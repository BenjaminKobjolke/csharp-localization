using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CSharpLocalization
{
    /// <summary>
    /// Localization provider that loads translations from embedded resources in an assembly.
    /// Resource names should follow the pattern: {Namespace}.lang.{langCode}.json
    /// </summary>
    public class EmbeddedResourceLocalizationProvider : ILocalizationProvider
    {
        private readonly Assembly _assembly;
        private readonly string _resourcePrefix;

        /// <summary>
        /// Creates a new embedded resource localization provider.
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded resources.</param>
        /// <param name="resourcePrefix">The prefix for resource names (e.g., "MyApp.lang.").</param>
        public EmbeddedResourceLocalizationProvider(Assembly assembly, string resourcePrefix)
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            _resourcePrefix = resourcePrefix ?? string.Empty;
        }

        /// <summary>
        /// Gets the file extension for JSON files.
        /// </summary>
        public string FileExtension => ".json";

        /// <summary>
        /// Loads all translations from an embedded resource.
        /// </summary>
        /// <param name="resourceName">The resource name (language code, e.g., "en" or "de").</param>
        /// <returns>A dictionary containing the translations.</returns>
        public Dictionary<string, object> LoadAll(string resourceName)
        {
            string fullResourceName = GetFullResourceName(resourceName);

            using (var stream = _assembly.GetManifestResourceStream(fullResourceName))
            {
                if (stream == null)
                {
                    return new Dictionary<string, object>();
                }

                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string json = reader.ReadToEnd();
                    var jObject = JObject.Parse(json);
                    return ConvertJObjectToDictionary(jObject);
                }
            }
        }

        /// <summary>
        /// Checks if an embedded resource exists.
        /// </summary>
        public bool FileExists(string resourceName)
        {
            string fullResourceName = GetFullResourceName(resourceName);
            return _assembly.GetManifestResourceNames().Contains(fullResourceName);
        }

        /// <summary>
        /// Gets the language name from the _meta_.language_name field.
        /// </summary>
        public string GetLanguageName(string resourceName)
        {
            string fullResourceName = GetFullResourceName(resourceName);

            using (var stream = _assembly.GetManifestResourceStream(fullResourceName))
            {
                if (stream == null)
                {
                    return null;
                }

                try
                {
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        string json = reader.ReadToEnd();
                        var jObject = JObject.Parse(json);

                        var meta = jObject["_meta_"] as JObject;
                        if (meta != null)
                        {
                            var languageName = meta["language_name"];
                            if (languageName != null)
                            {
                                return languageName.ToString();
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore parsing errors
                }

                return null;
            }
        }

        /// <summary>
        /// Gets all available language codes from embedded resources.
        /// </summary>
        public List<string> GetAvailableLanguageCodes()
        {
            var resourceNames = _assembly.GetManifestResourceNames();
            var langCodes = new List<string>();

            foreach (var name in resourceNames)
            {
                if (name.StartsWith(_resourcePrefix) && name.EndsWith(FileExtension))
                {
                    // Extract language code from resource name
                    // e.g., "MyApp.lang.en.json" -> "en"
                    string withoutPrefix = name.Substring(_resourcePrefix.Length);
                    string langCode = withoutPrefix.Substring(0, withoutPrefix.Length - FileExtension.Length);

                    if (!langCode.StartsWith("_")) // Skip metadata files
                    {
                        langCodes.Add(langCode);
                    }
                }
            }

            return langCodes;
        }

        private string GetFullResourceName(string resourceName)
        {
            // If it already looks like a full resource name, use it as-is
            if (resourceName.Contains(".") && resourceName.EndsWith(FileExtension))
            {
                return resourceName;
            }

            // Otherwise, construct the full resource name
            return _resourcePrefix + resourceName + FileExtension;
        }

        private Dictionary<string, object> ConvertJObjectToDictionary(JObject jObject)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in jObject.Properties())
            {
                result[property.Name] = ConvertJToken(property.Value);
            }

            return result;
        }

        private object ConvertJToken(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    return ConvertJObjectToDictionary((JObject)token);

                case JTokenType.Array:
                    var list = new List<object>();
                    foreach (var item in (JArray)token)
                    {
                        list.Add(ConvertJToken(item));
                    }
                    return list;

                case JTokenType.Integer:
                    return token.Value<long>();

                case JTokenType.Float:
                    return token.Value<double>();

                case JTokenType.Boolean:
                    return token.Value<bool>();

                case JTokenType.Null:
                    return null;

                case JTokenType.String:
                default:
                    return token.ToString();
            }
        }
    }
}
