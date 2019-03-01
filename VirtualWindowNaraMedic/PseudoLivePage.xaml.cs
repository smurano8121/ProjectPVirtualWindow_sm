using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.System;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using System.Diagnostics;
using Windows.Web.Http;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace VirtualWindowUWP
{
    public sealed partial class PseudoLivePage : Page
    {
        // To get video library, we have to declare the function in app manifest.
        private static StorageFolder videoLibrary = KnownFolders.VideosLibrary;
        // Media element static object
        private static MediaElement videoObject1;
        private static MediaElement videoObject2;
        //天気情報を取得 && 画像透過の関数を呼び出す時間トリガー　//smurano変更部 private→public 外部からのアクセスを許可するため
        public static DispatcherTimer transmission_image;
        //透過speedの調整用
        private int transmission_speed;
        //動画の更新頻度（分）
        private int refresh_minute = 2;
        //何秒で完全に透過させるか（S）
        private int transmission_span = 1;
        //5 (s)，1(s)〇
        //private static String stockvideo_path = "\\172.20.11.71\\Stockvideo\\";
        private static String stockvideo_path = "C:\\Users\\smura\\Videos\\stockvideo\\";


        public PseudoLivePage()
        {
            this.InitializeComponent();

            Window.Current.CoreWindow.KeyDown += KeyDownHandle;
            this.Unloaded += (sender, e) =>
            {
                Window.Current.CoreWindow.KeyDown -= KeyDownHandle;
            };

            // set MediaElement into static variable
            videoObject1 = PseudoLivePlayer1;
            videoObject2 = PseudoLivePlayer2;
            this.Loaded += UpdateVideo;
    
            // 画面の透過率を変える設定 //smurano変更部 this→PseudoLivePage
            PseudoLivePage.transmission_image = new DispatcherTimer();
            PseudoLivePage.transmission_image.Interval = TimeSpan.FromSeconds(1);
            PseudoLivePage.transmission_image.Tick += TransmissionImage;
            PseudoLivePage.transmission_image.Start();
        }

        private void KeyDownHandle(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs e) //smurano追加部　擬似ライブ映像スレッドを停止する（キーボード入力）
        {
            switch (e.VirtualKey)
            {
                case Windows.System.VirtualKey.Escape:
                    Debug.WriteLine("aaa");
                    PseudoLivePage.transmission_image.Stop();
                    break;
            }
        }

        private static async void ReadVideo2(string weather,int key)
        {
            int hh = 0;
            int mm = 0;
            string fileName = "";

            DateTime now = DateTime.Now;

            //例)8,9,10,11,12→10 | 13.14,15,16,17→15 | 18,19,20,21,22→20
            int real_minute = int.Parse(now.ToString("mm"));
            /*int judge = real_minute % 10;
            if (judge == 1) { mm = real_minute - 1; }
            else if (judge == 2) { mm = real_minute - 2; }
            else if (judge == 3) { mm = real_minute + 2; }
            else if (judge == 4) { mm = real_minute + 1; }
            else if (judge == 6) { mm = real_minute - 1; }
            else if (judge == 7) { mm = real_minute - 2; }
            else if (judge == 8) { mm = real_minute + 2; }
            else if (judge == 9) { mm = real_minute + 1; }
            else { mm = real_minute;  }

            if ( mm <= 2 || 58 <= mm ) //00分のときの処理
            {
                hh = 1 + int.Parse(now.ToString("HH"));
                Debug.WriteLine(mm);
                mm = 0;
                fileName = now.ToString("MM") + "_" + hh + "0" + mm + "_" + weather + ".mp4";
            }
            else if (mm == 5) {
                hh = int.Parse(now.ToString("HH"));
                fileName = now.ToString("MM") + "_" + hh + "0" + mm + "_" + weather + ".mp4";
            }
            else
            {
                hh = int.Parse(now.ToString("HH"));
                fileName = now.ToString("MM") + "_" + hh + "" + mm + "_" + weather + ".mp4";
            }*/
            if (real_minute % 2 == 1) { mm = real_minute + 1; }
            else { mm = real_minute; }

            if (mm == 59) //00分のときの処理
            {
                hh = 1 + int.Parse(now.ToString("HH"));
                Debug.WriteLine(mm);
                mm = 0;
                fileName = now.ToString("MM") + "_" + hh + "0" + mm + "_" + weather + ".mp4";
            }
            else if (mm == 2 || mm == 4 || mm == 6 || mm == 8)
            {
                hh = int.Parse(now.ToString("HH"));
                fileName = now.ToString("MM") + "_" + hh + "0" + mm + "_" + weather + ".mp4";
            }
            else
            {
                hh = int.Parse(now.ToString("HH"));
                fileName = now.ToString("MM") + "_" + hh + "" + mm + "_" + weather + ".mp4";
            }

            Debug.WriteLine(fileName);
            Debug.WriteLine(now.ToString("MM"));
            Debug.WriteLine(weather);
            StorageFolder targetDir = await StorageFolder.GetFolderFromPathAsync(stockvideo_path + now.ToString("MM") + "\\" + weather);
            StorageFile targetVideo = await targetDir.GetFileAsync(fileName);

            var stream = await targetVideo.OpenAsync(Windows.Storage.FileAccessMode.Read);

            if (key == 0)
            {
                videoObject1.SetSource(stream, targetVideo.ContentType);
                videoObject2.SetSource(stream, targetVideo.ContentType);
            }
            else if (key == 1)
            {
                videoObject1.SetSource(stream, targetVideo.ContentType);
            }
            else if (key == 2){
                videoObject2.SetSource(stream, targetVideo.ContentType);
            }
        }

        private async void UpdateVideo(object sender, object e)
        {
            string get_icon = await TestSync();
            ReadVideo2(get_icon, 0);
        }

        private static async Task<string> TestSync()
        {
            string endpoint = string.Format("https://api.darksky.net/forecast/579137f0816593c1d256911bc1c62f0f/34.8031949,135.7787311");

            HttpClient client = new HttpClient();
            HttpResponseMessage result = await client.GetAsync(new Uri(endpoint));
            string json = await result.Content.ReadAsStringAsync();

            //jsonを解析して結果を取得する
            JObject root = JObject.Parse(json);
            string weather = root["currently"]["icon"].ToString();

            var icon = "";

            //sunny
            if (weather == "clear-day" || weather == "clear-night" )
            {
                icon = "sunny";

            }
            //cloudy
            else if (weather == "cloudy" || weather == "partly-cloudy-day" || weather == "partly-cloudy-night" || weather == "wind")
            {
                icon = "cloudy";
            }
            //rainny
            else if (weather == "rain")
            {
                icon = "rainny";
            }
            //snow
            else if (weather == "snow" || weather == "sleet")
            {
                icon = "snowy";
            }
            //foggy
            else if (weather == "fog")
            {
                icon = "foggy";
            }
            return icon;
        }

        private async void TransmissionImage(object sender, object e)
        {
            transmission_speed += 1;
            Debug.WriteLine(transmission_speed);

            //if (transmission_speed == 1)
            //{
            //    videoObject2.Opacity = 0.1 * transmission_speed * 10;
            //}
            //else if (transmission_speed == 120)
            //{
            //    videoObject2.Opacity = 0.1 * (120 - transmission_speed) * 10;
            //}

            //透過を率を変更する
            //（詳細）開始（transmission_span）秒で透過0%，（60 * refresh_minute - transmission_span）秒から透過開始
            if (transmission_speed <= transmission_span)
            {
                videoObject2.Opacity = 0.1 * transmission_speed * (10 / transmission_span);
            }
            else if (60 * refresh_minute - transmission_span <= transmission_speed && transmission_speed <= 60 * refresh_minute)
            {
                videoObject2.Opacity = 0.1 * (60 * refresh_minute - transmission_speed) * (10 / transmission_span);
            }

            //動画更新
            if (transmission_speed == transmission_span)
            {
                string get_icon = await TestSync();
                ReadVideo2(get_icon, 1);
            }
            else if (transmission_speed == 60 * refresh_minute) {
                string get_icon = await TestSync();
                ReadVideo2(get_icon,2);
                transmission_speed = 0;
            }
        }
    }
}
