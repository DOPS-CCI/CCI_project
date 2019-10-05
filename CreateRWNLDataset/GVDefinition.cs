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
        internal int param = -1;

        int lastV = -1;
        public int nextGV
        {
            get
            {
                if (cyclic)
                {
                    lastV = (++lastV) % Nmax;
                    return lastV + 1;
                }
                return (int)Util.UniformRND(0D, (double)Nmax) + 1;
            }
        }
    }
}
