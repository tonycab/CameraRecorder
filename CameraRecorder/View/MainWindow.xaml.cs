using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LibVLCSharp.Shared;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading;
using System.IO;
using LibVLCSharp.WPF;
using System.Windows.Media.Media3D;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using CameraRecorder.ViewModel;
using CameraRecorder.View;


namespace CameraRecorder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        public BindingList<Log> Logs => LogsManager.GetLogs();

        RecorderViewModel recorder;



        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
        }

        private async void  MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
           
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    recorder = new RecorderViewModel();

                    DataContext = recorder;

                    var folder = Environment.ExpandEnvironmentVariables(recorder.appParams.ParamsRecorder.PathFolderRecorder);
                    Vm.DataContext = new VideoManagerViewModel(folder, recorder.appParams.ParamsRecorder.SizeFolder);

                    affichageVideo(recorder.Cameras);
                });

            });
        }

        private void LogsDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (e.Row.GetIndex() == logs.Items.Count - 1)
            {
                logs.ScrollIntoView(e.Row.Item);
            }
        }


        public void affichageVideo(List<CameraRTSP> cameras)
        {

            var grid = new UniformGrid();

            // Calcul dynamique des lignes/colonnes
            grid.Rows = CalculateGrid(cameras.Where((c) => c.CameraParams.IsValid == true).ToList().Count).Cols;
            grid.Columns = CalculateGrid(cameras.Where((c) => c.CameraParams.IsValid == true).ToList().Count).Rows;

            grid.HorizontalAlignment = HorizontalAlignment.Stretch;
            grid.VerticalAlignment = VerticalAlignment.Stretch;


            foreach (CameraRTSP cam in cameras)
            {

                if (cam.CameraParams.IsValid && cam.CameraParams.ViewEnable)
                {
                    //Affichage de la camera
                    var videoView = new VideoView()
                    {
                        Name = cam.CameraParams.Name,

                        MinWidth = 2560 * 1 / 40,
                        MinHeight = 1440 * 1 / 40,


                    };
                    videoView.MediaPlayer = cam.MediaPlayer;

                    grid.Children.Add(videoView);
                }
            }

            grilleCamera.Children.Add(grid);


            foreach (CameraRTSP cam in cameras)
            {
                cam.Play();
            }

        }

        public static (int Rows, int Cols) CalculateGrid(int cameraCount)
        {
            if (cameraCount <= 0)
                return (0, 0);

            // On part de la racine carrée pour équilibrer
            int rows = (int)Math.Ceiling(Math.Sqrt(cameraCount));
            int cols = (int)Math.Ceiling((double)cameraCount / rows);

            return (rows, cols);
        }





    }
}