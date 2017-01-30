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
    public class LoginViewModel : ViewModelBase,INavigable
    {
        protected INavigationService _navigationService;
        public ICommand SignInCommand { get; private set; }        
        private bool _IsBusy = false;
        public bool IsBusy
        {
            get { return _IsBusy; }
            set { _IsBusy = value; RaisePropertyChanged("IsBusy"); }
        }
        public string Phone { get { return phoneNumber; } set { phoneNumber = value; } }
        string phoneNumber = "250";
        public string Password { get; set; }
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;        
        ResourceLoader res = ResourceLoader.GetForCurrentView();


        public LoginViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            if (!IsInDesignMode)
                SignInCommand = new RelayCommand(async () => await DoLogin());
        }


        //login
        private async Task DoLogin()
        {
            ErrorBucket errors = new ErrorBucket();

            IsBusy = true;

            Validate(errors);

            // ok?
            if (!(errors.HasErrors))
            {
                //check internet
                if (UIHelper.HasInternetConnection())                
                    await LoginAsync();
                else
                    await UIHelper.ShowAlert(res.GetString("NoInternet"));
                
            }
            // errors?
            if (errors.HasErrors)
            {
                await UIHelper.ShowAlert(errors.GetErrorsAsString());
                errors.ClearErrors();
            }

            IsBusy = false;
        }


        private async Task LoginAsync()
        {
            JsonObject input = new JsonObject();
            input.Add("phone_number", Phone);
            input.Add("password", Password);
            input.Add("user_type", "Merchant");

            await UIHelper.ToggleProgressBar(true, res.GetString("SigningIn"));
            
            var MyResult = await Rest.PostAsync("auth", input.Stringify());
            
            HttpStatusCode statuscode = MyResult.Key;
            JObject output = MyResult.Value;
            await UIHelper.ToggleProgressBar(false);
            if (statuscode == HttpStatusCode.OK)
            {
                Password = string.Empty;
                if ((string)output["status"] != "error")
                {
                    localSettings.Values["Token"] = (string)output["token"];
                    localSettings.Values["Phone"] = Phone;
                    localSettings.Values["Registered"] = "true";
                    //set logged in removed only on explicit logout
                    localSettings.Values["LoggedIn"] = "LoggedIn";
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

            input.Clear();
        }
        
        //validate login
        private void Validate(ErrorBucket errors)
        {

            if (string.IsNullOrEmpty(Phone) || Phone.Length < 12)
                errors.AddError(res.GetString("InvalidPhone"));


            //if (string.IsNullOrEmpty(Password) || Password.Length < 6)
            //    errors.AddError(res.GetString("ValidPassword"));

        }

        public void Activate(object parameter)
        {
            if (localSettings.Values.ContainsKey("Phone"))
            {
                Phone = localSettings.Values["Phone"].ToString();
            }
            Password = string.Empty;
        }

        public void Deactivate(object parameter)
        {            
        }
    }
}
