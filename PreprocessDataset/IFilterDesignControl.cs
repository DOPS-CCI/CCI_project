using CCIUtilities;
using DigitalFilter;

namespace PreprocessDataset
{
    public interface IFilterDesignControl : IValidate
    {
        IIRFilter Filter { get; }

        IIRFilter FinishDesign();

        IIRFilter FilterDesign { get; }
    }
}
