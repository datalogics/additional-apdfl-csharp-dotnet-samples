using System.Configuration;
using System.Data;
using System.Windows;

namespace WPFviewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private void App_Startup(object sender, StartupEventArgs e)
        {
            // if (e.Args.Length == 0) return; // A way to examine Command-arguments.
            for(int i = 0;i < e.Args.Length;i++) 
                Console.WriteLine(e.Args[i]);

        }
    }
}
