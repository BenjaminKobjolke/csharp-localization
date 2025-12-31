using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CSharpLocalization
{
    /// <summary>
    /// Localization provider that loads translations from JSON files.
    /// </summary>
    public class JsonLocalizationProvider : ILocalizationProvider
    {
        /// <summary>
        /// Gets the file extension for JSON files.
        /// </summary>
        public string FileExtension => ".json";

        /// <summary>
        /// Loads all translations from a JSON file.
        /// </summary>
        /// <param name="filePath">The full path to the JSON file.</param>
        /// <returns>A dictionary containing the translations.</returns>
        public Dictionary<string, object> LoadAll(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new Dictionary<string, object>();
            }

            try
            {
                string json = File.ReadAllText(filePath, Encoding.UTF8);
                var jObject = JObject.Parse(json);
                return ConvertJObjectToDictionary(jObject);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse JSON file: {filePath}", ex);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException($"Failed to read file: {filePath}", ex);
            }
        }

        /// <summary>
        /// Checks if a JSON file exists at the specified path.
        /// </summary>
        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        /// <summary>
        /// Gets the language name from the _meta_.language_name field.
        /// </summary>
        public string GetLanguageName(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(filePath, Encoding.UTF8);
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

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Converts a JObject to a Dictionary, preserving nested structure.
        /// </summary>
        private Dictionary<string, object> ConvertJObjectToDictionary(JObject jObject)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in jObject.Properties())
            {
                result[property.Name] = ConvertJToken(property.Value);
            }

            return result;
        }

        /// <summary>
        /// Converts a JToken to the appropriate .NET type.
        /// </summary>
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
