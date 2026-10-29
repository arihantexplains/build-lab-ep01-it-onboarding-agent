using System;
using System.Globalization;
using System.Text;

namespace OnboardingAgent
{
    /// <summary>The parser behind Json.Parse. Reads one JSON document and reports errors with the character position.</summary>
    internal sealed class JsonReader
    {
        private const int MaxDepth = 64;

        private readonly string _text;
        private int _position;

        /// <summary>Creates a reader over the given JSON text.</summary>
        /// <param name="text">The JSON text to read.</param>
        public JsonReader(string text)
        {
            _text = text;
        }

        /// <summary>Reads the single JSON value in the text and checks that nothing but whitespace follows it.</summary>
        /// <returns>A JsonObject, JsonArray, string, long, double, bool or null.</returns>
        public object ReadDocument()
        {
            var value = ReadValue(0);
            SkipWhitespace();
            if (_position != _text.Length)
            {
                throw Error("Unexpected text after the JSON value");
            }

            return value;
        }

        private object ReadValue(int depth)
        {
            if (depth > MaxDepth)
            {
                throw Error("JSON is nested too deeply");
            }

            SkipWhitespace();
            var c = Peek();
            switch (c)
            {
                case '{': return ReadObject(depth);
                case '[': return ReadArray(depth);
                case '"': return ReadString();
                case 't': ReadLiteral("true"); return true;
                case 'f': ReadLiteral("false"); return false;
                case 'n': ReadLiteral("null"); return null;
                case '\0': throw Error("Unexpected end of JSON");
                default:
                    if (c == '-' || (c >= '0' && c <= '9'))
                    {
                        return ReadNumber();
                    }

                    throw Error("Unexpected character '" + c + "'");
            }
        }

        private JsonObject ReadObject(int depth)
        {
            var result = new JsonObject();
            _position++;
            SkipWhitespace();
            if (Peek() == '}')
            {
                _position++;
                return result;
            }

            while (true)
            {
                SkipWhitespace();
                if (Peek() != '"')
                {
                    throw Error("Expected a property name");
                }

                var key = ReadString();
                SkipWhitespace();
                Expect(':');
                result[key] = ReadValue(depth + 1);
                SkipWhitespace();
                var c = Next();
                if (c == '}')
                {
                    return result;
                }

                if (c != ',')
                {
                    throw Error("Expected ',' or '}'");
                }
            }
        }

        private JsonArray ReadArray(int depth)
        {
            var result = new JsonArray();
            _position++;
            SkipWhitespace();
            if (Peek() == ']')
            {
                _position++;
                return result;
            }

            while (true)
            {
                result.Add(ReadValue(depth + 1));
                SkipWhitespace();
                var c = Next();
                if (c == ']')
                {
                    return result;
                }

                if (c != ',')
                {
                    throw Error("Expected ',' or ']'");
                }
            }
        }

        private string ReadString()
        {
            _position++;
            var builder = new StringBuilder();
            while (true)
            {
                if (_position >= _text.Length)
                {
                    throw Error("Unterminated string");
                }

                var c = _text[_position++];
                if (c == '"')
                {
                    return builder.ToString();
                }

                if (c < ' ')
                {
                    throw Error("Control character in string");
                }

                if (c != '\\')
                {
                    builder.Append(c);
                    continue;
                }

                var escape = Next();
                switch (escape)
                {
                    case '"': builder.Append('"'); break;
                    case '\\': builder.Append('\\'); break;
                    case '/': builder.Append('/'); break;
                    case 'b': builder.Append('\b'); break;
                    case 'f': builder.Append('\f'); break;
                    case 'n': builder.Append('\n'); break;
                    case 'r': builder.Append('\r'); break;
                    case 't': builder.Append('\t'); break;
                    case 'u':
                        if (_position + 4 > _text.Length)
                        {
                            throw Error("Incomplete unicode escape");
                        }

                        int code;
                        if (!int.TryParse(_text.Substring(_position, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out code))
                        {
                            throw Error("Invalid unicode escape");
                        }

                        builder.Append((char)code);
                        _position += 4;
                        break;
                    default:
                        throw Error("Invalid escape sequence");
                }
            }
        }

        private object ReadNumber()
        {
            var start = _position;
            while (_position < _text.Length && "+-0123456789.eE".IndexOf(_text[_position]) >= 0)
            {
                _position++;
            }

            var token = _text.Substring(start, _position - start);
            long whole;
            if (token.IndexOfAny(new[] { '.', 'e', 'E' }) < 0
                && long.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out whole))
            {
                return whole;
            }

            double real;
            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out real))
            {
                return real;
            }

            throw Error("Invalid number");
        }

        private void ReadLiteral(string literal)
        {
            if (string.CompareOrdinal(_text, _position, literal, 0, literal.Length) != 0)
            {
                throw Error("Invalid literal");
            }

            _position += literal.Length;
        }

        private void SkipWhitespace()
        {
            while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
            {
                _position++;
            }
        }

        private char Peek()
        {
            return _position < _text.Length ? _text[_position] : '\0';
        }

        private char Next()
        {
            if (_position >= _text.Length)
            {
                throw Error("Unexpected end of JSON");
            }

            return _text[_position++];
        }

        private void Expect(char expected)
        {
            if (Next() != expected)
            {
                throw Error("Expected '" + expected + "'");
            }
        }

        private FormatException Error(string message)
        {
            return new FormatException(message + " at position " + _position + ".");
        }
    }
}
