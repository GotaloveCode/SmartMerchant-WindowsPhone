using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using GalaSoft.MvvmLight.Views;
using System;

namespace SmartMerchant.ViewModel
{
    public class EditCardViewModel : ViewModelBase, INavigable
    {
        //Properties
        #region
        protected INavigationService _navigationService;       
        public ICommand UpdateListCommand { get { return new RelayCommand(Suggest); } }
        public ICommand EditCardCommand { get; set; }
        public ICommand BackCommand { get; set; }
        CardDetail SelectedCard { get; set; }
        string _phoneNumber = "250", _expiryMonth, _expiryYear, _expiry, _cardNo, _bank, _cvv;
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
        List<string> _banks;
        bool _IsBusy = false;
        public bool IsBusy
        {
            get { return _IsBusy; }
            set { Set(() => IsBusy, ref _IsBusy, value); }
        }
        
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        ResourceLoader res = ResourceLoader.GetForCurrentView();
        List<string> Cards = new List<string>();
        List<CardDetail> Cardslist = new List<CardDetail>();
        #endregion


        public EditCardViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            if (!IsInDesignMode)
            {
                BackCommand = new RelayCommand(() => _navigationService.GoBack());
                EditCardCommand = new RelayCommand(async () => await EditCard());

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

        private async Task EditCard()
        {

            ErrorBucket errors = new ErrorBucket();
            IsBusy = true;
            // validate...
            ValidateAddCard(errors);

            // ok?
            if (!(errors.HasErrors))
            {
                if (UIHelper.HasInternetConnection())
                    await EditCardAsync();
                else
                    await UIHelper.ShowAlert(res.GetString("NoInternet"));
            }

            // errors?
            if (errors.HasErrors)
                await UIHelper.ShowAlert(errors.GetErrorsAsString());

            IsBusy = false;

        }

        private async Task EditCardAsync()
        {
            await UIHelper.ToggleProgressBar(true, res.GetString("Loading"));
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("Cards"))
            {
                Cards = ((string[])ApplicationData.Current.LocalSettings.Values["Cards"]).ToList();
                Cardslist.Clear();
                for (int i = 0; i < Cards.Count(); i++)
                {
                    string[] cardArray = Cards[i].Split('*');
                    CardDetail Cardtoupdate = new CardDetail()
                    {
                        Id = i+1,
                        Bank = cardArray[0],
                        CardNo = cardArray[1],
                        Expiry = cardArray[2],
                        CVV = cardArray[3]
                    };
                    // update with edited values
                    if (Cardtoupdate.Bank == SelectedCard.Bank && Cardtoupdate.CardNo == SelectedCard.CardNo
                        && Cardtoupdate.Expiry == SelectedCard.Expiry && Cardtoupdate.CVV == SelectedCard.CVV)
                    {
                        Cardtoupdate.Bank = Bank;
                        Cardtoupdate.CardNo = CardNo;
                        Cardtoupdate.Expiry = Expiry;
                        Cardtoupdate.CVV = CVV;
                        Cardslist.Add(Cardtoupdate);
                    }
                    else
                    {
                        Cardslist.Add(Cardtoupdate);
                    }
                    
                }
                Cardslist.Remove(SelectedCard);
            }

            Cards.Clear(); //clear list of cards

            foreach (var item in Cardslist) //add cards back with updated card
            {
                string cardstring = string.Format("{0}*{1}*{2}*{3}", item.Bank, item.CardNo, item.Expiry, item.CVV);
                Cards.Add(cardstring);
            }
            ApplicationData.Current.LocalSettings.Values["Cards"] = Cards.ToArray(); //save changes
            await UIHelper.ToggleProgressBar(false);
            _navigationService.GoBack(); //go back to manage card
        }

        private async Task GetBanks()
        {
            await UIHelper.ToggleProgressBar(true, res.GetString("Loading"));

            var MyResult = await Rest.GetAsync("banks", "");

            HttpStatusCode statuscode = MyResult.Key;

            JObject output = MyResult.Value;
            await UIHelper.ToggleProgressBar(false);
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
                    string error = (string)output["error"]["message"][0];
                    await UIHelper.ShowAlert(error);
                }
            }
            else if (statuscode == HttpStatusCode.Unauthorized)
            {                
            }
            else
            {
                string error = (string)output["error"]["message"][0];
                await UIHelper.ShowAlert(error);
            }


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
     
        public async void Activate(object parameter)
        {
            if (parameter is CardDetail)
            {
                await GetBanks();
                SelectedCard = (CardDetail)parameter;
                Bank = SelectedCard.Bank;
                CardNo = SelectedCard.CardNo;
                CVV = SelectedCard.CVV;
                string[] expirystring = SelectedCard.Expiry.Split('/');
                ExpiryMonth = expirystring[0];
                ExpiryYear = expirystring[1];
               
            }
            
        }

        public void Deactivate(object parameter)
        {
        }
    }
}
