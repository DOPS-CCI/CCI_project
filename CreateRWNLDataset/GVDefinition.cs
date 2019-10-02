using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCIUtilities;
using GroupVarDictionary;

namespace CreateRWNLDataset
{
    public class GVDefinition
    {
        internal string Name;
        internal int Nmax;
        internal Polynomial map;
        internal bool cyclic = true;
        internal string param = "None";
    }
}
