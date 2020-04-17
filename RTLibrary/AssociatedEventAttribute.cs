using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLibrary
{
    [AttributeUsage(AttributeTargets.Delegate | AttributeTargets.Method)]
    public sealed class AssociatedEventAttribute : Attribute
    {
        public string EventName;
        public AssociatedEventAttribute(string Event)
        {
            EventName = Event;
        }
    }
}
