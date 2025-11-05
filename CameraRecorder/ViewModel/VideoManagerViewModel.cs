
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Microsoft.Win32;
using System.Threading.Tasks;
using CameraRecorder.Model;
using System;
using System.Diagnostics;
using System.Windows.Media.Animation;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;



namespace CameraRecorder.ViewModel
{

    public class VideoManagerViewModel : INotifyPropertyChanged
    {
        //private readonly string _folderPath;
        private FileSystemWatcher _watcher;

        public  string FolderPath { get; private set; }

        public ObservableCollection<VideoFile> Videos { get; set; } = new();

        public ICommand CopyCommand { get; }
        public ICommand CopyMp4Command { get; }
        public ICommand DeleteCommand { get; }

        public ICommand AllSelect { get; }

        public ICommand NoAllSelect { get; }

        public ICommand OpenVideoCommand { get; }


        private double sizeFolderMax=300;

        //Taille max du répertoire en Go
        public double SizeFolderMax
        {
            get
            {
                return sizeFolderMax;
            }
            set
            {
                if (sizeFolderMax != value)
                {
                    sizeFolderMax = value;
                    OnPropertyChanged(nameof(SizeFolderMax));
                    SizeFolderString = $"{FormatSize(SizeFolder)} / {FormatSize(SizeFolderMax * 1024 * 1024)}";
                }
            }
        }

        //Taille actuelle du répertoire en Mo
        private double sizeFolder=0;
        public double SizeFolder
        {
            get
            {
                return sizeFolder;
            }
            set
            {
                if (sizeFolderMax != value)
                {
                    sizeFolder = value;
                    OnPropertyChanged(nameof(SizeFolder));
                    SizeFolderString = $"{ FormatSize(SizeFolder)} / { FormatSize(SizeFolderMax * 1024 * 1024)}" ;
                }
            }
        }


        //String affichant la taille du répertoire
        private string sizeFolderString;
        public string SizeFolderString
        {
            get
            {
                return sizeFolderString;
            }
            private set
            {
                if (sizeFolderString != value)
                {
                    sizeFolderString = value;
                    OnPropertyChanged(nameof(SizeFolderString));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
       
        public void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public VideoManagerViewModel() { }

        public VideoManagerViewModel(string folderPath, double sizeFolderMax)
        {

            SizeFolderMax = sizeFolderMax;

            OpenVideoCommand = new RelayCommand<VideoFile>(OpenVideo);

            FolderPath = folderPath;

            LoadVideos();

            // FileSystemWatcher pour MAJ automatique
            _watcher = new FileSystemWatcher(FolderPath)
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };
            _watcher.Created += (s, e) => App.Current.Dispatcher.Invoke(LoadVideos);
            _watcher.Deleted += (s, e) => App.Current.Dispatcher.Invoke(LoadVideos);
            _watcher.Renamed += (s, e) => App.Current.Dispatcher.Invoke(LoadVideos);
            _watcher.Changed += (s, e) => App.Current.Dispatcher.Invoke(LoadVideos);

            CopyCommand = new RelayCommand(async _ => await CopySelectedVideos());
            DeleteCommand = new RelayCommand(async _ => await DeleteSelectedVideos());
            AllSelect = new RelayCommand(async _ => await AllSelectVideos());
            NoAllSelect = new RelayCommand(async _ => await NoAllSelectVideos());
            CopyMp4Command = new RelayCommand(async _ => await CopySelectedVideosMP4());
        }

        private async Task NoAllSelectVideos()
        {
            foreach(var video in Videos)
            {
                video.IsSelected = false;   

            }
        }

        private async Task AllSelectVideos()
        {
            foreach (var video in Videos)
            {
                video.IsSelected = true;

            }
        }

        public static bool DeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true; // succès
                }
                return false; // le fichier n'existait pas
            }
            catch (UnauthorizedAccessException)
            {
                // Le fichier est en lecture seule ou permissions insuffisantes
                Console.WriteLine($"Accès refusé : {filePath}");
                return false;
            }
            catch (IOException ex)
            {
                // Le fichier est ouvert ou utilisé par un autre processus
                Console.WriteLine($"Erreur IO : {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // Autres exceptions
                Console.WriteLine($"Erreur : {ex.Message}");
                return false;
            }
        }
        private static string FormatSize(double bytes)
        {
            const double Ko = 1024;
            const double Mo = Ko * 1024;
            const double Go = Mo * 1024;

            if (bytes >= Go)
                return $"{bytes / Go:F2} Go";
            if (bytes >= Mo)
                return $"{bytes / Mo:F2} Mo";
            if (bytes >= Ko)
                return $"{bytes / Ko:F2} Ko";
            return $"{bytes} o";
        }
        public static double GetFolderSize(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return 0;

            DirectoryInfo dir = new DirectoryInfo(folderPath);

            // Récupère tous les fichiers, y compris dans les sous-dossiers
            long totalBytes = dir.GetFiles("*", SearchOption.AllDirectories)
                                 .Sum(f => f.Length);
           
            return totalBytes;
        }


