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
    public class AddUserViewModel : ViewModelBase
    {
        //Properties
        #region
        protected INavigationService _navigationService;
        public ICommand AddUserCommand { get; set; }
        public ICommand VerifyCommand { get; set; }
        public string FirstName { get { return _firstName; } set { _firstName = value; } }
        public string LastName { get { return _lastName; } set { _lastName = value; } }
        public string FullName { get { return string.Format("{0} {1}", _firstName, _lastName); } set { _fullName = value; } }
        public string Phone { get { return phoneNumber; } set { phoneNumber = value; } }
        public string IdNo { get; set; }        
        public string Code { get; set; }
        string phoneNumber = "250", _PIN, _confirm, _firstName, _lastName, _fullName;
        public string PIN
        {
            get { return _PIN = string.Format("{0}{1}{2}{3}", PIN1, PIN2, PIN3, PIN4); }
            set { _PIN = value; RaisePropertyChanged("PIN"); }
        }
        public string PIN1 { get; set; }
        public string PIN2 { get; set; }
        public string PIN3 { get; set; }
        public string PIN4 { get; set; }
       
        public string Confirm
        {
            get { return _confirm = string.Format("{0}{1}{2}{3}", Confirm1, Confirm2, Confirm3, Confirm4); }
            set { _confirm = value; RaisePropertyChanged("Confirm"); }
        }
        public string Confirm1 { get; set; }
        public string Confirm2 { get; set; }
        public string Confirm3 { get; set; }
        public string Confirm4 { get; set; }

        private bool _IsBusy = false;
        public bool IsBusy
        {
            get { return _IsBusy; }
            set { Set(() => IsBusy, ref _IsBusy, value); }
        }
        JsonObject input = new JsonObject();
        JObject output;
        HttpStatusCode statuscode;
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        ResourceLoader res = ResourceLoader.GetForCurrentView();
        #endregion

        public AddUserViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            if (!IsInDesignMode)
            {
                AddUserCommand = new RelayCommand(async () => await AddUser());               
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

        private async Task AddUser()
        {

            ErrorBucket errors = new ErrorBucket();
            IsBusy = true;
            // validate...
            ValidateSignUp(errors);

            // ok?
            if (!(errors.HasErrors))
            {
                if (UIHelper.HasInternetConnection())
                    await AddUserAsync();
                else
                    await UIHelper.ShowAlert(res.GetString("NoInternet"));
            }

            // errors?
            if (errors.HasErrors)
                await UIHelper.ShowAlert(errors.GetErrorsAsString());

            IsBusy = false;

        }

        private async Task AddUserAsync()
        {
            await UIHelper.ToggleProgressBar(true, res.GetString("Loading"));

            input.Add("phone_number", Phone);
            input.Add("name", FullName);
            input.Add("id_number", IdNo);
            input.Add("PIN", PIN);
            input.Add("code", Code);


            var MyResult = await Rest.PostAsync("users/add", input.Stringify());

            statuscode = MyResult.Key;

            output = MyResult.Value;


            if (statuscode == HttpStatusCode.OK)
            {
                if ((string)output["status"] != "error")
                {
                    _navigationService.NavigateTo("AddCard",Phone);
                    await UIHelper.ShowAlert((string)output["message"]);
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

        //validate AddUser
        private void ValidateSignUp(ErrorBucket errors)
        {
            if (string.IsNullOrEmpty(FirstName))
                errors.AddError(res.GetString("RequiredFirstName"));

            if (string.IsNullOrEmpty(LastName))
                errors.AddError(res.GetString("RequiredLastName"));

            if (string.IsNullOrEmpty(IdNo))
                errors.AddError(res.GetString("RequiredID"));

            if (string.IsNullOrEmpty(Phone) || Phone.Length < 12)
                errors.AddError(res.GetString("InvalidPhone"));

            if (string.IsNullOrEmpty(Code))
                errors.AddError(res.GetString("RequiredCode"));

            if (string.IsNullOrEmpty(PIN))
                errors.AddError(res.GetString("RequiredPIN"));

            if (string.IsNullOrEmpty(Confirm))
                errors.AddError(res.GetString("VerifyPin/Text"));

            if (!(string.IsNullOrEmpty(PIN)) && PIN.Length < 4)
                errors.AddError(res.GetString("ValidPIN"));

            // check the PINs...
            if (!(string.IsNullOrEmpty(PIN)) && PIN != Confirm && !(string.IsNullOrEmpty(PIN)))
                errors.AddError(res.GetString("MatchPIN"));


        }



    }
}