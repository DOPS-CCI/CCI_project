using System;
using System.IO;
using System.Net;
using System.Text;

namespace CCIUtilities
{
    public class Log
    {
        static readonly Uri ftpFile = new Uri("ftp://zoomlenz.net/log.txt");
        static readonly NetworkCredential cred = new NetworkCredential(Properties.Settings.Default.un, Properties.Settings.Default.pw);

        public static void writeToLog(string message)
        {

            // Get the object used to communicate with the server.
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpFile);
                request.Method = WebRequestMethods.Ftp.AppendFile;
                DateTime now = DateTime.Now;
                string nowString = now.ToString("F");
                string fullMessage = nowString + " " + Environment.MachineName + "(" + Environment.UserName + "): " + message + Environment.NewLine;
                byte[] buffer;
                buffer = new byte[fullMessage.Length];
                int i = 0;
                foreach (char c in fullMessage) buffer[i++] = (byte)c;
                request.ContentLength = buffer.Length;

                request.Credentials = cred;
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(buffer, 0, buffer.Length);
                requestStream.Close();
            }
            catch
            {
                return;//ignore errors; thus, off network OK
            }
        }
    }
}
