using CameraRecorder.View;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace CameraRecorder.ViewModel
{
    public class RecorderViewModel : INotifyPropertyChanged
    {

        public BindingList<Log> Logs => LogsManager.GetLogs();

        public ICommand StartRecorderCommand { get; set; }
        public ICommand StopRecorderCommand { get; set; }
        public ICommand ParamsRecorderCommand { get; set; }

        public List<CameraRTSP> Cameras = new List<CameraRTSP>();

        public ComPLC plc { get; set; }

        

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _isRecording;

        public bool IsRecording
        {
            get => _isRecording;
            set { _isRecording = value; OnPropertyChanged(nameof(IsRecording)); }
        }

        private void OnPropertyChanged(string v)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(v));
            }
        }

        //public AppParams appParams{ get; set; }
        public AppParamsViewModel appParams { get; set; }


        AppParamsEditor appParamsEditor;
        public RecorderViewModel()
        {
            try
            {

                appParams = new AppParamsViewModel(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.xml"));
                appParams.LoadAtStartup(); // 🔹 charge AppParams.xml automatiquement

            }
            catch (Exception ex) { 
            
                MessageBox.Show("Error in config file");
                Application.Current.Shutdown();
                return;
            }
           

                //Creation des caméras - Affichage et lecture
                foreach (var cameraParams in appParams.ParamsRecorder.CamerasParams)
                {
                    if (cameraParams.IsValid)
                    {
                        //Instanciation d'un caméra
                        var camera = new CameraRTSP(cameraParams) { SizeFile=appParams.ParamsRecorder.SizeFile};

                        //Ajout de la camera à la liste
                        Cameras.Add(camera);

                        //Lancement de la lecture de la camera
                        //camera.Play();
                    }
                }
            

            //Bouton d'enregistrement
            StartRecorderCommand = new RelayCommand(new Action<object>((o) => StartRecorder("Local")));
            StopRecorderCommand = new RelayCommand(new Action<object>((o) => StopRecorder()));
            ParamsRecorderCommand = new RelayCommand(new Action<object>((o) => ParamsRecorder()));

            //Initialisae la communication avec le PLC
            plc = new ComPLC(appParams.ParamsRecorder.IPplc, appParams.ParamsRecorder.DBnumber);

            //Abonnement aux événements du PLC
            plc.OnRecord += (f) => StartRecorder(f);
            plc.StopRecord += () => StopRecorder();

            //Lancement de la communication
            plc.StartCom();
        }

        private void ParamsRecorder()
        {
            appParamsEditor = new AppParamsEditor
            {
                DataContext = appParams
            };

            appParamsEditor.Show();
        }

        private void StartRecorder(string ArgPlc)
        {

            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var camera in Cameras)
                {
                    if (!camera.isRecording)
                    {
                        var folder = Environment.ExpandEnvironmentVariables(appParams.ParamsRecorder.PathFolderRecorder);
                        camera.StartRecording(Path.Combine(folder, $"{ArgPlc}_{camera.CameraParams.Name}_{DateTime.Now:yyyyMMdd_HHmmss}"));

                        Logs.Add(new Log(DateTime.Now.ToString(), EnumCategory.Info, camera.CameraParams.Name, $"Start Recording"));
                    }
                }
            });

            IsRecording = true;
        }

        private void StopRecorder()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var camera in Cameras)
                {
                    if (camera.isRecording)
                    {
                        camera.StopRecording();

                        Logs.Add(new Log(DateTime.Now.ToString(), EnumCategory.Info, camera.CameraParams.Name, $"Recording stopped file to {System.IO.Path.GetFullPath(camera.OutputFile)}.ts"));
                    }
                }
            });

            IsRecording = false;
        }
   
    }
}
