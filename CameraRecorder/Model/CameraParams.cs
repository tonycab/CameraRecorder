using System;
using System.Collections.Generic;
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
    public class CameraParams : INotifyPropertyChanged, IDataErrorInfo
    {

        private bool isValid;
        private bool viewEnable;
        private string name;
        private string adresseIP;
        private string user;
        private string password;
        private string url;
        private int bufferSizeSeconds;

        public bool IsValid
        {
            get => isValid;
            set
            {
                if (isValid != value)
                {
                    isValid = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool ViewEnable
        {
            get => viewEnable;
            set
            {
                if (viewEnable != value)
                {
                    viewEnable = value;
                    OnPropertyChanged();
                }
            }
        }


        public string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string AdresseIP
        {
            get => adresseIP;
            set
            {
                if (adresseIP != value)
                {
                    adresseIP = value;
                    OnPropertyChanged();
                }
            }
        }

        public string User
        {
            get => user;
            set
            {
                if (user != value)
                {
                    user = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Password
        {
            get => password;
            set
            {
                if (password != value)
                {
                    password = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Url
        {
            get => url;
            set
            {
                if (url != value)
                {
                    url = value;
                    OnPropertyChanged();
                }
            }
        }

        public int BufferSizeSeconds
        {
            get => bufferSizeSeconds;
            set
            {
                if (bufferSizeSeconds != value)
                {
                    bufferSizeSeconds = value;
                    OnPropertyChanged();
                }
            }
        }

        public CameraParams() { }

        /// <summary>
        /// Sérialise cette caméra en XElement
        /// </summary>
        public XElement ToXml()
        {
            return new XElement(nameof(CameraParams),
                new XElement("IsValid", IsValid),
                new XElement("ViewEnable", ViewEnable),
                new XElement("Name", Name ?? ""),
                new XElement("AdresseIP", AdresseIP ?? ""),
                new XElement("User", User ?? ""),
                new XElement("Password", Password ?? ""),
                new XElement("Url", Url ?? ""),
                new XElement("TimePreEnregistrement", BufferSizeSeconds)
            );
        }

        /// <summary>
        /// Reconstruit une caméra depuis un XElement
        /// </summary>
        public static CameraParams FromXml(XElement element)
        {
            return new CameraParams
            {
                IsValid = bool.TryParse(element.Element("IsValid")?.Value, out var u) ? u : false,
                ViewEnable = bool.TryParse(element.Element("ViewEnable")?.Value, out var v) ? v : false,
                Name = element.Element("Name")?.Value,
                AdresseIP = element.Element("AdresseIP")?.Value,
                User = element.Element("User")?.Value,
                Password = element.Element("Password")?.Value,
                Url = element.Element("Url")?.Value,
                BufferSizeSeconds = int.TryParse(element.Element("TimePreEnregistrement")?.Value, out var t) ? t : 0
            };
        }



        // --- Validation (inchangée) ---
        public string Error => null;
        public string this[string columnName]
        {

            get
            {
                return columnName switch
                {
                    nameof(AdresseIP) => !IPAddress.TryParse(AdresseIP, out _) ? "Adresse IP invalide." : null,
                    nameof(BufferSizeSeconds) => BufferSizeSeconds < 0 || BufferSizeSeconds > 300 ? "Invalide 0 - 300" : null,
                    _ => null
                };
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
