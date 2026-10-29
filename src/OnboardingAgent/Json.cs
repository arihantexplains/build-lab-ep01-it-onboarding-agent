using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OnboardingAgent
{
    /// <summary>
    /// Minimal JSON reader and writer. The sandbox has no Newtonsoft.Json, and DataContractJsonSerializer
    /// cannot read the arbitrary shapes Graph returns. Objects become <see cref="JsonObject"/>, arrays
    /// <see cref="JsonArray"/>, numbers long or double.
    /// </summary>
    public static class Json
    {
        private const int MaxDepth = 64;

        public static object Parse(string text)
        {
            if (text == null)
            {
                throw new FormatException("No JSON text was supplied.");
            }

            return new JsonReader(text).ReadDocument();
        }

        public static JsonObject ParseObject(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new JsonObject();
            }

            var result = Parse(text) as JsonObject;
            if (result == null)
            {
                throw new FormatException("Expected a JSON object.");
            }

            return result;
        }

        public static string Serialize(object value)
        {
            var builder = new StringBuilder();
            Write(builder, value, 0);
            return builder.ToString();
        }

        private static void Write(StringBuilder builder, object value, int depth)
        {
            if (depth > MaxDepth)
            {
                throw new InvalidOperationException("The value is nested too deeply to serialise.");
            }

            if (value == null)
            {
                builder.Append("null");
                return;
            }

            var text = value as string;
            if (text != null)
            {
                WriteString(builder, text);
                return;
            }

            if (value is bool)
            {
                builder.Append((bool)value ? "true" : "false");
                return;
            }

            if (value is int || value is long || value is short || value is byte || value is uint || value is ulong)
            {
                builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                return;
            }

            if (value is double || value is float || value is decimal)
            {
                var number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                builder.Append(double.IsNaN(number) || double.IsInfinity(number)
                    ? "null"
                    : number.ToString("R", CultureInfo.InvariantCulture));
                return;
            }

            if (value is DateTime)
            {
                WriteString(builder, ((DateTime)value).ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
                return;
            }

            if (value is DateTimeOffset)
            {
                WriteString(builder, ((DateTimeOffset)value).UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
                return;
            }

            if (value is Guid)
            {
                WriteString(builder, ((Guid)value).ToString("D"));
                return;
            }

            var dictionary = value as IDictionary<string, object>;
            if (dictionary != null)
            {
                builder.Append('{');
                var first = true;
                foreach (var pair in dictionary)
                {
                    if (!first)
                    {
                        builder.Append(',');
                    }

                    first = false;
                    WriteString(builder, pair.Key);
                    builder.Append(':');
                    Write(builder, pair.Value, depth + 1);
                }

                builder.Append('}');
                return;
            }

            var sequence = value as IEnumerable;
            if (sequence != null)
            {
                builder.Append('[');
                var first = true;
                foreach (var item in sequence)
                {
                    if (!first)
                    {
                        builder.Append(',');
                    }

                    first = false;
                    Write(builder, item, depth + 1);
                }

                builder.Append(']');
                return;
            }

            WriteString(builder, Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        private static void WriteString(StringBuilder builder, string text)
        {
            builder.Append('"');
            foreach (var c in text)
            {
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (c < ' ' || c == '\u2028' || c == '\u2029')
                        {
                            builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(c);
                        }

                        break;
                }
            }

            builder.Append('"');
        }
    }
}
