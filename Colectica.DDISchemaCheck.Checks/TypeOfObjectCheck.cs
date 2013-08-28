using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Schema;

namespace Colectica.DDISchemaCheck.Checks
{
    public class TypeOfObjectCheck
    {
        public static string GetTestResult(XmlSchemaSet schemas)
        {
            List<XmlSchemaElement> referenceable = new List<XmlSchemaElement>();
            List<XmlSchemaEnumerationFacet> defined = new List<XmlSchemaEnumerationFacet>();

            foreach (XmlSchemaElement element in schemas.GlobalElements.Values)
            {
                if (element.IsAbstract)
                {
                    continue;
                }

                if (element.QualifiedName.Name == "TypeOfObject")
                {
                    XmlSchemaSimpleType st = element.ElementSchemaType as XmlSchemaSimpleType;
                    if (st != null && st.Content is XmlSchemaSimpleTypeRestriction)
                    {
                        var restriction = st.Content as XmlSchemaSimpleTypeRestriction;
                        foreach (var enumeration in restriction.Facets.OfType<XmlSchemaEnumerationFacet>())
                        {
                            defined.Add(enumeration);
                        }
                    }
                }

                XmlSchemaComplexType ct = element.ElementSchemaType as XmlSchemaComplexType;
                if (ct == null) { continue; }

                if (ct.AttributeUses.Values.Cast<XmlSchemaAttribute>().Any(a =>
                    a.Name == "isVersionable" ||
                    a.Name == "isMaintainable" ||
                    a.Name == "isIdentifiable"))
                {
                    referenceable.Add(element);
                }
            }

            Dictionary<string, List<XmlSchemaElement>> foundNames = new Dictionary<string, List<XmlSchemaElement>>();
            foreach (var r in referenceable)
            {
                if (foundNames.ContainsKey(r.QualifiedName.Name))
                {
                    foundNames[r.QualifiedName.Name].Add(r);
                }
                else
                {
                    foundNames[r.QualifiedName.Name] = new List<XmlSchemaElement>() { r };
                }
            }

            List<XmlSchemaElement> missing = new List<XmlSchemaElement>(referenceable);

            missing = missing.OrderBy(x => x.QualifiedName.ToString()).ToList();
            defined = defined.OrderBy(x => x.Value).ToList();

            for (int i = 0; i < missing.Count; ++i)
            {
                XmlSchemaElement element = missing[i];
                foreach (var d in defined)
                {
                    if (d.Value == element.QualifiedName.Name)
                    {
                        missing.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }

            StringBuilder b = new StringBuilder();

            string table = @"<table class=""table"">
                                <thead>
                                  <tr>
                                    <th>#</th>
                                    <th>Item</th>
                                    <th>Duplicated</th>
                                    <th>Location</th>
                                  </tr>
                                </thead>
                                <tbody>";
            string tableRow = @"<tr>
                                    <td>{0}</td>
                                    <td>{1}</td>
                                    <td>{2}</td>
                                    <td>{3} {4}:{5}</td>
                                  </tr>";

            if (foundNames.Where(kv => kv.Value.Count > 1).Count() > 0)
            {
                b.Append(@"<div class=""alert alert-danger"">Duplicate Element names detected for referenceable types.</div>");
                b.AppendLine(table);
                int j = 1;
                foreach (var error in foundNames.Where(kv => kv.Value.Count > 1))
                {
                    foreach(var dup in error.Value) 
                    {
                        b.AppendLine(string.Format(tableRow,
                            j++,
                            error.Key,
                            dup.QualifiedName.ToString(),
                            dup.SourceUri.Split('/').Last(),
                            dup.LineNumber, dup.LinePosition));
                    }
                }
                b.AppendLine("</table>");
            }
            else
            {
                b.Append("<div class=\"alert alert-success\">No duplicate Element names detected for referenceable types.</div>");
            }


            table = @"<table class=""table"">
                                <thead>
                                  <tr>
                                    <th>#</th>
                                    <th>Item</th>
                                    <th>Location</th>
                                  </tr>
                                </thead>
                                <tbody>";
            tableRow = @"<tr>
                                    <td>{0}</td>
                                    <td>{1}</td>
                                    <td>{2} {3}:{4}</td>
                                  </tr>";
            if (missing.Count > 0)
            {
                b.Append(@"<div class=""alert alert-danger"">Element names detected without a TypeOfObject defined.</div>");
                b.AppendLine(table);
                int j = 1;
                foreach (var error in missing.OrderBy(x => x.QualifiedName.ToString()))
                {
                    b.AppendLine(string.Format(tableRow,
                        j++,
                        error.QualifiedName.ToString(),
                        error.SourceUri.Split('/').Last(),
                        error.LineNumber, error.LinePosition));
                }
                b.AppendLine("</table>");
            }
            else
            {
                b.Append("<div class=\"alert alert-success\">All element names detected with a TypeOfObject defined.</div>");
            }


            return b.ToString();
        }
    }
}
