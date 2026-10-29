using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OnboardingAgent
{
    /// <summary>
    /// A JSON object with typed accessors. Keys are case-insensitive so callers can write "userId" or "UserId".
    /// </summary>
    public sealed class JsonObject : Dictionary<string, object>
    {
        /// <summary>Creates an empty object with case-insensitive keys.</summary>
        public JsonObject() : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        /// <summary>Returns the value as text, or null when it is missing, null, an object or an array. Booleans become "true" or "false".</summary>
        public string GetString(string key)
        {
            object value;
            if (!TryGetValue(key, out value) || value == null || value is JsonObject || value is JsonArray)
            {
                return null;
            }

            if (value is bool)
            {
                return (bool)value ? "true" : "false";
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        /// <summary>Returns the trimmed string, or null when it is missing or blank.</summary>
        public string GetTrimmed(string key)
        {
            var text = GetString(key);
            if (text == null)
            {
                return null;
            }

            text = text.Trim();
            return text.Length == 0 ? null : text;
        }

        /// <summary>Returns the trimmed string, or throws a ValidationException when it is missing or blank.</summary>
        public string RequireString(string key)
        {
            var text = GetTrimmed(key);
            if (text == null)
            {
                throw new ValidationException("Parameter '" + key + "' is required.");
            }

            return text;
        }

        /// <summary>Returns the value as a boolean, also accepting the strings "true" and "false".</summary>
        /// <param name="key">The property name.</param>
        /// <param name="defaultValue">Returned when the value is missing or null.</param>
        /// <exception cref="ValidationException">The value is not a boolean.</exception>
        public bool GetBool(string key, bool defaultValue)
        {
            object value;
            if (!TryGetValue(key, out value) || value == null)
            {
                return defaultValue;
            }

            if (value is bool)
            {
                return (bool)value;
            }

            bool parsed;
            var text = value as string;
            if (text != null && bool.TryParse(text.Trim(), out parsed))
            {
                return parsed;
            }

            throw new ValidationException("Parameter '" + key + "' must be true or false.");
        }

        /// <summary>Returns the value as a whole number, accepting numbers and numeric strings.</summary>
        /// <param name="key">The property name.</param>
        /// <param name="defaultValue">Returned when the value is missing or null.</param>
        /// <exception cref="ValidationException">The value is not a whole number that fits in an int.</exception>
        public int GetInt(string key, int defaultValue)
        {
            return GetInt(key, defaultValue, int.MinValue, int.MaxValue);
        }

        /// <summary>Returns the value as a whole number within a range, accepting numbers and numeric strings.</summary>
        /// <param name="key">The property name.</param>
        /// <param name="defaultValue">Returned when the value is missing or null.</param>
        /// <param name="min">The smallest allowed value.</param>
        /// <param name="max">The largest allowed value.</param>
        /// <exception cref="ValidationException">The value is not a whole number or is outside min to max.</exception>
        public int GetInt(string key, int defaultValue, int min, int max)
        {
            object value;
            if (!TryGetValue(key, out value) || value == null)
            {
                return defaultValue;
            }

            long number;
            if (value is long)
            {
                number = (long)value;
            }
            else if (value is double && Math.Abs((double)value % 1) < double.Epsilon && Math.Abs((double)value) < long.MaxValue)
            {
                number = (long)(double)value;
            }
            else if (value is string && long.TryParse(((string)value).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
            {
            }
            else
            {
                throw new ValidationException("Parameter '" + key + "' must be a whole number.");
            }

            if (number < min || number > max)
            {
                throw new ValidationException("Parameter '" + key + "' must be between " + min + " and " + max + ".");
            }

            return (int)number;
        }

        /// <summary>Returns the nested object, or null when it is missing or not an object.</summary>
        public JsonObject GetObject(string key)
        {
            object value;
            return TryGetValue(key, out value) ? value as JsonObject : null;
        }

        /// <summary>Returns the array, or an empty array when it is missing.</summary>
        public JsonArray GetArray(string key)
        {
            object value;
            return (TryGetValue(key, out value) ? value as JsonArray : null) ?? new JsonArray();
        }

        /// <summary>Accepts a JSON array of strings or a comma/semicolon separated string. Blank and duplicate entries are dropped.</summary>
        public List<string> GetStringList(string key)
        {
            var result = new List<string>();
            object value;
            if (!TryGetValue(key, out value) || value == null)
            {
                return result;
            }

            IEnumerable<object> items;
            var text = value as string;
            if (text != null)
            {
                items = text.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            }
            else if (value is JsonArray)
            {
                items = (JsonArray)value;
            }
            else
            {
                throw new ValidationException("Parameter '" + key + "' must be a list of text values.");
            }

            foreach (var item in items)
            {
                var entry = item as string;
                if (item != null && entry == null)
                {
                    throw new ValidationException("Parameter '" + key + "' must contain only text values.");
                }

                entry = entry?.Trim();
                if (!string.IsNullOrEmpty(entry) && !result.Contains(entry, StringComparer.OrdinalIgnoreCase))
                {
                    result.Add(entry);
                }
            }

            return result;
        }

        /// <summary>Returns the first of several alias keys that is present, so callers may use either spelling.</summary>
        public string ResolveKey(params string[] keys)
        {
            return keys.FirstOrDefault(ContainsKey) ?? keys[0];
        }
    }
}
