using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace IntuneWinLib {
    [XmlRoot("ApplicationInfo")]
    [Serializable]
    public class MsiApplicationInfo : ApplicationInfo {
        public MsiApplicationInfo() => this.MsiInfo = new MsiInfo();

        public MsiInfo MsiInfo { get; set; }
    }
}
