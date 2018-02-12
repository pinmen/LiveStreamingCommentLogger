using Codeplex.Data;
using Microsoft.Win32;
using mshtml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Settings
{
    public static class UserSetting
    {
        public static Dictionary<string, string> Settings { get; set; }
        static UserSetting()
        {
            Settings = new Dictionary<string, string>();
            Settings.Add("APIKey", "AIzaSyAIcDYvn6g2tRfclfEWbhlri_S9r7XAoL0");
        }
    }
}

namespace YoutubeLiveCommentViewer
{

    public class DelegateCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;
        public Func<object, bool> CanExecuteFunc { get; set; } = param => true;
        public Action<object> ExecuteAction { get; set; }
        public bool CanExecute(object parameter)
        {
            Debug.Assert(CanExecuteFunc != null);
            return CanExecuteFunc(parameter);
        }
        public void Execute(object parameter)
        {
            Debug.Assert(ExecuteAction != null);
            ExecuteAction(parameter);
        }
        public void UpdateCanExecuteChanged()
        {
            CanExecuteChanged(this, EventArgs.Empty);
        }
    }

    public class Comment
    {
        public string Content { get; set; }
        public string ImageURI { get; set; }
        public string PostedDate { get; set; }
        public string AuthorName { get; set; }
    }
    public class CommentList
    {
        public ObservableCollection<Comment> Comments { get; set; }

        public CommentList()
        {
            Comments = new ObservableCollection<Comment>();
            Comments.Add(new Comment { AuthorName = "pint", Content = "テスト", ImageURI = "https://yt3.ggpht.com/-9qajmBzbniU/AAAAAAAAAAI/AAAAAAAAAAA/u75Y-kgDKdg/s32-c-k-no-mo-rj-c0xffffff/photo.jpg", PostedDate = "今日" });
            Comments.Add(new Comment { AuthorName = "pint", Content = "テスト", ImageURI = "https://yt3.ggpht.com/-9qajmBzbniU/AAAAAAAAAAI/AAAAAAAAAAA/u75Y-kgDKdg/s32-c-k-no-mo-rj-c0xffffff/photo.jpg", PostedDate = "明日" });
        }
    }

    public class Channel
    {
        public string Id { get; set; }
        public string ImageURI { get; set; }
        public string Name { get; set; }
    }
    public class ChannelList
    {
        public ObservableCollection<Channel> Channels { get; set; }
        public ChannelList()
        {
            Channels = new ObservableCollection<Channel>();
            Channels.Add(new Channel { ImageURI = "https://yt3.ggpht.com/-2Q1Ypr7CxB4/AAAAAAAAAAI/AAAAAAAAAAA/55OaUx4wNPc/s88-c-k-no-mo-rj-c0xffffff/photo.jpg", Id = "UCcrhzdni-tnpoIon36E4qiQ", Name = "イルカ" });
        }
        public void RegisterChannnel(Channel channel)
        {
            Debug.Assert(CanRegisterChannel(channel));
            Channels.Add(channel);
        }

        public bool CanRegisterChannel(Channel channel)
        {
            if (channel == null) { return false; }
            foreach (var ch in Channels)
            {
                if (ch.Id == channel.Id) { return false; }
            }
            return true;
        }
    }

    public class WebBrowserVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public DelegateCommand JumpToURL { get; set; }
        public DelegateCommand AddChannel { get; set; }
        public CommentList CommentList { get; set; }
        public ChannelList ChannelList { get; set; }

        //public string ChannelURI { get { return _ChannelURI; } set { _ChannelURI = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ChannelURI))); AddChannel?.UpdateCanExecuteChanged(); }  }
        //private string _ChannelURI { get; set; }

        public string BrowsingURI { get { return _BrowsingURI; } set { _BrowsingURI = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BrowsingURI))); JumpToURL?.UpdateCanExecuteChanged(); } }
        private string _BrowsingURI { get; set; }

        public Channel BrowsingChannel { get { return _BrowsingChannel; } set { _BrowsingChannel = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BrowsingChannel))); AddChannel?.UpdateCanExecuteChanged(); } }
        public Channel _BrowsingChannel { get; set; }

        public WebBrowserVM(WebBrowser browser/*, TextBox browser_text_box, TextBox ch_uri_text_box*/)
        {
            CommentList = new CommentList();
            ChannelList = new ChannelList();

            // jump
            Regex HttpRegex = new Regex(@"^https?://");
            JumpToURL = new DelegateCommand()
            {
                ExecuteAction = (param) => {
                    Debug.Assert(JumpToURL.CanExecuteFunc(param));
                    try { browser.Navigate(BrowsingURI); }
                    catch { BrowsingURI = "Invalid URL"; }
                },
                CanExecuteFunc = (param) =>
                {
                    if (BrowsingURI == null) { return false; }
                    return BrowsingURI != null && HttpRegex.IsMatch(BrowsingURI);
                }
            };
            browser.Navigated += (sender, e) => {
                var uri = e.Uri.ToString();
                BrowsingURI = uri;
                //BrowsingChannelId = GetChannelId(uri);
                var id = GetChannelId(uri);
                var api_str = $"https://www.googleapis.com/youtube/v3/channels?part=snippet&id={id}&key={Settings.UserSetting.Settings["APIKey"]}";
                var result = new StreamReader(WebRequest.Create(api_str).GetResponse().GetResponseStream(), System.Text.Encoding.UTF8).ReadToEnd();
                dynamic info = DynamicJson.Parse(result).items[0].snippet;

                BrowsingChannel = new Channel() { Id = id, Name = info.title, ImageURI = info.thumbnails.@default.url };
            };

            // channel
            AddChannel = new DelegateCommand()
            {
                ExecuteAction = (param) =>
                {
                    Debug.Assert(AddChannel.CanExecuteFunc(param));
                    ChannelList.RegisterChannnel(BrowsingChannel);
                    AddChannel.UpdateCanExecuteChanged();
                },
                CanExecuteFunc = (param) =>
                {
                    return ChannelList.CanRegisterChannel(BrowsingChannel);
                }
            };
            

            #region old
                            //AddChannel = new DelegateCommand()
                            //{
                            //    ExecuteAction = (param) =>
                            //    {
                            //        Debug.Assert(AddChannel.CanExecuteFunc(param));
                            //        ChannelList.RegisterChannnel(GetChannelId(ChannelURI));
                            //    },
                            //    CanExecuteFunc = (param) =>
                            //    {
                            //        if (ChannelURI == null) { return false; }
                            //        return GetChannelId(ChannelURI) != null;
                            //    }
                            //};




            //JumpToURL = new DelegateCommand()
            //{
            //    ExecuteAction = (param) => {
            //        var uri = (string)param;
            //        try
            //        {
            //            browser.Navigate(uri);
            //        }
            //        catch
            //        {
            //            browser_text_box.Text = "Invalid URL";
            //        }
            //    }
            //};
            //browser.Navigated += (sender, e) => {
            //    browser_text_box.Text = e.Uri.ToString();
            //};
            //AddChannel = new DelegateCommand()
            //{
            //    ExecuteAction = (param) =>
            //    {
            //        var str = (string)param;
            //        Debug.Assert(ChannelRegex.IsMatch(str));
            //        ChannelList.RegisterChannnel(ChannelRegex.Match(str).Groups[1].Value);
            //    },
            //    CanExecuteFunc = (param) =>
            //    {
            //        var str = (string)param;
            //        return ChannelRegex.IsMatch(str);
            //    }
            //};
            //ch_uri_text_box.TextChanged += (sender, e) => AddChannel.UpdateCanExecuteChanged();
            #endregion
        }
        string GetChannelId (string uri){
            Debug.Assert(uri != null);

            Regex ChannelRegex = new Regex(@"https://www.youtube.com/channel/([\w\-]+)(?:/.*)?");
            if (ChannelRegex.IsMatch(uri))
            {
                return ChannelRegex.Match(uri).Groups[1].Value;
            }

            Regex VideoRegex = new Regex(@"https://www.youtube.com/watch\?v=([\w\-]+)(?:.*)?");
            if (VideoRegex.IsMatch(uri))
            {
                var id = VideoRegex.Match(uri).Groups[1].Value;
                var api_str = $"https://www.googleapis.com/youtube/v3/videos?part=snippet&id={id}&key={Settings.UserSetting.Settings["APIKey"]}";
                var result = new StreamReader(WebRequest.Create(api_str).GetResponse().GetResponseStream(), System.Text.Encoding.UTF8).ReadToEnd();
                try { return DynamicJson.Parse(result).items[0].snippet.channelId; }
                catch { return null; }
            }

            return null;
        }

    }

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 起動IEを11に固定
            using (var Key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true))
            {
                var ver = 11000; // 99999
                var appName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                Key.SetValue(appName+".exe", ver, RegistryValueKind.DWord);
                Key.SetValue(appName+".vshost.exe", ver, RegistryValueKind.DWord);
            }

            DataContext = new { browser = new WebBrowserVM(_Browser), settings = Settings.UserSetting.Settings };

            this.Loaded += (obj, e) =>
            {
                var addr = @"https://www.youtube.com/";
                addr = @"https://www.youtube.com/channel/UC4R8DWoMoI7CAwX8_LjQHig";
                _Browser.Navigate(addr);
            };

            _Browser.Navigated += (obj, e) =>
            {
                Debug.WriteLine("test");

                var browser = obj as WebBrowser;

                var main = browser.Document as mshtml.IHTMLDocument3;

                main.getElementsByTagName("yt-live-chat-text-message-renderer");

                //IHTMLDocument3
            };


        }
    }

#if DEBUG
    public static class DebugMgr
    {
        public static DelegateCommand TryPraseComment { get; set; }

        static DebugMgr()
        {

        }

    }
#else
    public static class DebugMgr
    {
    }
#endif
}
