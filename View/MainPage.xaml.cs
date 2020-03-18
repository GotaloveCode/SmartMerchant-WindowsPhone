using Lumia.Imaging;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using VideoEffects;
using Windows.ApplicationModel.Resources;
using Windows.Data.Json;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using ZXing;
using ZXing.Common;

namespace SmartMerchant
{
    public sealed partial class MainPage : BindablePage
    {
        public string ScannedCard { get; set; }
        ResourceLoader res = ResourceLoader.GetForCurrentView();
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        DisplayRequest m_displayRequest = new DisplayRequest();
        MediaCapture m_capture;
        ContinuousAutoFocus m_autoFocus;
        bool m_initializing;
        volatile bool m_snapRequested;
        BarcodeReader m_reader = new BarcodeReader
        {
            Options = new DecodingOptions
            {
                PossibleFormats = new BarcodeFormat[] { BarcodeFormat.QR_CODE },
                TryHarder = true
            }
        };        
        

        public MainPage()
        {
            InitializeComponent();
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ScannedCard = "";
            // Prevent screen timeout
            m_displayRequest.RequestActive();
            Application.Current.Suspending += App_Suspending;
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            Application.Current.Suspending -= App_Suspending;
            Window.Current.VisibilityChanged -= Current_VisibilityChanged;

            m_displayRequest.RequestRelease();

            await DisposeCaptureAsync();
        }

        private void btnScan_Click(object sender, RoutedEventArgs e)
        {
            // Disable app UI rotation
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            //hide elements on page
            MyContent.Visibility = Visibility.Collapsed;
            //show captureelement
            MyCanvas.Visibility = Visibility.Visible;
            var ignore = InitializeCaptureAsync();
        }




