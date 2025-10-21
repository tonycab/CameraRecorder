using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using CameraRecorder.ViewModel;

namespace CameraRecorder.View
{
    /// <summary>
    /// Logique d'interaction pour VideoManagerView.xaml
    /// </summary>
    /// 

    public partial class VideoManagerView : UserControl
    {

        public VideoManagerView()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            string folderPath = ((VideoManagerViewModel)DataContext).FolderPath; // ton dossier

            if (System.IO.Directory.Exists(folderPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            else
            {
                MessageBox.Show("Le dossier n'existe pas.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
      
    }
    }
}
