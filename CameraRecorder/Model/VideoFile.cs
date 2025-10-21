using Shell32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using Shell32;
using System.ComponentModel;

namespace CameraRecorder.Model
{

    public class VideoFile : INotifyPropertyChanged
    {
        public string FilePath { get; set; }
        public string Name => Path.GetFileName(FilePath);
        public TimeSpan? Duration { get; set; }
     

        public DateTime? Date {  get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set { isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        private void OnPropertyChanged(string v)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(v));
            }
        }
        public static TimeSpan? GetVideoDuration(string path)
        {
         
                return GetDuration(path);
            
        }

        public static TimeSpan? GetDuration(string filePath)
        {
            try
            {
                var shell = new Shell();
                var folderPath = System.IO.Path.GetDirectoryName(filePath);
                var fileName = System.IO.Path.GetFileName(filePath);

                var folder = shell.NameSpace(folderPath);
                if (folder == null)
                    return null;

                var file = folder.ParseName(fileName);
                if (file == null)
                    return null;

                // Index 27 correspond à "Durée" sur la plupart des systèmes Windows
                string durationStr = folder.GetDetailsOf(file, 27);

                if (TimeSpan.TryParse(durationStr, out TimeSpan duration))
                    return duration;

                // Parfois le format est "mm:ss", il faut parser manuellement
                var parts = durationStr.Split(':');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int min) &&
                    int.TryParse(parts[1], out int sec))
                {
                    return new TimeSpan(0, min, sec);
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }
    }
}


