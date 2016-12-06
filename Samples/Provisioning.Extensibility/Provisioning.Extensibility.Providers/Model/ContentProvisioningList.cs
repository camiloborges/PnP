using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Provisioning.Extensibility.Providers.Model
{
    public class ContentProvisioningList
    {
        [XmlAttribute]
        public string Name;
        public string CamlQuery;

    }
}
