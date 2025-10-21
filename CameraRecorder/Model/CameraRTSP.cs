using LibVLCSharp.Shared;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Shapes;
using System.Windows.Threading;
using static System.Net.WebRequestMethods;


namespace CameraRecorder
{
    public class CameraRTSP : IDisposable
    {
        public CameraParams CameraParams { get; private set; }
        public MediaPlayer MediaPlayer { get; set; }
        public bool isRecording { get; private set; } = false;
        public string OutputFile
        {
            get
            {
                return $"{OutputFileName}_part_{part}";
            }
        }



        public int SizeFile { get; set; } = 300;
        private readonly LibVLC _libVLC;


        private readonly ConcurrentQueue<(DateTime Timestamp, byte[] Data, int Length)> _bufferQueue = new();
        private readonly object _recordLock = new();
        private FileStream? _recordStream;
        private CancellationTokenSource? _cts;
        private CancellationTokenSource? _cts2;
        private Task? _readerTask;
        private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
        private Media media;
        private string OutputFileName;


        public CameraRTSP(CameraParams cameraParams)
        {
            CameraParams = cameraParams;

            Core.Initialize();

            // Options de démarrage VLC
            var vlcOptions = new[]
            {
                "--no-video-title-show",     // Pas de titre affiché
                "--no-sub-autodetect-file",  // Pas de détection automatique de sous-titres
                "--no-xlib",                 // Évite de charger X11 (inutile sous Windows)
                "--no-snapshot-preview",     // Pas de preview quand tu fais un snapshot
                "--quiet",                   // Réduit la verbosité
                "--no-plugins-cache",        // Optionnel : pour tester sans cache
                "--plugin-path=./plugins_min"// Ton dossier plugins réduit
            };

            _libVLC = new LibVLC();

            MediaPlayer = new MediaPlayer(_libVLC);

        }

        /// <summary>
        /// Retourne le chemin complet du flux video
        /// </summary>
        /// <returns>Chemin complet du flux video</returns>
        public string GetFullRtspUrl()
        {
            if (!string.IsNullOrEmpty(CameraParams.User) && !string.IsNullOrEmpty(CameraParams.Password))
            {
                var user = CameraParams.User ?? "";
                var pass = CameraParams.Password != null ? Uri.EscapeDataString(CameraParams.Password) : "";
                return $"rtsp://{user}:{pass}@{CameraParams.AdresseIP}{CameraParams.Url}";
            }
            return CameraParams.Url;
        }

        /// <summary>
        /// Lance la lecture du flux et de l'enregistrement circulaire
        /// </summary>
        public void Play()
        {

            StartBuffering();

            if (CameraParams.IsValid && CameraParams.ViewEnable)
            {

                media = new Media(_libVLC, new Uri(GetFullRtspUrl()), ":rtsp-tcp", ":network-caching=1000");

                MediaPlayer.Play(media);

                media.StateChanged += StateVlcChanged;

            }

        }

        /// <summary>
        /// Redemarre la lecture du flux video en cas d'erreur
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void StateVlcChanged(object o, MediaStateChangedEventArgs e)
        {
            LogsManager.Add(EnumCategory.Process, "Cam", e.State.ToString());
            if (e.State == VLCState.Ended || e.State == VLCState.Error || e.State == VLCState.NothingSpecial)
            {
                // Détache l’ancien handler pour éviter plusieurs triggers
                media.StateChanged -= StateVlcChanged;

                // Relance le flux
                RestartMedia();
            }
        }

        /// <summary>
        /// Redemarre la lecture du flux video
        /// </summary>
        private void RestartMedia()
        {
            Task.Delay(2000).ContinueWith((Action<Task>)(_ =>
            {

                // Dispose ancien media
                media?.Dispose();

                // Crée un nouveau media
                media = new Media(_libVLC, new Uri(GetFullRtspUrl()), ":rtsp-tcp", ":network-caching=1000", ":keep-alive");
                this.MediaPlayer.Play(media);

                //// Événement pour le nouvel media
                media.StateChanged += StateVlcChanged;

            }));
        }

