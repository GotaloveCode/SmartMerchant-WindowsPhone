using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Views;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Resources;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml.Media.Imaging;
using ZXing;
using System;

namespace SmartMerchant.ViewModel
{
    public class QRViewModel : ViewModelBase,INavigable
    {
        //Properties
        #region
        protected INavigationService _navigationService;
        public ICommand GenerateCommand { get; set; }
        public ICommand PrintCommand { get; set; }       

        public WriteableBitmap QRImage { get { return _qrImage; } set { Set(() => QRImage, ref _qrImage, value); } }
        WriteableBitmap _qrImage;
        public string Phone { get { return phoneNumber; } set { phoneNumber = value; } }
        string phoneNumber = "250", _pin;
        public string Pin
        {
            get { return _pin = string.Format("{0}{1}{2}{3}", Pin1, Pin2, Pin3, Pin4); }
            set { _pin = value; RaisePropertyChanged("Pin"); }
        }
        public string Pin1 { get; set; }
        public string Pin2 { get; set; }
        public string Pin3 { get; set; }
        public string Pin4 { get; set; }
        private bool _IsBusy = false;
        public bool IsBusy
        {
            get { return _IsBusy; }
            set { Set(() => IsBusy, ref _IsBusy, value); }
        }
        bool _HasQR = false;
        public bool HasQR
        {
            get { return _HasQR; }
            set { Set(() => HasQR, ref _HasQR, value); }
        }
        JsonObject input = new JsonObject();
        JObject output;
        HttpStatusCode statuscode;
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        ResourceLoader res = ResourceLoader.GetForCurrentView();
        #endregion

        public QRViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            if (!IsInDesignMode)
            {
                GenerateCommand = new RelayCommand(async () => await Generate());               
                PrintCommand = new RelayCommand(async () => await PrintQR());
            }
        }

        private Task PrintQR()
        {
            throw new NotImplementedException();
        }

        private async Task Generate()
        {
            ErrorBucket errors = new ErrorBucket();
            IsBusy = true;
            // validate...
            ValidateGenerate(errors);

            // ok?
            if (!(errors.HasErrors))
            {
                if (UIHelper.HasInternetConnection())
                    await GenerateAsync();
                else
                    await UIHelper.ShowAlert(res.GetString("NoInternet"));
            }
            // errors?
            if (errors.HasErrors)
                await UIHelper.ShowAlert(errors.GetErrorsAsString());

            IsBusy = false;

        }

        private async Task GenerateAsync()
        {
            await UIHelper.ToggleProgressBar(true, res.GetString("Loading"));

            string MyParam = string.Format("?phone_number={0}&password={1}", Phone , Pin);

            var MyResult = await Rest.GetAsync("user-cards/feature", MyParam);

            statuscode = MyResult.Key;

            output = MyResult.Value;


            if (statuscode == HttpStatusCode.OK)
            {
                if ((string)output["status"] == "success")
                {
                    QRImage = GetBitmap((string)output["data"]);
                    HasQR = true;
                }
                else
                {
                    await UIHelper.ShowAlert((string)output["error"]["message"][0]);
                }
            }
            else
            {
                await UIHelper.ShowAlert((string)output["error"]["message"][0]);
            }
            await UIHelper.ToggleProgressBar(false);
            input.Clear();
        }

        private WriteableBitmap GetBitmap(string QRstring)
        {
            IBarcodeWriter writer = new BarcodeWriter
            {   //Mentioning type of bar code generation   
                Format = BarcodeFormat.QR_CODE,
                Options = new ZXing.Common.EncodingOptions
                {
                    Height = 240,
                    Width = 240
                },
                Renderer = new ZXing.Rendering.PixelDataRenderer()
                {
                    Foreground = Colors.Black,
                    Background = Colors.Transparent
                }

            };
            var result = writer.Write(QRstring);
            return result.ToBitmap() as WriteableBitmap;
        }

        //validate Generate
        private void ValidateGenerate(ErrorBucket errors)
        {

            if (string.IsNullOrEmpty(Phone) || Phone.Length < 12)
                errors.AddError(res.GetString("InvalidPhone"));

            if (string.IsNullOrEmpty(Pin))
                errors.AddError(res.GetString("RequiredPIN"));

            if (!string.IsNullOrEmpty(Pin) && Pin.Length<4)
                errors.AddError(res.GetString("ValidPIN"));

        }

        public void Activate(object parameter)
        {
            if (parameter is string)
                Phone = parameter.ToString();
        }

        public void Deactivate(object parameter)
        {
        }
    }
}