using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Schema;

namespace Colectica.DDISchemaCheck.Checks
{
    public class ReportGenerator
    {

        public string CreateReport(string filename)
        {

            string reportFilename = Path.GetTempFileName();
            File.Move(reportFilename, reportFilename + ".html");
            reportFilename = reportFilename + ".html";
            using (StreamWriter reportFile = File.CreateText(reportFilename))
            {

                string title = "DDI Schema Checks " + DateTime.Now.ToString();
                string template = File.ReadAllText("ReportTemplate.html");
                template = template.Replace("{title}", title);


                string content = GetContent(filename);
                template = template.Replace("{content}", content);

                
                reportFile.Write(template);
            }
            return reportFilename;
        }


        private string GetContent(string filename)
        {
            StringBuilder b = new StringBuilder();

            // correct schema file
            string partial = null;
            bool correctFile = DetectSchemaType.GetTestResult(filename, out partial);
            b.AppendLine(partial);
            if (!correctFile) { return b.ToString(); }


            ValidateSchema validate = new ValidateSchema();
            XmlSchemaSet schemaSet = validate.GetTestResult(filename, out partial);
            b.AppendLine(partial);
            if (schemaSet == null) { return b.ToString(); }

            partial = EnsureReferencability.GetTestResult(schemaSet);
            b.AppendLine(partial);

            partial = FragmentContainsItem.GetTestResult(schemaSet);
            b.AppendLine(partial);

            partial = TypeOfObjectCheck.GetTestResult(schemaSet);
            b.AppendLine(partial);

            partial = SpellCheck.GetTestResult(schemaSet);
            b.AppendLine(partial);

            return b.ToString();
        }




        

    }
}
