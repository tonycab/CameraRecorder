using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;

namespace CameraRecorder.ViewModel
{
    public class AppParamsViewModel : INotifyPropertyChanged
    {

        public  Params ParamsRecorder { get; set; }


        public RelayCommand SaveCommand { get; }
        public RelayCommand AddCameraCommand { get; }
        public RelayCommand RemoveCameraCommand { get; }

        private CameraParams _selectedCamera;
        public CameraParams SelectedCamera
        {
            get => _selectedCamera;
            set { _selectedCamera = value; OnPropertyChanged(nameof(SelectedCamera)); (RemoveCameraCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        public string FilePath {  get; set; }

        public AppParamsViewModel(string filePath)
        {
            FilePath = filePath;
            SaveCommand = new RelayCommand(o => SaveToXml(o));
            AddCameraCommand = new RelayCommand(_ => AddCamera());
            RemoveCameraCommand = new RelayCommand(_ => RemoveCamera(), (o) => SelectedCamera != null);
            
        }

        // 🔹 Chargement automatique au démarrage
        public void LoadAtStartup()
        {
            if (File.Exists(FilePath))
            {
                try
                {
                    ParamsRecorder = Params.LoadFromFile(FilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur au chargement de la configuration : " + ex.Message);
                }
            }
            else
            {
                // Fichier non trouvé → valeurs par défaut
                ParamsRecorder = new Params
                {
                    PathFolderRecorder = "%USERPROFILE%\\Videos",
                    SizeFolder = 1000,
                    SizeFile = 500,
                    IPplc = "192.168.0.10",
                    DBnumber = 300,
                    CamerasParams = new ObservableCollection<CameraParams>
                {
                    new CameraParams
                    {
                        IsValid = true,
                        Name = "Caméra 1",
                        AdresseIP = "192.168.0.101",
                        User = "admin",
                        Password = "",
                        Url = "/Streaming/Channels/101",
                        BufferSizeSeconds = 60
                    }
                }
                };
                

                try
                {
                    ParamsRecorder.SaveToFile(FilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Impossible de créer le fichier de configuration : " + ex.Message);
                }
            }
        }

        // 🔹 Sauvegarde automatique
        private void SaveToXml(object obj)
        {
           
            try
            {
           
                ParamsRecorder.SaveToFile(FilePath);

                if (obj is System.Windows.Window window)
                    window.Close();

                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });

                // Fermer l'application entière
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la sauvegarde : " + ex.Message);
            }
        }

        //// --- Validation (inchangée) ---
        //public string Error => null;
        //public string this[string columnName]
        //{
        //    get
        //    {
        //        return columnName switch
        //        {
        //            nameof(ParamsRecorder.PathFolderRecorder) => !Directory.Exists(Environment.ExpandEnvironmentVariables(ParamsRecorder.PathFolderRecorder)) ? "Dossier inexistant." : null,
        //            nameof(ParamsRecorder.IPplc) => !IPAddress.TryParse(ParamsRecorder.IPplc, out _) ? "Adresse IP invalide." : null,
        //            nameof(ParamsRecorder.DBnumber) => ParamsRecorder.DBnumber < 0 ? "Doit être positif." : null,
        //            _ => null
        //        };
        //    }
        //}

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        private void AddCamera() => ParamsRecorder.CamerasParams.Add(new CameraParams { Name = "Nouvelle caméra" });
        private void RemoveCamera() { if (SelectedCamera != null) ParamsRecorder.CamerasParams.Remove(SelectedCamera); }
    }
}

