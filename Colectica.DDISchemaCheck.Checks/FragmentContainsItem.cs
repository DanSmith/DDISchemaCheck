using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace Colectica.DDISchemaCheck.Checks
{
    public class FragmentContainsItem
    {
        public static string GetTestResult(XmlSchemaSet schemas)
        {
            StringBuilder b = new StringBuilder();


            List<XmlSchemaElement> versionables = new List<XmlSchemaElement>();
            List<XmlSchemaElement> maintainables = new List<XmlSchemaElement>();

            Dictionary<XmlQualifiedName, List<XmlSchemaElement>> substitutes = schemas.GetSubstitutionGroups();

            foreach (XmlSchemaElement element in schemas.GlobalElements.Values)
            {
                if (element.IsAbstract)
                {
                    continue;
                }

                XmlSchemaComplexType ct = element.ElementSchemaType as XmlSchemaComplexType;
                if (ct == null) { continue; }

                if (ct.AttributeUses.Values.Cast<XmlSchemaAttribute>().Where(a => a.Name == "isVersionable").Count() > 0)
                {
                    versionables.Add(element);
                }
                else if (ct.AttributeUses.Values.Cast<XmlSchemaAttribute>().Where(a => a.Name == "isMaintainable").Count() > 0)
                {
                    maintainables.Add(element);
                }
                else
                {
                    continue;
                }
            }



            XmlSchemaElement fragment = schemas.GlobalElements.Values.Cast<XmlSchemaElement>().Where(x => x.Name == "Fragment").FirstOrDefault();

            XmlSchemaComplexType fragmentType = fragment.ElementSchemaType as XmlSchemaComplexType;
            XmlSchemaParticle particle = fragmentType.Particle;
            XmlSchemaSequence sequence = particle as XmlSchemaSequence;
            XmlSchemaChoice choice = sequence.Items[0] as XmlSchemaChoice;
            List<XmlSchemaElement> references = new List<XmlSchemaElement>();
            foreach (XmlSchemaElement childElement in choice.Items)
            {
                XmlSchemaComplexType ct = childElement.ElementSchemaType as XmlSchemaComplexType;
                if (ct != null)
                {
                    if (ct.IsAbstract)
                    {
                        if (substitutes.ContainsKey(childElement.QualifiedName))
                        {
                            references.AddRange(substitutes[childElement.QualifiedName]);
                        }
                    }
                }


                references.Add(childElement);
            }

            int refCount = references.Count;
            int actual = versionables.Count + maintainables.Count;

            List<XmlSchemaElement> missing = new List<XmlSchemaElement>(versionables);
            missing.AddRange(maintainables);

            missing = missing.OrderBy(x => x.QualifiedName.ToString()).ToList();
            references = references.OrderBy(x => x.QualifiedName.ToString()).ToList();

            for (int i = 0; i < missing.Count; ++i)
            {
                XmlSchemaElement element = missing[i];
                foreach (XmlSchemaElement reference in references)
                {
                    if (reference.QualifiedName.ToString() == element.QualifiedName.ToString())
                    {
                        missing.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }




            string table = @"<table class=""table"">
                                <thead>
                                  <tr>
                                    <th>#</th>
                                    <th>Item</th>
                                    <th>Location</th>
                                  </tr>
                                </thead>
                                <tbody>";
            string tableRow = @"<tr>
                                    <td>{0}</td>
                                    <td>{1}</td>
                                    <td>{2} {3}:{4}</td>
                                  </tr>";

            if (missing.Count > 0)
            {
                b.Append(@"<div class=""alert alert-danger"">Versionables and Maintainables found without FragmentInstance inclusion.</div>");
                b.AppendLine(table);
                int j = 1;
                foreach (var error in missing)
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
                b.Append("<div class=\"alert alert-success\">No duplicate Element names detected for referenceable types.</div>");
            }
            
            return b.ToString();
        }
    }
}
