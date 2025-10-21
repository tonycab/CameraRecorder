using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CameraRecorder
{
    public class Params : INotifyPropertyChanged, IDataErrorInfo
    {

        private string pathFolderRecorder;
        public string PathFolderRecorder
        {
            get => pathFolderRecorder;
            set
            {
                if (pathFolderRecorder != value)
                {
                    pathFolderRecorder = value;
                    OnPropertyChanged();
                }
            }
        }
        private int sizeFolder;
        public int SizeFolder
        {
            get => sizeFolder;
            set
            {
                if (sizeFolder != value)
                {
                    sizeFolder = value;
                    OnPropertyChanged();
                }
            }
        }

        private int sizeFile;
        public int SizeFile
        {
            get => sizeFile;
            set
            {
                if (sizeFile != value)
                {
                    sizeFile = value;
                    OnPropertyChanged();
                }
            }
        }

        private string iPplc;
        public string IPplc
        {
            get => iPplc;
            set
            {
                if (iPplc != value)
                {
                    iPplc = value;
                    OnPropertyChanged();
                }
            }
        }

        private int dBnumber;
        public int DBnumber
        {
            get => dBnumber;
            set
            {
                if (dBnumber != value)
                {
                    dBnumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<CameraParams> CamerasParams { get; set; } = new ObservableCollection<CameraParams>();



        public Params() { }

        /// <summary>
        /// Sérialise la configuration complète en XML string
        /// </summary>
        public string ToXml()
        {
            var doc = new XDocument(
                new XElement(nameof(Params),
                    new XElement(nameof(PathFolderRecorder), PathFolderRecorder ?? ""),
                    new XElement(nameof(SizeFolder), SizeFolder),
                    new XElement(nameof(SizeFile), SizeFile),
                    new XElement(nameof(IPplc), IPplc ?? "192.32.98.50"),
                    new XElement(nameof(DBnumber), DBnumber),
                    new XElement(nameof(CamerasParams),
                        from cam in CamerasParams
                        select cam.ToXml()
                    )
                )
            );
            return doc.ToString();
        }

        /// <summary>
        /// Désérialise une configuration depuis XML string
        /// </summary>
        public static Params FromXml(string xml)
        {
            var doc = XDocument.Parse(xml);
            var root = doc.Element(nameof(Params));

            var config = new Params
            {
                PathFolderRecorder = root.Element(nameof(PathFolderRecorder))?.Value,
                SizeFolder = int.TryParse(root.Element(nameof(SizeFolder))?.Value,out int u) ? u : 1000,
                SizeFile = int.TryParse(root.Element(nameof(SizeFile))?.Value, out int v) ? v : 200,
                IPplc = root.Element(nameof(IPplc))?.Value,
                DBnumber = int.Parse(root.Element(nameof(DBnumber))?.Value)
            };

            var cameraElements = root.Element(nameof(CamerasParams))?.Elements(nameof(CameraParams));
            if (cameraElements != null)
            {
                foreach (var elem in cameraElements)
                {
                    config.CamerasParams.Add(CameraParams.FromXml(elem));
                }
            }

            return config;
        }

        public void SaveToFile(string filePath) => File.WriteAllText(filePath, ToXml());
        public static Params LoadFromFile(string filePath) => FromXml(File.ReadAllText(filePath));


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }
        // --- Validation (inchangée) ---
        public string Error => null;
        public string this[string columnName]
        {
            get
            {
                return columnName switch
                {
                    nameof(PathFolderRecorder) => !Directory.Exists(Environment.ExpandEnvironmentVariables(PathFolderRecorder)) ? "Dossier inexistant." : null,
                    nameof(IPplc) => !IPAddress.TryParse(IPplc, out _) ? "Adresse IP invalide." : null,
                    nameof(DBnumber) => DBnumber < 0 ? "Doit être positif." : null,
                   
                    _ => null
                };
            }
        }
    }
}
