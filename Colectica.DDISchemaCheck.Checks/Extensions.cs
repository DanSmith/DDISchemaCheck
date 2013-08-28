using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace Colectica.DDISchemaCheck.Checks
{
    public static class Extensions
    {

        public static Dictionary<XmlQualifiedName, List<XmlSchemaElement>> GetSubstitutionGroups(this XmlSchemaSet schema)
        {
            Dictionary<XmlQualifiedName, List<XmlSchemaElement>> substitutes = new Dictionary<XmlQualifiedName, List<XmlSchemaElement>>();

            foreach (var element in schema.GlobalElements.Values.OfType<XmlSchemaElement>())
            {
                if (!element.SubstitutionGroup.IsEmpty)
                {
                    if (!substitutes.ContainsKey(element.SubstitutionGroup))
                    {
                        substitutes[element.SubstitutionGroup] = new List<XmlSchemaElement>() { element };
                    }
                    else
                    {
                        substitutes[element.SubstitutionGroup].Add(element);
                    }
                }

            }
            return substitutes;

        }

        public static void WalkXsdReferences(this XmlSchemaParticle particle, HashSet<Tuple<XmlQualifiedName, XmlSchemaElement>> elementsFound, XmlQualifiedName parentElement)
        {


            if (particle is XmlSchemaElement)
            {
                XmlSchemaElement element = particle as XmlSchemaElement;

                if (element.QualifiedName.Namespace == "http://www.w3.org/1999/xhtml") { return; }

                Tuple<XmlQualifiedName, XmlSchemaElement> current = new System.Tuple<XmlQualifiedName, XmlSchemaElement>(parentElement, element);

                if (elementsFound.Contains(current)) { return; }

                // processing code
                elementsFound.Add(current);

                XmlSchemaType type = (XmlSchemaType)element.ElementSchemaType;
                if (type is XmlSchemaComplexType)
                {
                    XmlSchemaComplexType ct = type as XmlSchemaComplexType;
                    ct.ContentTypeParticle.WalkXsdReferences(elementsFound, element.QualifiedName);
                }
            }
            else if (particle is XmlSchemaGroupBase)
            //xs:all, xs:choice, xs:sequence
            {
                XmlSchemaGroupBase baseParticle = particle as XmlSchemaGroupBase;
                foreach (XmlSchemaParticle subParticle in baseParticle.Items)
                {
                    subParticle.WalkXsdReferences(elementsFound, parentElement);
                }
            }
        }
    }
}
