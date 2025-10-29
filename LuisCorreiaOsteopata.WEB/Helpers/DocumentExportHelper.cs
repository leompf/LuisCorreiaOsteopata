using System.Text.RegularExpressions;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;
using OpenXmlPowerTools;

namespace LuisCorreiaOsteopata.WEB.Helpers;

public class DocumentExportHelper
{
    public byte[] GenerateUserRecord(string templatePath, Dictionary<string, string> data)
    {
        using var memoryStream = new MemoryStream();
        using (var fileStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read))
        {
            fileStream.CopyTo(memoryStream);
        }
        memoryStream.Position = 0;

        using (var wordDoc = WordprocessingDocument.Open(memoryStream, true))
        {
            var xDoc = wordDoc.MainDocumentPart.GetXDocument();

            IEnumerable<XElement> paragraphs = xDoc.Descendants(W.p);

            foreach (var kv in data)
            {
                var regex = new Regex("\\{" + Regex.Escape(kv.Key) + "\\}", RegexOptions.IgnoreCase);
                OpenXmlRegex.Replace(paragraphs, regex, kv.Value ?? "", null);
            }

            wordDoc.MainDocumentPart.PutXDocument();
        }

        return memoryStream.ToArray();
    }
}