        /// <summary>
        /// Enregistrement circulaire et enregistrement direct
        /// </summary>
        private void StartBuffering()
        {

            _readerTask = Task.Run(async () =>
            {
                _cts2 = new CancellationTokenSource();

                while (!_cts2.Token.IsCancellationRequested)
                {

                    _cts = new CancellationTokenSource();


                    while (!_cts.Token.IsCancellationRequested)
                    {
                        var ffmpeg = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe"),
                                Arguments = $" -rtsp_transport tcp -timeout 5000000  -i \"{GetFullRtspUrl()}\" -c copy -f mpegts pipe:1",
                                RedirectStandardOutput = true,
                                StandardOutputEncoding = null,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };

                        ffmpeg.ErrorDataReceived += (s, e) =>
                        {

                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                Debug.WriteLine("[FFMPEG] " + e.Data);

                                // Détecter les mots clés qui indiquent un flux mort
                                if (e.Data.Contains("Connection refused") ||
                                    e.Data.Contains("Too many errors") ||
                                    e.Data.Contains("Protocol not found") ||
                                    e.Data.Contains("Failed reading") ||
                                    e.Data.Contains("Connection timed out") ||
                                    e.Data.Contains("Error opening"))

                                {
                                    // signaler qu'il faut relancer
                                    _cts?.Cancel();
                                    //try { ffmpeg.Kill(); } catch { }
                                    //ffmpeg.WaitForExit();
                                }
                            }
                        };



                        try
                        {
                            ffmpeg.Start();

                            LogsManager.Add(EnumCategory.Info, "ffmpeg", "Flux connecting");

                            var stdout = ffmpeg.StandardOutput.BaseStream;
                            var buffer = new byte[4096];


                            ffmpeg.BeginErrorReadLine();

                            var fluxok = false;


                            while (!_cts.Token.IsCancellationRequested)
                            {
                                //Lecture des données vidéo
                                var d1 = DateTime.Now;
                                int read = await stdout.ReadAsync(buffer, 0, buffer.Length, _cts.Token)
                                 .WaitAsync(TimeSpan.FromSeconds(5));

                                var d2 = DateTime.Now;
                                var d3 = d1 - d2;
                                var tolerance = TimeSpan.FromSeconds(5);
                                if (d3 > tolerance)
                                {
                                    _cts?.Cancel();
                                }

                                if (read > 0)
                                {

                                    if (!fluxok)
                                    {
                                        LogsManager.Add(EnumCategory.Info, "ffmpeg", "Flux connected");
                                        fluxok = true;
                                    }

                                    //Loue un tableau de byte
                                    var rented = _arrayPool.Rent(read);

                                    //Copy les données dans le tableau
                                    Array.Copy(buffer, rented, read);

                                    //Ecrit le tableau et la date dans un liste
                                    _bufferQueue.Enqueue((DateTime.Now, rented, read));

                                    //Purge les donnnées trop ancienne
                                    PurgeOldBuffer();

                                    // écriture directe si enregistrement
                                    if (isRecording && _recordStream != null)
                                    {
                                        lock (_recordLock)
                                        {
                                            _recordStream.Write(rented, 0, read);

                                        }
                                    }

                                    //Découpe les fichiers en 500Mo 
                                    if (_recordStream != null && _recordStream.Length > SizeFile * 1024 * 1024)
                                    {
                                        _recordStream?.Flush(true);
                                        _recordStream?.Dispose();
                                        //ConvertToMp4(OutputFile, OutputFile + ".mp4");
                                        
                                        part++;
                                        _recordStream = new FileStream(OutputFile + ".ts", FileMode.Append, FileAccess.Write);              

                                    }
                                }
                                else
                                {
                                    Thread.Sleep(10);
                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            LogsManager.Add(EnumCategory.Error, "ffmpeg", ex.Message);
                            Debug.WriteLine("FFmpeg process crashed: " + ex);
                            Thread.Sleep(2000);
                        }
                        finally
                        {
                            try { ffmpeg.Kill(); } catch { }
                            ffmpeg.WaitForExit();

                        }
                    }

                    LogsManager.Add(EnumCategory.Error, "ffmpeg", "Flux interrompu");
                }
            });

        }

        //Purge sécurisée pour conserver uniquement la dernière minute
        private void PurgeOldBuffer()
        {
            //Determine la date trop ancienne
            var cutoffTime = DateTime.Now.AddSeconds(-CameraParams.BufferSizeSeconds);

            //Compare les plus anciennes données
            while (_bufferQueue.TryPeek(out var oldest) && oldest.Timestamp < cutoffTime)
            {
                //Supprime les données périmés
                if (_bufferQueue.TryDequeue(out var old))

                    //Rend le tableau alloué
                    _arrayPool.Return(old.Data);
            }
        }

        int part = 0;

        // Démarrer un enregistrement
        // minDurationSeconds = 60 pour les vidéos suivantes
        public void StartRecording(string outputFile, int minDurationSeconds = 60)
        {

            OutputFileName = outputFile;

            if (isRecording) return;

            part = 0;

            lock (_recordLock)
            {

                //_recordStream = new FileStream(OutputFile, FileMode.Create, FileAccess.Write);
                _recordStream = new FileStream(OutputFile + ".ts", FileMode.Append, FileAccess.Write);

                isRecording = true;

                // écrire la dernière minute du buffer
                var cutoff = DateTime.Now.AddSeconds(-CameraParams.BufferSizeSeconds);
                foreach (var (timestampDt, chunk, length) in _bufferQueue)
                {
                    if (timestampDt >= cutoff)
                    {
                        _recordStream.Write(chunk, 0, length);
                    }
                }

                Debug.WriteLine($"[CameraRTSP2] Recording started → {OutputFile}");

            }
        }

        /// <summary>
        /// Stop l'enregistrement
        /// </summary>
        public async void StopRecording()
        {
            if (isRecording)
            {
                lock (_recordLock)
                {

                    isRecording = false;
                    _recordStream?.Flush(true);
                    _recordStream?.Dispose();
                    _recordStream = null;

                }

                //ConvertToMp4(OutputFile + ".ts", OutputFile + ".mp4");


            }

        }

        private async void ConvertToMp4(string inputPath, string outputPath)
        {
            var a = await ConvertTsToMp4Async(inputPath, outputPath);

            Console.WriteLine($"[CameraRTSP2] Recording stopped → {a}");

            System.IO.File.Delete(inputPath);

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



        /// <summary>
        /// Stop la lecture du flux video
        /// </summary>
        public void Stop()
        {
            try
            {
                _cts?.Cancel();
                _cts2?.Cancel();

                if (_readerTask != null && !_readerTask.Wait(2000))
                {
                    foreach (var p in Process.GetProcessesByName("ffmpeg"))
                        p.Kill();
                }
            }
            catch { }
            finally
            {
                _cts?.Cancel();
                _cts2?.Dispose();
                _readerTask = null;
            }
        }

        /// <summary>
        /// Libere la mémoire
        /// </summary>
        public void Dispose()
        {
            StopRecording();
            Stop();
            MediaPlayer?.Dispose();
            _libVLC?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
