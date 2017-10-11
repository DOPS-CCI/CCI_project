using System;
namespace CCIUtilities
{
    public interface IValidate
    {
        bool Validate(object o = null);
        event EventHandler ErrorCheckReq;
    }
}
