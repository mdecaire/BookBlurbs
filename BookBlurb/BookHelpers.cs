using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookBlurb;

class BookHelpers
{
    public static string ConvertToPlainText(string filePath)
    {
        if (Path.GetExtension(filePath).ToLower() == ".txt")
        {
            return File.ReadAllText(filePath);
        }

        var paragraphGroups = new List<string>();

        using (WordprocessingDocument wordDocument = WordprocessingDocument.Open(filePath, false))
        {
            Body body = wordDocument.MainDocumentPart.Document.Body;
            StringBuilder textBuilder = new StringBuilder();

            foreach (var paragraph in body.Elements<Paragraph>())
            {
                string paragraphText = paragraph.InnerText;
                textBuilder.Append(paragraphText).Append(" ");
            }

            return textBuilder.ToString().Trim();
        }
    }
}
