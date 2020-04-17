using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLibrary
{
    public class RTException : Exception
    {
        public RTException(string message)
            : base(message)
        {

        }
    }
}
