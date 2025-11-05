using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraRecorder.Model.Communication
{
    public interface ICom
    {
        public event Action<string, uint> OnRecord;

        public event Action StopRecord;
        public bool Connected { get; }
        public void StopCom();
        public void StartCom();


    }
}
