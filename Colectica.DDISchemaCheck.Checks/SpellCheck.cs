using NHunspell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Schema;

namespace Colectica.DDISchemaCheck.Checks
{
    public static class SpellCheck
    {


        public static void AddCustomWords(Hunspell hunspell)
        {
            try
            {
                var lines = File.ReadAllLines("CustomWords.txt");
                foreach (string line in lines)
                {
                    string word = line.Trim();
                    if (!string.IsNullOrWhiteSpace(word))
                    {
                        hunspell.Add(word);
                    } 
                }
            }
            catch (Exception)
            {

            }

        }

        public static string GetTestResult(XmlSchemaSet schemas)
        {
            List<Tuple<List<string>,XmlSchemaElement>> misspelledElements = new List<Tuple<List<string>,XmlSchemaElement>>();
            List<Tuple<List<string>,XmlSchemaAttribute>> misspelledAttributes = new List<Tuple<List<string>,XmlSchemaAttribute>>();
            List<Tuple<List<string>, XmlSchemaDocumentation>> misspelledDocumentation = new List<Tuple<List<string>, XmlSchemaDocumentation>>();

            HashSet<XmlSchemaDocumentation> uniqueDocs = new HashSet<XmlSchemaDocumentation>();

            int xhtmlSkipped = 0;
            int xmlSkipped = 0;

            StringBuilder b = new StringBuilder();

            using (Hunspell hunspell = new Hunspell(@".\Dictionaries\en_US.aff", @".\Dictionaries\en_US.dic"))
            {
                AddCustomWords(hunspell);

                //List<XmlSchemaElement> allElements = new List<XmlSchemaElement>();
                //foreach (XmlSchemaElement element in schemas.GlobalElements.Values)
                //{
                //    WalkXsdParticle(element, allElements);
                //}
                HashSet<Tuple<XmlQualifiedName, XmlSchemaElement>> fullTraversal = new HashSet<System.Tuple<XmlQualifiedName, XmlSchemaElement>>();
                foreach (XmlSchemaElement element in schemas.GlobalElements.Values)
                {
                    element.WalkXsdReferences(fullTraversal, element.QualifiedName);
                }
                List<XmlSchemaElement> allElements = fullTraversal.Select(x => x.Item2).Distinct().ToList();

                foreach (XmlSchemaElement element in allElements)
                {
                    if (element.QualifiedName.Namespace == "http://www.w3.org/1999/xhtml")
                    {
                        xhtmlSkipped++;
                        continue;
                    }

                    List<string> misspelling = new List<string>();
                    if (ElementMisspelled(hunspell, element, misspelling))
                    {
                        misspelledElements.Add(new Tuple<List<string>, XmlSchemaElement>(misspelling, element));
                    }

                    if (element.Annotation != null)
                    {
                        foreach (XmlSchemaDocumentation doc in element.Annotation.Items.OfType<XmlSchemaDocumentation>())
                        {
                            if (uniqueDocs.Contains(doc)) { continue; }
                            uniqueDocs.Add(doc);
                            misspelling = new List<string>();
                            if (DocumentationMisspelled(hunspell, doc, misspelling))
                            {
                                misspelledDocumentation.Add(new Tuple<List<string>, XmlSchemaDocumentation>(misspelling, doc));
                            }
                        }
                    }

                    XmlSchemaComplexType ct = element.ElementSchemaType as XmlSchemaComplexType;
                    if (ct == null) { continue; }

                    foreach (XmlSchemaAttribute attribute in ct.AttributeUses.Values.Cast<XmlSchemaAttribute>())
                    {
                        if (attribute.QualifiedName.Namespace == "http://www.w3.org/XML/1998/namespace")
                        {
                            xmlSkipped++;
                            continue;
                        }

                        misspelling = new List<string>();
                        if (AttributeMisspelled(hunspell, attribute, misspelling))
                        {
                            misspelledAttributes.Add(new Tuple<List<string>, XmlSchemaAttribute>(misspelling, attribute));
                        }

                        if (attribute.Annotation != null)
                        {
                            foreach (XmlSchemaDocumentation doc in attribute.Annotation.Items.OfType<XmlSchemaDocumentation>())
                            {
                                if (uniqueDocs.Contains(doc)) { continue; }
                                uniqueDocs.Add(doc);
                                misspelling = new List<string>();
                                if (DocumentationMisspelled(hunspell, doc, misspelling))
                                {
                                    misspelledDocumentation.Add(new Tuple<List<string>, XmlSchemaDocumentation>(misspelling, doc));
                                }
                            }
                        }
                    }
                }
            }



            if (misspelledElements.Count > 0)
            {
                b.Append(@"<div class=""alert alert-danger"">Misspelled elements have been found.</div>");
                b.AppendLine(@"<table class=""table"">
                                <thead>
                                  <tr>
                                    <th>#</th>
                                    <th>Item</th>
                                    <th>Location</th>
                                  </tr>
                                </thead>
                                <tbody>");
                int i = 1;
                string tableRow = @"<tr>
                                    <td>{0}</td>
                                    <td>{1}</td>
                                    <td>{2} {3}:{4}</td>
                                  </tr>";

                foreach (var error in misspelledElements.Distinct())
                {
                    string display = error.Item2.QualifiedName.ToString();
                    foreach(string s in error.Item1) 
                    {
                        display = display.Replace(s, @"<span class=""text-danger""><strong>" + HttpUtility.HtmlEncode(s) + "</strong></span>");
                    }

                    b.Append(string.Format(tableRow,
                            i++,
                            display,
                            HttpUtility.HtmlEncode(error.Item2.SourceUri.Split('/').Last()),
                            error.Item2.LineNumber,
                            error.Item2.LinePosition));
                }
                b.AppendLine("</table>");
            }
            else
            {
                b.Append("<div class=\"alert alert-success\">All Elements spelled correctly.</div>");
            }


            if (misspelledAttributes.Count > 0)
            {
                b.Append(@"<div class=""alert alert-danger"">Misspelled attributes have been found.</div>");
                b.AppendLine(@"<table class=""table"">
                                <thead>
                                  <tr>
                                    <th>#</th>
                                    <th>Item</th>
                                    <th>Location</th>
                                  </tr>
                                </thead>
                                <tbody>");
                int i = 1;
                string tableRow = @"<tr>
                                    <td>{0}</td>
                                    <td>{1}</td>
                                    <td>{2} {3}:{4}</td>
                                  </tr>";

                foreach (var error in misspelledAttributes.Distinct())
                {
                    string display = error.Item2.QualifiedName.ToString();
                    foreach (string s in error.Item1)
                    {
                        display = display.Replace(s, @"<span class=""text-danger""><strong>" + HttpUtility.HtmlEncode(s) + "</strong></span>");
                    }

                    b.Append(string.Format(tableRow,
                            i++,
                            display,
                            HttpUtility.HtmlEncode(error.Item2.SourceUri.Split('/').Last()),
                            error.Item2.LineNumber,
                            error.Item2.LinePosition));
                }
                b.AppendLine("</table>");
            }
            else
            {
                b.Append("<div class=\"alert alert-success\">All Attributes spelled correctly.</div>");
            }

            if (misspelledDocumentation.Count > 0)
            {
                b.Append(@"<div class=""alert alert-danger"">Misspelled documentation has been found.</div>");
                b.AppendLine(@"<table class=""table"">
                                <thead>
                                  <tr>
                                    <th>#</th>
                                    <th>Item</th>
                                    <th>Location</th>
                                  </tr>
                                </thead>
                                <tbody>");
                int i = 1;
                string tableRow = @"<tr>
                                    <td>{0}</td>
                                    <td>{1}</td>
                                    <td>{2} {3}:{4}</td>
                                  </tr>";

                foreach (var error in misspelledDocumentation.OrderBy(x => x.Item2.SourceUri + x.Item2.LineNumber + x.Item2.LinePosition))
                {
                    string display = error.Item2.Markup.FirstOrDefault().InnerText;
                    foreach (string s in error.Item1)
                    {
                        display = display.Replace(s, @"<span class=""text-danger""><strong>" + HttpUtility.HtmlEncode(s) + "</strong></span>");
                    }

                    b.Append(string.Format(tableRow,
                            i++,
                            display,
                            HttpUtility.HtmlEncode(string.IsNullOrWhiteSpace(error.Item2.SourceUri.Split('/').Last()) ? "instance.xsd" : error.Item2.SourceUri.Split('/').Last()),
                            error.Item2.LineNumber,
                            error.Item2.LinePosition));
                }
                b.AppendLine("</table>");
            }
            else
            {
                b.Append("<div class=\"alert alert-success\">All Attributes spelled correctly.</div>");
            }

            return b.ToString();
        }

        static char[] spaces = new char[] { ' ', '_', '-' };
        static char[] space = new char[] { ' ', '_' };
        static private bool ElementMisspelled(Hunspell hunspell, XmlSchemaElement element, List<string> misspelling)
        {
            string name = element.Name ?? element.QualifiedName.Name;

            string[] split = name.SplitCamelCase().Split(spaces, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in split)
            {
                bool correct = hunspell.Spell(part);
                if (!correct)
                {
                    misspelling.Add(part);
                }
            }
            return misspelling.Count > 0;
        }
        static private bool AttributeMisspelled(Hunspell hunspell, XmlSchemaAttribute attribute, List<string> misspelling)
        {
            if (attribute.Name == null)
            {
                return false; // xml:lang
            }

            string[] split = attribute.Name.SplitCamelCase().Split(spaces, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in split)
            {
                bool correct = hunspell.Spell(part);
                if (!correct)
                {
                    misspelling.Add(part);
                }
            }
            return misspelling.Count > 0; ;
        }
        static private bool DocumentationMisspelled(Hunspell hunspell, XmlSchemaDocumentation doc, List<string> misspelling)
        {
            if (doc.Markup.Count() == 0) { return false; }
            string text = doc.Markup.FirstOrDefault().InnerText;

            // strip punctuation
            string stripped = text.Replace("ddi:", " ");
            stripped = stripped.Replace("xs:", " ");
            stripped = stripped.Replace(@"mm/dd/yyyy", " ");
            stripped = stripped.Replace(@"xml:lang", " ");
            stripped = stripped.Replace(@"xml:", " ");
            // camel cased acronyms
            stripped = stripped.Replace(@"EOLs", " ");
            stripped = stripped.Replace(@"IDs", " ");
            
            stripped = new string(stripped.Select(c =>
                (char.IsLetterOrDigit(c) || c == '-') ? c : ' ').ToArray());


            string[] split = stripped.Split(space, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in split)
            {
                string[] split2 = part.SplitCamelCase().Split(spaces, StringSplitOptions.RemoveEmptyEntries);
                foreach (string part2 in split2)
                {
                    if (part2.Length > 2 && !part2.Any(c => char.IsLower(c)))
                    {
                        continue; //Acronym
                    }

                    bool correct = hunspell.Spell(part2);
                    if (!correct)
                    {
                        misspelling.Add(part2);
                    }
                }
            }
            return misspelling.Count > 0;
        }
    }


    public static class ConventionBasedFormattingExtensions
    {
        /// <summary>
        /// Turn CamelCaseText into Camel case text.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <remarks>Use AppSettings["SplitCamelCase_AllCapsWords"] to specify a comma-delimited list of words that should be ALL CAPS after split</remarks>
        /// <example>
        /// wordWordIDWord1WordWORDWord32Word2
        /// Word Word ID Word 1 Word WORD Word 32 Word 2
        /// 
        /// wordWordIDWord1WordWORDWord32WordID2ID
        /// Word Word ID Word 1 Word WORD Word 32 Word ID 2 ID
        /// 
        /// WordWordIDWord1WordWORDWord32Word2Aa
        /// Word Word ID Word 1 Word WORD Word 32 Word 2 Aa
        /// 
        /// wordWordIDWord1WordWORDWord32Word2A
        /// Word Word ID Word 1 Word WORD Word 32 Word 2 A
        /// </example>
        public static string SplitCamelCase(this string input)
        {
            if (input == null) return null;
            if (string.IsNullOrWhiteSpace(input)) return "";

            var separated = input;

            separated = SplitCamelCaseRegex.Replace(separated, @" $1").Trim();


            return separated;
        }

        private static readonly Regex SplitCamelCaseRegex = new Regex(@"
            (
                (?<=[a-z])[A-Z0-9] (?# lower-to-other boundaries )
                |
                (?<=[0-9])[a-zA-Z] (?# number-to-other boundaries )
                |
                (?<=[A-Z])[0-9] (?# cap-to-number boundaries; handles a specific issue with the next condition )
                |
                (?<=[A-Z])[A-Z](?=[a-z]) (?# handles longer strings of caps like ID or CMS by splitting off the last capital )
            )"
            , RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace
        );

        private static readonly string[] _SplitCamelCase_AllCapsWords = new string[] { };

        private static Dictionary<string, System.Text.RegularExpressions.Regex> _SplitCamelCase_AllCapsWords_Regexes;
        private static Dictionary<string, Regex> SplitCamelCase_AllCapsWords_Regexes
        {
            get
            {
                if (_SplitCamelCase_AllCapsWords_Regexes == null)
                {
                    _SplitCamelCase_AllCapsWords_Regexes = new Dictionary<string, Regex>();
                    foreach (var word in _SplitCamelCase_AllCapsWords)
                        _SplitCamelCase_AllCapsWords_Regexes.Add(word, new Regex(@"\b" + word + @"\b", RegexOptions.Compiled | RegexOptions.IgnoreCase));
                }

                return _SplitCamelCase_AllCapsWords_Regexes;
            }
        }
    }
}
