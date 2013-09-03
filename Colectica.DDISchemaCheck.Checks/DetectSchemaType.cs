using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;

namespace Colectica.DDISchemaCheck.Checks
{
    public class DetectSchemaType
    {        
        public static bool GetTestResult(string filename, out string content)
        {
            StringBuilder b = new StringBuilder();

            try
            {
                DdiFileFormat format = GetSchemaFileFormat(filename);
                string formatVersion = string.Empty;
                if (format == DdiFileFormat.Ddi31) { formatVersion = "3.1"; }
                else if (format == DdiFileFormat.Ddi32) { formatVersion = "3.2"; }
                b.Append(string.Format("<div class=\"alert alert-success\"></span>Detected DDI Schema for Lifecycle version {0}</div>", formatVersion));
                return true;
            }
            catch (Exception error)
            {
                b.Append("<div class=\"alert alert-danger\">The DDI Schema has parsing errors</div>");
                b.Append(string.Format("<ul><li>{0}</li></ul>", HttpUtility.HtmlEncode(error.Message)));
                return false;
            }
            finally
            {
                content = b.ToString();
            }
        }
        
        public static DdiFileFormat GetSchemaFileFormat(string filename)
        {
            try
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.DtdProcessing = DtdProcessing.Parse;

                using (XmlReader xmlReader = XmlReader.Create(filename, settings))
                {
                    if (xmlReader.MoveToContent() == XmlNodeType.Element)
                    {
                        string ns = xmlReader.NamespaceURI;
                        string name = xmlReader.LocalName;

                        if (string.Compare(ns, "http://www.w3.org/2001/XMLSchema", true) != 0)
                        {
                            throw new InvalidOperationException("The .xsd file does not use the DDI schema.");
                        }

                        string attribute = xmlReader.GetAttribute("xmlns");
                        if (string.Compare(attribute, "ddi:instance:3_2", true) == 0)
                        {
                            return DdiFileFormat.Ddi32;
                        }
                        else if (string.Compare(attribute, "ddi:instance:3_1", true) == 0)
                        {
                            return DdiFileFormat.Ddi31;
                        }
                        throw new InvalidOperationException("The schema file is using an unknown namespace.");
                    }
                    throw new InvalidOperationException("The .xsd file was empty.");
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Cannot open the .xsd file. " + e.Message);
            }
        }
        
    }

    public enum DdiFileFormat
    {
        Unknown = 0,
        Ddi25 = 1,
        Ddi31 = 2,
        Ddi32 = 4,
    }

}
