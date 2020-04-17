using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RTTestApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        void MainStartUp(object sender, StartupEventArgs e)
        {
            MyExperiment experiment = new MyExperiment();
        }
    }
}
