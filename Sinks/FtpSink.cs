using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace FeatureNinjas.LogPack.Sinks
{
    public class FtpSink : LogPackSink
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;

        public FtpSink(string host, int port, string username, string password)
        {
            _host = host;
            _port = port;
            _username = username;
            _password = password;
        }

        public override async Task Send(string file)
        {
            var target = $"ftp://{_username}:{_password}@{_host}:{_port}/{file}";
            Console.WriteLine(target);
            var request = (FtpWebRequest)WebRequest.Create(target);
            request.UsePassive = true;
            request.Method = WebRequestMethods.Ftp.UploadFile;

            using var fileStream = File.OpenRead(file);
            using var ftpStream = request.GetRequestStream();
            await fileStream.CopyToAsync(ftpStream);
        }
    }
}