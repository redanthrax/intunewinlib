using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace IntuneWinLib {
    [XmlRoot("ApplicationInfo")]
    [XmlInclude(typeof(MsiApplicationInfo))]
    [XmlInclude(typeof(CustomApplicationInfo))]
    [Serializable]
    public class ApplicationInfo {
        [XmlAttribute]
        public string ToolVersion { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public long UnencryptedContentSize { get; set; }

        public string FileName { get; set; }

        public string SetupFile { get; set; }

        public FileEncryptionInfo EncryptionInfo { get; set; }

        public string ToXml() {
            using (StringWriter output = new StringWriter()) {
                XmlWriterSettings settings = new XmlWriterSettings {
                    OmitXmlDeclaration = true,
                    Indent = true,
                    NewLineHandling = NewLineHandling.Entitize
                };

                using (XmlWriter xmlWriter = XmlWriter.Create(output, settings)) {
                    XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                    namespaces.Add("xsd", "http://www.w3.org/2001/XMLSchema");
                    namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");

                    new XmlSerializer(this.GetType()).Serialize(xmlWriter, this, namespaces);
                }

                // Load the serialized XML into an XDocument
                XDocument doc = XDocument.Parse(output.ToString());

                XElement root = doc.Root;
                if (root != null) {
                    XAttribute toolVersionAttr = root.Attribute("ToolVersion");
                    if (toolVersionAttr != null) {
                        toolVersionAttr.Remove();
                        root.Add(toolVersionAttr);
                    }
                }

                // Return the modified XML string
                return doc.ToString();
            }
        }
    }
}
