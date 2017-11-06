using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Threading;
using System.Net.NetworkInformation;

namespace EarthViewWallpaper
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _saveDir;
        private FileMessage _fileDown = null;

        public MainWindow()
        {
            InitializeComponent();
            _saveDir = AppDomain.CurrentDomain.BaseDirectory + "download\\";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _getOne.IsEnabled = false;

            this.lblMessage.Text = "尝试连接...";
            Ping pingSender = new Ping();
            PingReply reply = pingSender.Send("earthview.withgoogle.com",200);//第一个参数为ip地址，第二个参数为ping的时间
            if (reply.Status == IPStatus.Success)
            {
                string fileName = string.Format("{0}.jpg", (new Random()).Next(1001, 3000));
                string imageUrl = string.Format(@"https://earthview.withgoogle.com/download/{0}", fileName);

                HttpWebRequest req = null;
                HttpWebResponse res = null;
                try
                {
                    req = (HttpWebRequest)WebRequest.CreateDefault(new Uri(imageUrl));
                    res = (HttpWebResponse)req.GetResponse();
                    req.Method = "HEAD";
                    req.Timeout = 100;
                    if (res.StatusCode != HttpStatusCode.OK)
                    {
                        _getOne.IsEnabled = true;
                        this.lblMessage.Text = res.StatusCode.ToString();
                        return;
                    }
                }
                catch (WebException ex)
                {
                    this.lblMessage.Text = (ex.ToString().Substring(0, ex.ToString().IndexOf('\r') - 1));
                    if (res != null) res.Close();
                    else
                    {
                        _getOne.IsEnabled = true;
                        return;
                    }
                }

                downloadImg(imageUrl);
            }
            else
            {
                this.lblMessage.Text = "无法连接...";
                _getOne.IsEnabled = true;
            }
        }

        private void downloadImg(string imageUrl)
        {

            this.lblMessage.Text = "开始下载...";
            string fileExt = Path.GetExtension(imageUrl);
            string fileNewName = Guid.NewGuid() + fileExt;
            bool isDownLoad = false;
            string filePath = Path.Combine(_saveDir, fileNewName);
            if (File.Exists(filePath))
            {
                isDownLoad = true;
            }
            var file = new FileMessage
            {
                FileName = fileNewName,
                Url = imageUrl,
                IsDownLoad = isDownLoad,
                SavePath = filePath
            };
            _fileDown = file;

            if (!file.IsDownLoad)
            {
                string fileDirPath = Path.GetDirectoryName(file.SavePath);
                if (!Directory.Exists(fileDirPath))
                {
                    Directory.CreateDirectory(fileDirPath);
                }
                try
                {
                    WebClient client = new WebClient();
                    client.DownloadFileCompleted += Client_DownloadFileCompleted;
                    client.DownloadProgressChanged += client_DownloadProgressChanged;
                    client.DownloadFileAsync(new Uri(file.Url), file.SavePath, file.FileName);
                }
                catch
                {

                }

            }
        }


        private void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.UserState != null && _fileDown!=null)
            {
                this.lblMessage.Text = e.UserState.ToString() + ",下载完成";
                _img.Source = new BitmapImage(new Uri(_fileDown.SavePath, UriKind.Absolute));
                setWallpaperApi(_fileDown.SavePath);
            }
            _getOne.IsEnabled = true;
        }

        //利用系统的用户接口设置壁纸
        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo")]
        public static extern int SystemParametersInfo(
                int uAction,
                int uParam,
                string lpvParam,
                int fuWinIni
                );
        public static void setWallpaperApi(string strSavePath)
        {
            SystemParametersInfo(20, 1, strSavePath, 1);
        }

        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.proBarDownLoad.Minimum = 0;
            this.proBarDownLoad.Maximum = (int)e.TotalBytesToReceive;
            this.proBarDownLoad.Value = (int)e.BytesReceived;
            this.lblMessage.Text = e.ProgressPercentage + "%";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(_saveDir))
            {
                Directory.CreateDirectory(_saveDir);
            }
        }

        private void _getBing_Click(object sender, RoutedEventArgs e)
        {
            string xmlDoc = string.Empty;
            try
            {
                HttpWebRequest oHttp_Web_Req = (HttpWebRequest)WebRequest.Create("https://cn.bing.com/HPImageArchive.aspx?idx=0&n=1");
                Stream oStream = oHttp_Web_Req.GetResponse().GetResponseStream();
                using (StreamReader respStreamReader = new StreamReader(oStream, Encoding.UTF8))
                {
                    string line = string.Empty;
                    while ((line = respStreamReader.ReadLine()) != null)
                    {
                        xmlDoc += line;
                    }
                }
            }
            catch (Exception ex)
            {

            }

            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("<Url>(?<MyUrl>.*?)</Url>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            System.Text.RegularExpressions.MatchCollection collection = regex.Matches(xmlDoc);
            // 取得匹配项列表
            string ImageUrl = "http://www.bing.com" + collection[0].Groups["MyUrl"].Value;
            if (true)
            {
                ImageUrl = ImageUrl.Replace("1366x768", "1920x1080");
            }

            downloadImg(ImageUrl);
        }
    }

    internal class FileMessage
    {
        public string FileName { get; set; }
        public bool IsDownLoad { get; set; }
        public string RelativeUrl { get; set; }
        public string SavePath { get; set; }
        public string Url { get; set; }
    }
}
