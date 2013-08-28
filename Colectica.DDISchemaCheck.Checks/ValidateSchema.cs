using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Schema;

namespace Colectica.DDISchemaCheck.Checks
{
    public class ValidateSchema
    {

        private List<ValidationEventArgs> errorsField = new List<ValidationEventArgs>();
        private List<ValidationEventArgs> warningsField = new List<ValidationEventArgs>();

        public XmlSchemaSet GetTestResult(string filename, out string content)
        {
            StringBuilder b = new StringBuilder();
            // validate schema
            XmlSchemaSet xmlSchemaSet = GetSchema(filename);

            errorsField.AddRange(warningsField);
            errorsField = errorsField.Where(x => !x.Exception.SourceUri.Contains("xhtml")).ToList();
            if (errorsField.Count > 0)
            {
                b.Append(@"<div class=""alert alert-danger"">The DDI Schema Set has failed validation.</div>");
                b.AppendLine(@"<table class=""table"">
                                <thead>
                                  <tr>
                                    <th>#</th>
                                    <th>Item</th>
                                    <th>Location</th>
                                    <th>Message</th>
                                  </tr>
                                </thead>
                                <tbody>");
                int i = 1;
                string tableRow = @"<tr class=""{0}"">
                                    <td>{1}</td>
                                    <td>{2}</td>
                                    <td>{3} {4}:{5}</td>
                                    <td>{6}</td>
                                  </tr>";
                
                foreach (var error in errorsField)
                {                    
                    string contextual = error.Severity == XmlSeverityType.Error ? "danger" : "warning";
                    string name = string.Empty;
                    if (error.Exception.SourceSchemaObject != null)
                    {
                        XmlSchemaType st = error.Exception.SourceSchemaObject as XmlSchemaType;
                        if (st != null)
                        {
                            if (!string.IsNullOrWhiteSpace(st.Name)) { name = st.Name; }
                            else if (st.QualifiedName != null) { name = st.QualifiedName.Name; }
                        }
                    }
                    b.Append(string.Format(tableRow,
                            contextual,
                            i++,
                            name,
                            HttpUtility.HtmlEncode(error.Exception.SourceUri.Split('/').Last()),
                            error.Exception.LineNumber,
                            error.Exception.LinePosition,
                            HttpUtility.HtmlEncode(error.Message)));
                }
                b.AppendLine("</table>");
            }
            else
            {
                b.Append("<div class=\"alert alert-success\">DDI Schema Set passed validation</div>");
            }

            content = b.ToString();
            if (errorsField.Count > 0) { return null; }
            return xmlSchemaSet;
        }

        void ValidationCallback(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Warning)
            {
                warningsField.Add(args);
            }
            else if (args.Severity == XmlSeverityType.Error)
            {
                errorsField.Add(args);
            }
        }
        public XmlSchemaSet GetSchema(string filename)
        {
            XmlSchemaSet xmlSchemaSet = new XmlSchemaSet();
            xmlSchemaSet.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            using (XmlTextReader reader = new XmlTextReader(filename))
            {
                XmlSchema xmlSchema = XmlSchema.Read(reader, new ValidationEventHandler(ValidationCallback));
                xmlSchemaSet.Add(xmlSchema);
            }

            xmlSchemaSet.Compile();
            return xmlSchemaSet;
        }
    }
}
