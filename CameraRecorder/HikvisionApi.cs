using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 using System.Net.Http;
 using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Windows.Media.Media3D;
using System.Net;
using System.IO;
using System.Net;
namespace CameraRecorder
{

    public class HikvisionApi
    {
        private readonly HttpClient client;

        public HikvisionApi(string ip, string username, string password)
        {
            client = new HttpClient();
            client.BaseAddress = new Uri($"http://{ip}/");

            var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));


            GetInfo();

        }

        public async Task<string> GetStatusAsync()
        {
            var response = await client.GetAsync("ISAPI/System/status");
            var content = await response.Content.ReadAsStringAsync();
            Console.Write(response.Content);

            if (!response.IsSuccessStatusCode)
            {
               
                throw new Exception($"Erreur {response.StatusCode}: {content}");
            }

            return content;
        }

        public async Task<string> GetInfo()
        {
            
            var response = await client.GetAsync("http://192.32.98.120/ISAPI/System/deviceInfo");
            var content = await response.Content.ReadAsStringAsync();
            Console.Write(content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Erreur {response.StatusCode}: {content}");
            }

            return content;
        }


        public HikvisionApi(string username, string password)
        {
            client = new HttpClient();
            var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }

        public async Task StartRecordingAsync()
        {
            //var content = new StringContent("<ManualRecord><enabled>true</enabled></ManualRecord>", Encoding.UTF8, "application/xml");
            var content = new StringContent("");
            var response = await client.PostAsync("http://192.32.98.120/ISAPI/ContentMgmt/record/control/manual/start/tracks/101",content);
            response.EnsureSuccessStatusCode();

            string result = await response.Content.ReadAsStringAsync();

            Console.WriteLine(result);
        }

        public async Task StopRecordingAsync()
        {
            var content = new StringContent("<ManualRecord><enabled>false</enabled></ManualRecord>", Encoding.UTF8, "application/xml");
            var response = await client.PostAsync("http://192.32.98.120/ISAPI/ContentMgmt/record/control/manual/stop/tracks/101", content);
            response.EnsureSuccessStatusCode();
        }

        public async Task DownloadRecordAsync()
        {
            WebClient  client = new WebClient();
        
            client.Credentials = new NetworkCredential("admin", "@HIKVISION");
            string url = $"http://192.32.98.120/ISAPI/ContentMgmt/download?channel=101&starttime=2025-09-22T10:00:00Z&endtime=2025-09-22T10:05:00Z";
            string outputFile = "video.mp4";
            client.DownloadFile(url, outputFile);
            Console.WriteLine("Vidéo téléchargée avec succès : " + outputFile);
        }

    }
}