        private void LoadVideos()
        {
            Videos.Clear();

            var extensions = new[] { "*.mp4", "*.ts" };

            var lf = extensions
                .SelectMany(ext => Directory.GetFiles(FolderPath, ext))
                .ToArray();

            if (lf != null)
            {

                foreach (var file in lf)
                {
                    Videos.Add(new VideoFile
                    {
                        FilePath = file,
                        Duration = VideoFile.GetVideoDuration(file),
                        Date = GetCreationDate(file),
                    });
                }
            }

            if (Videos != null && Videos.Count > 0)
            {
                SizeFolder = GetFolderSize(FolderPath);

                if (SizeFolder > SizeFolderMax*1024*1024)
                {
                    var lastfile = Videos.OrderByDescending(f => f.Date).Last();

                    DeleteFile(lastfile.FilePath);

                }
            }


        }
        public static DateTime? GetCreationDate(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    FileInfo fi = new FileInfo(filePath);
                    return fi.CreationTime;
                }
                return null; // fichier inexistant
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération de la date : {ex.Message}");
                return null;
            }
        }
        private async Task CopySelectedVideos()
        {
            var selected = Videos.Where(v => v.IsSelected).ToList();
            if (!selected.Any())
            {
                System.Windows.MessageBox.Show("Aucune vidéo sélectionnée.", "Information");
                return;
            }

            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Choisissez le dossier de destination"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string destinationFolder = dialog.FileName;

                foreach (var video in selected)
                {
                    string destPath = Path.Combine(destinationFolder, video.Name);
                    await Task.Run(() => File.Copy(video.FilePath, destPath, true));
                }

                System.Windows.MessageBox.Show("Copie terminée ✅", "Succès");
            }
        }

        private async Task CopySelectedVideosMP4()
        {
            var selected = Videos.Where(v => v.IsSelected).ToList();
            if (!selected.Any())
            {
                System.Windows.MessageBox.Show("Aucune vidéo sélectionnée.", "Information");
                return;
            }

            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Choisissez le dossier de destination"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string destinationFolder = dialog.FileName;

                foreach (var video in selected)
                {
                 

                    string destPath = Path.Combine(destinationFolder, Path.GetFileNameWithoutExtension(video.Name) + ".mp4");

                    await ConvertTsToMp4Async(video.FilePath, destPath);

                    //await Task.Run(() => File.Copy(video.FilePath, destPath, true));
                }

                System.Windows.MessageBox.Show("Copie terminée ✅", "Succès");
            }
        }

        /// <summary>
        /// Convertit un fichier .ts en .mp4 très compressé à l'aide de ffmpeg.
        /// </summary>
        /// <param name="inputPath">Chemin complet du fichier source (.ts)</param>
        /// <param name="outputPath">Chemin complet du fichier cible (.mp4)</param>
        private static async Task<string> ConvertTsToMp4Async(string inputPath, string outputPath)
        {
            if (!System.IO.File.Exists(inputPath))
                throw new FileNotFoundException("Le fichier source n'existe pas.", inputPath);

            // Paramètres de compression : H.264 + CRF 28 pour une forte compression
            string arguments = $"-i \"{inputPath}\" -c:v libx264 -preset veryslow -crf 28 -c:a aac -b:a 96k -movflags +faststart \"{outputPath}\" -y";

            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            // Optionnel : lire la sortie d’erreur pour suivre la progression
            string stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new Exception($"Erreur lors de la conversion FFmpeg : {stderr}");

            return outputPath;
        }



        private async Task DeleteSelectedVideos()
        {
            var selected = Videos.Where(v => v.IsSelected).ToList();
            foreach (var video in selected)
            {
                await Task.Run(() =>
                {
                    if (File.Exists(video.FilePath))
                        File.Delete(video.FilePath);
                });
            }
            LoadVideos();
        }
    
    private void OpenVideo(VideoFile video)
        {
            if (video == null || string.IsNullOrEmpty(video.FilePath))
                return;

            // Méthode simple : ouvre la vidéo avec le lecteur par défaut
            Process.Start(new ProcessStartInfo
            {
                FileName = video.FilePath,
                UseShellExecute = true
            });
        }
    }

}


