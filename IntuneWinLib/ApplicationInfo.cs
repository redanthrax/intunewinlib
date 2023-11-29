using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace IntuneWinLib {
    [XmlRoot("ApplicationInfo")]
    [XmlInclude(typeof(MsiApplicationInfo))]
    [XmlInclude(typeof(CustomApplicationInfo))]
    [Serializable]
    public abstract class ApplicationInfo {
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
                    new XmlSerializer(this.GetType()).Serialize(xmlWriter, this, namespaces);
                }

                return output.ToString();
            }
        }
    }
}
