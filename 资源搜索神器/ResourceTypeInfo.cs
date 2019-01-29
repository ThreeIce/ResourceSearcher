using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace 资源搜索神器
{
    [Serializable]
    public struct ResourceTypeInfo
    {
        public string name;
        public string AddressRegex;
        public string pwRegex;
        public string verifyRegex;

        public ResourceTypeInfo(string name, string addressRegex, string pwRegex,string verifyRegex)
        {
            this.name = name;
            AddressRegex = addressRegex;
            this.pwRegex = pwRegex;
            this.verifyRegex = verifyRegex;
        }
    }
}
