using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Globalization;
using System.Reflection;

namespace CameraRecorder.Model.Communication.EthernetIP
{

    public class LodgeEthernetIP : ICom
    {

        public EthernetIP eip;

        public event Action<string, uint> OnRecord;
        public event Action StopRecord;


        public LodgeEthernetIP()
        {

            eip = new EthernetIP("192.32.98.50", "1.0", "Recorder_Out", "Recorder_In");
        

            eip.AddSignalInputBool("StartRecorder", "", 0, 0);
            eip.AddSignalInputUInt("TimeBuffer", "", 3);


            //Evennement changement d'etat de la connection Modbus
            eip.EthernetIPStateEventArgs += (o, e) => { connected = e.State == StateConnect.Connected ? true : false; };

            //Inputs = eip.Inputs;
            //Outputs = eip.Outputs;

            //Bit de vie communication
            eip["IN.StartRecorder"].SignalChanged += (s) =>
            {
                if (s.Value == true)
                {
                    var f = eip["IN.FileName"].Value;
                    var b = eip["IN.TimeBuffer"].Value;

                    OnRecord?.Invoke(f, b);

                    eip["OUT.Recorder"].Value = true;
                }
                else
                {
                    eip["OUT.Recorder"].Value = false;
                }

            };
        }



        public string IP
        {
            get
            {
                return eip?.IPadressPlc;
            }
            set
            {
                if (eip != null)
                {
                    eip.IPadressPlc = value;
                }
            }
        }

        ///// <summary>
        ///// Signaux d'entrées
        ///// </summary>
        //public ThreadedBindingList<Signal> Inputs { get; set; }

        ///// <summary>
        ///// Signaux de sorties
        ///// </summary>
        //public ThreadedBindingList<Signal> Outputs { get; set; }


        public void StopCom()
        {
            eip?.Stop();
        }

        public void StartCom()
        {
            eip?.Start();
        }

        public bool OfflineMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private bool connected = false;
        public bool Connected => connected;
    }
}