        private void App_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            if (MyCanvas.Visibility == Visibility.Visible)
            {
                // Dispatch call to the UI thread since the event may get fired on some other thread
                var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await DisposeCaptureAsync();
                });
            }
        }

        private async void Current_VisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            if (!e.Visible)
            {
                await DisposeCaptureAsync();
            }
        }

        // Must be called on the UI thread
        private async Task InitializeCaptureAsync()
        {
            if (m_initializing || (m_capture != null))
            {
                return;
            }
            m_initializing = true;

            try
            {
                var settings = new MediaCaptureInitializationSettings
                {
                    VideoDeviceId = await GetBackOrDefaulCameraIdAsync(),
                    StreamingCaptureMode = StreamingCaptureMode.Video
                };

                var capture = new MediaCapture();
                await capture.InitializeAsync(settings);

                // Select the capture resolution closest to screen resolution
                var formats = capture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview);
                var format = (VideoEncodingProperties)formats.OrderBy((item) =>
                {
                    var props = (VideoEncodingProperties)item;
                    return Math.Abs(props.Width - this.ActualWidth) + Math.Abs(props.Height - this.ActualHeight);
                }).First();
                await capture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, format);

                // Make the preview full screen
                var scale = Math.Min(this.ActualWidth / format.Width, this.ActualHeight / format.Height);
                Preview.Width = format.Width;
                Preview.Height = format.Height;
                Preview.RenderTransformOrigin = new Point(.5, .5);
                Preview.RenderTransform = new ScaleTransform { ScaleX = scale, ScaleY = scale };
                //BarcodeOutline.Width = format.Width;
                //BarcodeOutline.Height = format.Height;
                //BarcodeOutline.RenderTransformOrigin = new Point(.5, .5);
                //BarcodeOutline.RenderTransform = new ScaleTransform { ScaleX = scale, ScaleY = scale };

                // Enable QR code detection
                var definition = new LumiaAnalyzerDefinition(ColorMode.Yuv420Sp, 640, AnalyzeBitmap);
                await capture.AddEffectAsync(MediaStreamType.VideoPreview, definition.ActivatableClassId, definition.Properties);

                // Start preview
                //m_time.Restart();
                Preview.Source = capture;
                await capture.StartPreviewAsync();

                capture.Failed += capture_Failed;

                m_autoFocus = await ContinuousAutoFocus.StartAsync(capture.VideoDeviceController.FocusControl);

                m_capture = capture;
            }
            catch (Exception e)
            {
                MessageDialog dialog = new MessageDialog(string.Format("Failed to start the camera: {0}", e.Message));
                await dialog.ShowAsync();

            }

            m_initializing = false;
        }

        private void AnalyzeBitmap(Bitmap bitmap, TimeSpan time)
        {
            if (m_snapRequested)
            {
                m_snapRequested = false;

                IBuffer jpegBuffer = (new JpegRenderer(new BitmapImageSource(bitmap))).RenderAsync().AsTask().Result;
                var folder = ApplicationData.Current.LocalFolder;
                var jpegFile = folder.CreateFileAsync("QrCodeSnap.jpg", CreationCollisionOption.ReplaceExisting).AsTask().Result;
                FileIO.WriteBufferAsync(jpegFile, jpegBuffer).AsTask().Wait();
            }

            // Log.Events.QrCodeDecodeStart();

            Result result = m_reader.Decode(
                bitmap.Buffers[0].Buffer.ToArray(),
                (int)bitmap.Buffers[0].Pitch, // Should be width here but I haven't found a way to pass both width and stride to ZXing yet
                (int)bitmap.Dimensions.Height,
                BitmapFormat.Gray8
                );

            // Log.Events.QrCodeDecodeStop(result != null);

            var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                    //var elapsedTimeInMS = m_time.ElapsedMilliseconds;
                    //m_time.Restart();

                    if (result == null)
                {
                        //TextLog.Text = string.Format("[{0,4}ms] No barcode", elapsedTimeInMS);

                        //if (m_autoFocus != null)
                        //{
                        //    m_autoFocus.BarcodeFound = false;
                        //}

                        //BarcodeOutline.Points.Clear();
                    }
                else
                {
                        //TextLog.Text = result.Text

                        ScannedCard = result.Text;

                    if (!string.IsNullOrEmpty(ScannedCard))
                    {
                        btnSend.Visibility = Visibility.Visible;
                        btnScan.Visibility = Visibility.Collapsed;
                        string[] CardArray = ScannedCard.Split('*');
                        if (CardArray[2].Length == 4)
                        {
                            Scanned.Visibility = Visibility.Visible;
                            ScannedCard = CardArray[1];
                        }
                        else
                        {
                            ScannedCard = "";
                            await UIHelper.ShowAlert("Invalid QR Code");
                        }

                    }
                    if (m_autoFocus != null)
                    {
                        m_autoFocus.BarcodeFound = true;
                    }

                        //BarcodeOutline.Points.Clear();

                        //for (int n = 0; n < result.ResultPoints.Length; n++)
                        //{
                        //    BarcodeOutline.Points.Add(new Point(
                        //        result.ResultPoints[n].X * (BarcodeOutline.Width / bitmap.Dimensions.Width),
                        //        result.ResultPoints[n].Y * (BarcodeOutline.Height / bitmap.Dimensions.Height)
                        //        ));
                        //}
                        //cleanup
                        //m_time.Stop();
                        Preview.Source = null;
                    MyCanvas.Visibility = Visibility.Collapsed;
                    MyContent.Visibility = Visibility.Visible;
                    DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        await DisposeCaptureAsync();

                    });

                }
            });
        }

        public static async Task<string> GetBackOrDefaulCameraIdAsync()
        {
            var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            string deviceId = "";

            foreach (var device in devices)
            {
                if ((device.EnclosureLocation != null) &&
                    (device.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back))
                {
                    deviceId = device.Id;
                    break;
                }
            }

            return deviceId;
        }

        // Must be called on the UI thread
        private async Task DisposeCaptureAsync()
        {
            Preview.Source = null;

            if (m_autoFocus != null)
            {
                m_autoFocus.Dispose();
                m_autoFocus = null;
            }

            MediaCapture capture;
            lock (this)
            {
                capture = m_capture;
                m_capture = null;
            }

            if (capture != null)
            {
                capture.Failed -= capture_Failed;

                await capture.StopPreviewAsync();

                capture.Dispose();
            }
        }

        void capture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            // Dispatch call to the UI thread since the event may get fired on some other thread
            var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await DisposeCaptureAsync();
            });
        }

        //make payment request
        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (!UIHelper.HasInternetConnection())
            {
                //no internet
                await UIHelper.ShowAlert(res.GetString("CheckInternet"), res.GetString("NoInternet"));
            }
            else
            {
                if (ScannedCard.Length > 0 && txtAmount.Text.Length > 0)
                {
                    await UIHelper.ToggleProgressBar(true, res.GetString("Communicating"));
                    await PostAsync();
                    await UIHelper.ToggleProgressBar(false);
                }
                else
                {
                    if (txtAmount.Text.Length < 1)
                    {
                        await UIHelper.ShowAlert(res.GetString("Enter/Text"));
                    }
                    if (ScannedCard.Length < 1)
                    {
                        await UIHelper.ShowAlert(res.GetString("Rescan"));
                    }

                }
            }
            btnScan.Visibility = Visibility.Visible;
            btnSend.Visibility = Visibility.Collapsed;
        }

        //actual server call to request payment
        private async Task PostAsync()
        {
            if (UIHelper.HasInternetConnection())
            {
                JsonObject input = new JsonObject();
                Debug.WriteLine(ScannedCard);
                input.Add("user_token", ScannedCard);
                input.Add("amount", txtAmount.Text);

                var MyResult = await Rest.PostAsync("charge-card", input.Stringify());

                HttpStatusCode statuscode = MyResult.Key;

                JObject output = MyResult.Value;

                Debug.WriteLine(statuscode);

                await UIHelper.ToggleProgressBar(false);
                if (statuscode == HttpStatusCode.OK)
                {
                    await UIHelper.ShowAlert((string)output["message"]);
                }
                else
                {
                    string error = (string)output["error"]["message"][0];
                    await UIHelper.ShowAlert(error);
                }
               
            }
            else
            {
                await UIHelper.ShowAlert(res.GetString("NoInternet"));
            }


        }

        private void Grid_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            FrameworkElement s = sender as FrameworkElement;
            Flyout.ShowAttachedFlyout(s);
        }

    }
}


