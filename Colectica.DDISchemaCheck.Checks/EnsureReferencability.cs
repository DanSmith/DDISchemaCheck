using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Schema;

namespace Colectica.DDISchemaCheck.Checks
{
    public class EnsureReferencability
    {
        public static string GetTestResult(XmlSchemaSet schema)
        {
            XmlSchemaElement instance = schema.GlobalElements.Values
                .OfType<XmlSchemaElement>()
                .Where(x => x.QualifiedName.Name == "DDIInstance")
                .FirstOrDefault();

            Dictionary<XmlQualifiedName, List<XmlSchemaElement>> substitutes = schema.GetSubstitutionGroups();
            HashSet<Tuple<XmlQualifiedName, XmlSchemaElement>> elementsFound = new HashSet<Tuple<XmlQualifiedName, XmlSchemaElement>>();
            instance.WalkXsdReferences( elementsFound, instance.QualifiedName);


            List<Tuple<XmlQualifiedName, XmlSchemaElement>> noChoice = new List<Tuple<XmlQualifiedName, XmlSchemaElement>>();
            List<Tuple<XmlQualifiedName, XmlSchemaElement>> wrongFormatChoice = new List<Tuple<XmlQualifiedName, XmlSchemaElement>>();
            List<Tuple<XmlQualifiedName, XmlSchemaElement>> wrongNumberChoice = new List<Tuple<XmlQualifiedName, XmlSchemaElement>>();
            foreach (var tuple in elementsFound)
            {
                XmlSchemaElement element = tuple.Item2;

                XmlSchemaComplexType ct = element.ElementSchemaType as XmlSchemaComplexType;
                if (ct == null) { continue; }

                if (ct.AttributeUses.Values.Cast<XmlSchemaAttribute>().Any(a =>
                    a.Name == "isVersionable" ||
                    a.Name == "isMaintainable"))
                {
                    if (element.Parent != null)
                    {
                        if (element == instance) { continue; }

                        XmlSchemaChoice choice = element.Parent as XmlSchemaChoice;
                        if (choice == null)
                        {
                            noChoice.Add(tuple);
                        }
                        else
                        {
                            List<XmlSchemaElement> siblings = choice.Items.OfType<XmlSchemaElement>().ToList();
                            if (siblings.Count != 2)
                            {
                                wrongNumberChoice.Add(tuple);
                            }
                            else
                            {
                                bool hasSiblingRef = siblings.Any(x => x.QualifiedName.Name == element.QualifiedName.Name + "Reference");

                                if (!hasSiblingRef)
                                {
                                    if (substitutes.ContainsKey(element.QualifiedName))
                                    {
                                        var anyRefs = substitutes[element.QualifiedName].Select(x => x.QualifiedName.Name + "Reference")
                                            .Intersect(siblings.Select(x => x.QualifiedName.Name)).ToList();
                                        if (anyRefs.Count != 0)
                                        {
                                            continue;
                                        }
                                    }
                                    wrongFormatChoice.Add(tuple);
                                }
                            }
                        }

                    }
                }
            }

            StringBuilder b = new StringBuilder();
            string tableRow = @"<tr>
                                    <td>{0}</td>
                                    <td>{1}</td>
                                    <td>{2}</td>
                                  </tr>";

            if (noChoice.Count > 0)
            {
                b.Append(@"<div class=""alert alert-danger"">Versionables and Maintainables not in a xs:Choice.</div>");
                b.AppendLine(@"<table class=""table"">
                                <thead>
                                  <tr>
                                    <th>#</th>
                                    <th>Item</th>
                                    <th>No xs:choice in Parent</th>
                                  </tr>
                                </thead>
                                <tbody>");
                int i = 1;

                foreach (var n in noChoice.OrderBy(x => x.Item1.ToString()))
                {
                    b.Append(string.Format(tableRow,
                            i++,
                            n.Item2.QualifiedName.ToString(),
                            n.Item1.ToString()));
                }
                b.AppendLine("</table>");
            }
            else
            {
                b.Append("<div class=\"alert alert-success\">All Versionables and Maintainables are in a xs:Choice.</div>");
            }

            if (wrongNumberChoice.Count > 0)
            {
                b.Append(@"<div class=""alert alert-danger"">Wrong number of elements in Versionables and Maintainables xs:Choice.</div>");
                b.AppendLine(@"<table class=""table"">
                                <thead>
                                  <tr>
                                    <th>#</th>
                                    <th>Item</th>
                                    <th>Incorrect number of elements in xs:choice</th>
                                  </tr>
                                </thead>
                                <tbody>");
                int i = 1;

                foreach (var n in wrongNumberChoice.OrderBy(x => x.Item1.ToString()))
                {
                    b.Append(string.Format(tableRow,
                            i++,
                            n.Item2.QualifiedName.ToString(),
                            n.Item1.ToString()));
                }
                b.AppendLine("</table>");
            }
            else
            {
                b.Append("<div class=\"alert alert-success\">Correct number of elements in all Versionables and Maintainables xs:Choice.</div>");
            }

            if (wrongFormatChoice.Count > 0)
            {
                b.Append(@"<div class=""alert alert-danger"">Missing xxxReference to Versionables and Maintainables in xs:Choice.</div>");
                b.AppendLine(@"<table class=""table"">
                                <thead>
                                  <tr>
                                    <th>#</th>
                                    <th>Item</th>
                                    <th>No xxxReference element in xs:choice</th>
                                  </tr>
                                </thead>
                                <tbody>");
                int i = 1;

                foreach (var n in wrongFormatChoice.OrderBy(x => x.Item1.ToString()))
                {
                    b.Append(string.Format(tableRow,
                            i++,
                            n.Item2.QualifiedName.ToString(),
                            n.Item1.ToString()));
                }
                b.AppendLine("</table>");
            }
            else
            {
                b.Append("<div class=\"alert alert-success\">Correct number of elements in all Versionables and Maintainables xs:Choice.</div>");
            }

            return b.ToString();
        }
    }
}
