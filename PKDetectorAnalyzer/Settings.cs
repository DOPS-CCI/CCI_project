using System.ComponentModel;

namespace PKDetectorAnalyzer.Properties
{

    internal sealed partial class Settings
    {
        public Settings()
        {
            this.PropertyChanged += this.PropertyChangedEventHandler;
        }

        private void PropertyChangedEventHandler(object sender, PropertyChangedEventArgs e)
        {
            this.Save();
        }
    }
}
