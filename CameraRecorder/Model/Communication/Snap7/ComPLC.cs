using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Sharp7;
using System.Numerics;
using System.ComponentModel;
using CameraRecorder.Model;
using CameraRecorder.Model.Communication;

namespace CameraRecorder.Model.Communication.Snap7
{
    public class ComPLC : ICom, INotifyPropertyChanged
    {
        public int DBNumber { get; set; }
        public string IP { get; set; }

        public event Action<string, uint> OnRecord;
        public event Action StopRecord;
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _isRecording = false;

        private bool connected = false;

        public bool Connected
        {
            get => connected;
            private set { connected = value; OnPropertyChanged(nameof(Connected)); }
        }

        public ComPLC(string ip, int dbNumber)
        {
            DBNumber = dbNumber;
            IP = ip;

        }

        private void OnPropertyChanged(string v)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(v));
            }
        }

        public void StopCom()
        {
            Connect = false;
            Connected = false;
        }

        private bool Connect;
        public void StartCom()
        {
            Connect = true;
            Connected = false;

            Task.Run(() =>
            {
                while (Connect)
                {

                    //Connection au PLC
                    var client = new S7Client();
                    int connectionResult = client.ConnectTo(IP, 0, 1);

                    if (client.Connected)
                    {
                        LogsManager.Add(EnumCategory.Info, "PLC", $"PLC communication connected to IP:{IP} - DB{DBNumber}  ");
                        Connected = true;
                    }
                    //Connection établie
                    while (client.Connected && connected)
                    {
                        try
                        {

                            //buffer read
                            byte[] bufferRead = new byte[260];
                            //Lecture du DB du PLC
                            client.DBRead(DBNumber, 0, bufferRead.Length, bufferRead);

                            var timebuffer = bufferRead.GetUIntAt(2);
                            var fileName = bufferRead.GetStringAt(4);

                            if (_isRecording == false && bufferRead.GetBitAt(0, 0))
                            {

                                OnRecord?.Invoke(fileName, timebuffer);
                                _isRecording = true;
                            }
                            else if (_isRecording == true && !bufferRead.GetBitAt(0, 0))
                            {
                                StopRecord?.Invoke();
                                _isRecording = false;

                            }


                            // buffer write
                            byte[] bufferWrite = new byte[2];

                            bufferWrite.SetBitAt(0, 0, _isRecording);

                            //Ecriture dans le DB du PLC
                            client.DBWrite(DBNumber, 260, bufferWrite.Length, bufferWrite);
                        }

                        catch (Exception ex)
                        {
                            Console.WriteLine("Execption :{0} ", ex);
                            LogsManager.Add(EnumCategory.Error, "PLC", $"Exception during PLC communication: {ex.Message}");
                        }
                        finally
                        {

                        }

                    }
                    Connected = false;
                    client.Disconnect();
                    LogsManager.Add(EnumCategory.Error, "PLC", $"PLC communication disconnected");

                    Task.Delay(2000).Wait();
                }
            }

            );
        }

    }
}
