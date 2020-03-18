using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Resources;
using Windows.Data.Json;
using Windows.Storage;
using GalaSoft.MvvmLight.Views;
using System;
using System.Diagnostics;
using Newtonsoft.Json;

namespace SmartMerchant.ViewModel
{
    public class AddCardViewModel : ViewModelBase, INavigable
    {
        //Properties
        #region
        protected INavigationService _navigationService;
        public ICommand BackCommand { get; set; }
        public ICommand UpdateListCommand { get { return new RelayCommand(Suggest); } }
        public ICommand AddCardCommand { get; set; }
        public ICommand SubmitCardCommand { get; set; }
        public ICommand VerifyCommand { get; set; }
        public string Phone { get { return _phoneNumber; } set { _phoneNumber = value; } }
        public string CardNo { get { return _cardNo; } set { _cardNo = value; RaisePropertyChanged("CardNo"); } }
        public string Bank { get { return _bank; } set { _bank = value; RaisePropertyChanged("Bank"); } }
        public string CVV { get { return _cvv; } set { _cvv = value; RaisePropertyChanged("CVV"); } }
        public string ExpiryMonth
        {
            get { return _expiryMonth; }
            set
            {
                Set(() => ExpiryMonth, ref _expiryMonth, value);
                RaisePropertyChanged("Expiry");
            }
        }
        public string ExpiryYear
        {
            get { return _expiryYear; }
            set
            {
                Set(() => ExpiryYear, ref _expiryYear, value);
                RaisePropertyChanged("Expiry");
            }
        }
        public string Expiry { get { return ExpiryMonth + "/" + ExpiryYear; } set { _expiry = ExpiryMonth + "/" + ExpiryYear; RaisePropertyChanged("Expiry"); } }
        public string Code { get; set; }
        public string AddCardNo { get { return _addCardNo; } set { _addCardNo = value; RaisePropertyChanged("AddCardNo"); } }
        public List<string> bankslist { get; set; }
        public List<string> Bankslist
        {
            get { return _banks; }
            set
            {
                Set(() => Bankslist,
                    ref _banks, value);
            }
        }
        private List<string> _banks;

        string _phoneNumber = "250", _expiryMonth, _expiryYear, _expiry,
            _cardNo,_bank,_cvv,_addCardNo;
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
        List<string> Cards = new List<string>();

        #endregion


        public AddCardViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            if (!IsInDesignMode)
            {
                AddCardNo = res.GetString("AddCard/Text");
                BackCommand = new RelayCommand(() => _navigationService.GoBack());
                AddCardCommand = new RelayCommand(async () => await AddToCardsList());
                SubmitCardCommand = new RelayCommand(async () => await AddCard());
                VerifyCommand = new RelayCommand(async () => await Verify());
            }
        }

