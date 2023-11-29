using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace IntuneWinLib {
    public class FileEncryptionInfo {
        public string EncryptionKey { get; set; }

        public string MacKey { get; set; }

        public string InitializationVector { get; set; }

        public string Mac { get; set; }

        public string ProfileIdentifier { get; set; }

        public string FileDigest { get; set; }

        public string FileDigestAlgorithm { get; set; }
    }
}
