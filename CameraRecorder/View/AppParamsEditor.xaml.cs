using CameraRecorder.ViewModel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace CameraRecorder.View
{
    /// <summary>
    /// Logique d'interaction pour AppParamsEditor.xaml
    /// </summary>
    public partial class AppParamsEditor : Window
    {
        public AppParamsEditor()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            var appParamsViewModel = DataContext as AppParamsViewModel;

            var u = new OpenFolderDialog();

            if (u.ShowDialog() == true)
            {

                Chemin.Text = u.FolderName;
            }



        }
    }
}
