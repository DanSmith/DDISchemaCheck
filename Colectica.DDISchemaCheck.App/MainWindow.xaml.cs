using Colectica.DDISchemaCheck.Checks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Colectica.DDISchemaCheck.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".xsd";
            dialog.Filter = "DDI Instance|instance.xsd";
            //dialog.Filter = "*.xsd|*.xsd";//for testing

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                ReportGenerator helpers = new ReportGenerator();
                string filename = dialog.FileName;

                string reportFilename = helpers.CreateReport(filename);
                Uri uri = new Uri(reportFilename, UriKind.Absolute);
                string path = uri.ToString();
                Process.Start(path);
            }

            
        }
    }
}