        //autosuggest filter
        private void Suggest()
        {
            if (!string.IsNullOrEmpty(Bank))
            {
                Bankslist = bankslist.Where(x => x.ToLower().Contains(Bank.ToLower())).ToList();
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

            var MyResult = await Rest.PostAsync("user-phone-verify", input.Stringify());

            statuscode = MyResult.Key;

            output = MyResult.Value;

            await UIHelper.ToggleProgressBar(false);

            if (statuscode == HttpStatusCode.OK)
            {
                await UIHelper.ShowAlert((string)output["message"][0]);
            }
            else
            {
                await UIHelper.ShowAlert((string)output["error"]["message"][0]);
            }
            input.Clear();
        }

        private async Task AddCard()
        {

            ErrorBucket errors = new ErrorBucket();
            IsBusy = true;
            // validate...
            ValidateAddCards(errors);

            // ok?
            if (!(errors.HasErrors))
            {
                if (UIHelper.HasInternetConnection())
                    await AddCardAsync();
                else
                    await UIHelper.ShowAlert(res.GetString("NoInternet"));
            }

            // errors?
            if (errors.HasErrors)
                await UIHelper.ShowAlert(errors.GetErrorsAsString());

            IsBusy = false;

        }

        private async Task AddCardAsync()
        {
            await UIHelper.ToggleProgressBar(true, res.GetString("Loading"));
            CardItem cardItem = new CardItem();
            cardItem.Phone = Phone;
            cardItem.Code = Code;
            List<string> bankslst = new List<string>();
            List<string> cardslst = new List<string>();
            List<string> expiryslst = new List<string>();
            List<string> cvvslst = new List<string>();

            for (int i = 0; i < Cards.Count(); i++)
            {
                string[] cardArray = Cards[i].Split('*');
                bankslst.Add(cardArray[0]);
                cardslst.Add(cardArray[1]);
                expiryslst.Add(cardArray[2]);
                cvvslst.Add(cardArray[3]);
            }
            cardItem.BankArray = bankslst;
            cardItem.CardArray = cardslst;
            cardItem.ExpiryArray = expiryslst;
            cardItem.CVVArray = cvvslst;
            cardItem.NumberOfCards = Cards.Count();

            string jsonstr = JsonConvert.SerializeObject(cardItem);
            Debug.WriteLine(jsonstr);

            var MyResult = await Rest.PostAsync("users/add-card", jsonstr);
            statuscode = MyResult.Key;
            output = MyResult.Value;

            if (statuscode == HttpStatusCode.OK)
            {
                if ((string)output["status"] != "error")
                {
                    if (ApplicationData.Current.LocalSettings.Values.ContainsKey("Cards"))
                        ApplicationData.Current.LocalSettings.Values.Remove("Cards");
                    _navigationService.NavigateTo("QR",Phone);
                }
                else
                {
                    await UIHelper.ShowAlert((string)output["error"]["message"][0]);
                }
            }
            else if (statuscode.ToString() == "422" )
            {
                JArray banksArray = (JArray)output["error"]["message"];
                ErrorBucket errors = new ErrorBucket();
                List<string> lsterrors = banksArray.Select(c => (string)c).ToList();
                foreach (var item in lsterrors)
                    errors.AddError(item);
                if(lsterrors.Count() == 1 && lsterrors[0] == "Invalid verification code")
                {
                    await UIHelper.ShowAlert(errors.GetErrorsAsString());
                }
                else
                {
                    _navigationService.NavigateTo("ManageCard");
                    await UIHelper.ShowAlert(errors.GetErrorsAsString());
                }

            }
            else if(statuscode == HttpStatusCode.Unauthorized)
            {                
            }
            else
            {
                await UIHelper.ShowAlert((string)output["error"]["message"][0]);
            }
            await UIHelper.ToggleProgressBar(false);
            input.Clear();
        }

        async Task AddToCardsList()
        {
            ErrorBucket errors = new ErrorBucket();
            ValidateAddCard(errors);

            if (!(errors.HasErrors))
            {   //create string and add to list
                string cardstring = string.Format("{0}*{1}*{2}*{3}", Bank, CardNo, Expiry, CVV);
                Cards.Add(cardstring);
                Bank = string.Empty;
                CardNo = string.Empty;
                ExpiryMonth = string.Empty;
                ExpiryYear = string.Empty;
                CVV = string.Empty;
                Bank = string.Empty;

                // save as array
                if (Cards.Count > 0)
                {
                    ApplicationData.Current.LocalSettings.Values["Cards"] = Cards.ToArray();
                    AddCardNo = res.GetString("AddCard/Text") + "[" + Cards.Count + "]";
                }
            }
            else
            {
                await UIHelper.ShowAlert(errors.GetErrorsAsString());
            }

        }

        private async Task GetBanks()
        {
            await UIHelper.ToggleProgressBar(true, res.GetString("Loading"));
            KeyValuePair<HttpStatusCode,JObject> MyResult = await Rest.GetAsync("banks", "");
            HttpStatusCode statuscode = MyResult.Key;
            JObject output = MyResult.Value;
            
            if (statuscode == HttpStatusCode.OK)
            {
                string status = (string)output["status"];
                if (status != "error")
                {
                    JArray banksArray = (JArray)output["data"];
                    List<string> lstbank = banksArray.Select(c => (string)c).ToList();
                    bankslist = new List<string>();
                    Bankslist = new List<string>();
                    foreach (var item in lstbank)
                    {
                        Bankslist.Add(item);
                        bankslist.Add(item);
                    }
                }
                else
                {
                    await UIHelper.ShowAlert((string)output["error"]["message"][0]);
                }
            }
            else if (statuscode == HttpStatusCode.Unauthorized)
            {               
            }
            else
            {
                await UIHelper.ShowAlert((string)output["error"]["message"][0]);
            }
            await UIHelper.ToggleProgressBar(false);
        }

        private void ValidateAddCard(ErrorBucket errors)
        {
            if (string.IsNullOrEmpty(Bank))
                errors.AddError(res.GetString("RequiredBank"));

            if (string.IsNullOrEmpty(CardNo))
                errors.AddError(res.GetString("RequiredCardNo"));

            if (string.IsNullOrEmpty(Expiry))
                errors.AddError(res.GetString("RequiredExpiry"));

            if (string.IsNullOrEmpty(CVV.ToString()))
                errors.AddError(res.GetString("RequiredCVV"));

        }

        //validate AddCard
        private void ValidateAddCards(ErrorBucket errors)
        {
            if (Cards.Count() < 1)
                errors.AddError(res.GetString("NeedCard"));

            if (string.IsNullOrEmpty(Phone) || Phone.Length < 12)
                errors.AddError(res.GetString("InvalidPhone"));

            if (string.IsNullOrEmpty(Code))
                errors.AddError(res.GetString("RequiredCode"));

        }

        public async void Activate(object parameter)
        {
            if (parameter is string)
                Phone = parameter.ToString();

            await GetBanks();
            Cards.Clear();

            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("Cards"))
            {
                Cards = ((string[])ApplicationData.Current.LocalSettings.Values["Cards"]).ToList();
                if (Cards.Count > 0)
                    AddCardNo = res.GetString("AddCard/Text") + "[" + Cards.Count + "]";
            }
            else
            {
                AddCardNo = res.GetString("AddCard/Text");
            }
        }

        public void Deactivate(object parameter)
        {
        }
    }
}