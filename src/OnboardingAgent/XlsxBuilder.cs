using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace OnboardingAgent
{
    /// <summary>
    /// Writes a single-sheet .xlsx with a bold, frozen header row. Built by hand because Open XML SDK is not available in the sandbox.
    /// </summary>
    public static class XlsxBuilder
    {
        /// <summary>MIME type for .xlsx files, used when uploading the workbook.</summary>
        public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private const int MaxCellLength = 32767;

        private const string ContentTypesXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
            "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
            "</Types>";

        private const string RootRelsXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
            "</Relationships>";

        private const string WorkbookRelsXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
            "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
            "</Relationships>";

        private const string StylesXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
            "<fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills>" +
            "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
            "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
            "<cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
            "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/></cellXfs>" +
            "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>" +
            "</styleSheet>";

        /// <summary>Builds the workbook. Every cell is written as text, empty values are left blank and long values are cut to the Excel cell limit.</summary>
        /// <param name="sheetName">Name of the single worksheet.</param>
        /// <param name="headers">Header row, shown in bold and frozen.</param>
        /// <param name="rows">Data rows, one string per column.</param>
        /// <returns>The .xlsx file as bytes.</returns>
        public static byte[] Build(string sheetName, IList<string> headers, IEnumerable<IList<string>> rows)
        {
            using (var buffer = new MemoryStream())
            {
                using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, true))
                {
                    AddEntry(zip, "[Content_Types].xml", ContentTypesXml);
                    AddEntry(zip, "_rels/.rels", RootRelsXml);
                    AddEntry(zip, "xl/workbook.xml", WorkbookXml(sheetName));
                    AddEntry(zip, "xl/_rels/workbook.xml.rels", WorkbookRelsXml);
                    AddEntry(zip, "xl/styles.xml", StylesXml);
                    AddEntry(zip, "xl/worksheets/sheet1.xml", SheetXml(headers, rows));
                }

                return buffer.ToArray();
            }
        }

        private static void AddEntry(ZipArchive zip, string name, string content)
        {
            var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
            using (var stream = entry.Open())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(content);
            }
        }

        private static string WorkbookXml(string sheetName)
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
                "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                "<sheets><sheet name=\"" + Escape(sheetName) + "\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";
        }

        private static string SheetXml(IList<string> headers, IEnumerable<IList<string>> rows)
        {
            var xml = new StringBuilder();
            xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            xml.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            xml.Append("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
            xml.Append("<sheetData>");
            AppendRow(xml, 1, headers, 1);
            var rowNumber = 2;
            foreach (var row in rows)
            {
                AppendRow(xml, rowNumber++, row, 0);
            }

            xml.Append("</sheetData></worksheet>");
            return xml.ToString();
        }

        private static void AppendRow(StringBuilder xml, int rowNumber, IList<string> values, int style)
        {
            xml.Append("<row r=\"").Append(rowNumber).Append("\">");
            for (var column = 0; column < values.Count; column++)
            {
                var value = values[column];
                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }

                if (value.Length > MaxCellLength)
                {
                    value = value.Substring(0, MaxCellLength);
                }

                xml.Append("<c r=\"").Append(ColumnName(column)).Append(rowNumber).Append("\" t=\"inlineStr\"");
                if (style != 0)
                {
                    xml.Append(" s=\"").Append(style).Append('"');
                }

                xml.Append("><is><t xml:space=\"preserve\">").Append(Escape(value)).Append("</t></is></c>");
            }

            xml.Append("</row>");
        }

        private static string ColumnName(int index)
        {
            var name = string.Empty;
            for (var n = index + 1; n > 0; n = (n - 1) / 26)
            {
                name = (char)('A' + (n - 1) % 26) + name;
            }

            return name;
        }

        /// <summary>XML-escapes text and drops characters XML cannot hold, which would otherwise corrupt the workbook.</summary>
        private static string Escape(string text)
        {
            var result = new StringBuilder(text.Length);
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (char.IsHighSurrogate(c) && i + 1 < text.Length && XmlConvert.IsXmlSurrogatePair(text[i + 1], c))
                {
                    result.Append(c).Append(text[++i]);
                    continue;
                }

                if (!XmlConvert.IsXmlChar(c))
                {
                    continue;
                }

                switch (c)
                {
                    case '&': result.Append("&amp;"); break;
                    case '<': result.Append("&lt;"); break;
                    case '>': result.Append("&gt;"); break;
                    case '"': result.Append("&quot;"); break;
                    default: result.Append(c); break;
                }
            }

            return result.ToString();
        }
    }
}
