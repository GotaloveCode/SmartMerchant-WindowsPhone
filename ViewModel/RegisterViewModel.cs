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

namespace SmartMerchant.ViewModel
{
    public class RegisterViewModel:ViewModelBase
    {
        //Properties
        #region
        protected INavigationService _navigationService;
        public ICommand RegisterCommand { get; set; }
        public ICommand VerifyCommand { get; set; }
        public string BusinessName { get; set; }
        public string Phone { get { return phoneNumber; } set { phoneNumber = value; } }
        public string Email { get; set; }
        public string VerifyEmail { get; set; }
        public string Code { get; set; }
        string phoneNumber = "250";
        public string Password { get; set; }        
        public string Confirm { get; set; }
        private bool _IsBusy = false;
        public bool IsBusy
        {
            get { return _IsBusy; }
            set {Set(() => IsBusy,ref _IsBusy, value);}
        }
        JsonObject input = new JsonObject();
        JObject output;
        HttpStatusCode statuscode;
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        ResourceLoader res = ResourceLoader.GetForCurrentView();
        #endregion

        public RegisterViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            if (!IsInDesignMode)
            {
                RegisterCommand = new RelayCommand(async () => await DoRegister());
                VerifyCommand = new RelayCommand(async () => await Verify());
            }
        }

        private async Task Verify()
        {
            IsBusy = true;

            if (string.IsNullOrEmpty(Phone) || Phone.Length < 12)
            {
                await UIHelper.ShowAlert(res.GetString("Enterphone"));

            }
            else
            {
                //check internet
                if (UIHelper.HasInternetConnection())
                {
                    await VerifyAsync();
                }
                else
                {
                    await UIHelper.ShowAlert(res.GetString("NoInternet"));
                }
            }
            IsBusy = false;

        }

        private async Task VerifyAsync()
        {
            await UIHelper.ToggleProgressBar(true, res.GetString("Loading"));

            input.Add("phone_number", Phone);

            var MyResult = await Rest.PostAsync("phone-verify", input.Stringify());

            statuscode = MyResult.Key;

            output = MyResult.Value;

            await UIHelper.ToggleProgressBar(false);

            if (statuscode == HttpStatusCode.OK)
            {
                if ((string)output["status"] != "error")
                    await UIHelper.ShowAlert((string)output["message"]);
                else
                    await UIHelper.ShowAlert((string)output["error"]["message"][0]);
            }
            else
            {
                await UIHelper.ShowAlert((string)output["error"]["message"][0]);
            }
            input.Clear();
        }

        private async Task DoRegister()
        {
            ErrorBucket errors = new ErrorBucket();
            IsBusy = true;
            // validate...
            ValidateSignUp(errors);

            // ok?
            if (!(errors.HasErrors))
            {
                if (UIHelper.HasInternetConnection())
                    await RegisterAsync();
                else
                    await UIHelper.ShowAlert(res.GetString("NoInternet"));
            }

            // errors?
            if (errors.HasErrors)
                await UIHelper.ShowAlert(errors.GetErrorsAsString());

            IsBusy = false;

        }

        private async Task RegisterAsync()
        {
            await UIHelper.ToggleProgressBar(true, res.GetString("Loading"));
            input.Add("phone_number", Phone);
            input.Add("name", BusinessName);
            input.Add("password", Password);
            input.Add("code", Code);
            input.Add("email", Email);
            input.Add("user_type", "Merchant");

            var MyResult = await Rest.PostAsync("register", input.Stringify());

            statuscode = MyResult.Key;
            output = MyResult.Value;
            if (statuscode == HttpStatusCode.OK)
            {
                if ((string)output["status"] != "error")
                {
                    localSettings.Values["Registered"] = "true";
                    localSettings.Values["Phone"] = Phone;
                   localSettings.Values["Token"] = (string)output["token"];
                    Rest.Token = (string)output["token"];
                    _navigationService.NavigateTo("Main");                    
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

        //validate register
        private void ValidateSignUp(ErrorBucket errors)
        {

            if (string.IsNullOrEmpty(BusinessName))
                errors.AddError(res.GetString("RequiredBusinessName"));

            if (string.IsNullOrEmpty(Email))
                errors.AddError(res.GetString("RequiredEmail"));

            if (string.IsNullOrEmpty(VerifyEmail))
                errors.AddError(res.GetString("RequiredEmail"));

            if (!(string.IsNullOrEmpty(Email)) && Email != VerifyEmail && !(string.IsNullOrEmpty(VerifyEmail)))
                errors.AddError(res.GetString("MatchEmail"));

            if (string.IsNullOrEmpty(Phone) || Phone.Length < 12)
                errors.AddError(res.GetString("InvalidPhone"));

            if (string.IsNullOrEmpty(Code))
                errors.AddError(res.GetString("RequiredCode"));

            if (string.IsNullOrEmpty(Password))
                errors.AddError(res.GetString("RequiredPassword"));

            if (string.IsNullOrEmpty(Confirm))
                errors.AddError(res.GetString("ConfirmPassword/Text"));

            if (!(string.IsNullOrEmpty(Password)) && Password.Length < 6)
                errors.AddError(res.GetString("ValidPassword"));

            // check the PINs...
            if (!(string.IsNullOrEmpty(Password)) && Password != Confirm && !(string.IsNullOrEmpty(Confirm)))
                errors.AddError(res.GetString("MatchPassword"));


        }



    }
}
