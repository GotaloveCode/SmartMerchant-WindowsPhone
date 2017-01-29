using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Views;
using GalaSoft.MvvmLight.Command;
using Windows.Storage;
using Windows.ApplicationModel.Resources;
using Windows.UI.Popups;

namespace SmartMerchant.ViewModel
{
    public class ManageCardViewModel : ViewModelBase, INavigable
    {
        protected INavigationService _navigationService;
        private ObservableCollection<CardDetail> _Cards;
        public ObservableCollection<CardDetail> Cards
        {
            get { return _Cards; }
            set
            {
                Set(() => Cards, ref _Cards, value);
            }
        }
        List<string> lstCards = new List<string>();

        public ICommand EditCardCommand { get; private set; }
        public ICommand DeleteCardCommand { get; private set; }
        public ICommand LogoutCommand { get; private set; }
       
        
        bool _hasCard = true;
        public bool HasCard
        {
            get { return _hasCard; }
            set { Set(() => HasCard, ref _hasCard, value); }
        }
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        ResourceLoader res = ResourceLoader.GetForCurrentView();


        public ManageCardViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            if (!IsInDesignMode)
            {
                LogoutCommand = new RelayCommand(async () => await Rest.LogOutAsync());
                DeleteCardCommand = new RelayCommand<int>(async (id) => await DeleteCardAsync(id));
                EditCardCommand = new RelayCommand<int>((id) => EditCard(id));
            }

        }

        private void EditCard(int id)
        {
            CardDetail SelectedCard = Cards.Where(x => x.Id == id).FirstOrDefault();
            _navigationService.NavigateTo("EditCard", SelectedCard);
        }

        private async Task DeleteCardAsync(int id)
        {
            if (id > 0)
            {
                CardDetail SelectedCard = Cards.Where(x => x.Id == id).FirstOrDefault();

                MessageDialog dialog = new MessageDialog(res.GetString("RemoveCard"));
                dialog.Commands.Add(new UICommand(res.GetString("Yes"), async delegate (IUICommand command)
                {
                    await DeleteCard(id);
                }));
                dialog.Commands.Add(new UICommand(res.GetString("No")));
                await dialog.ShowAsync();
            }
        }

        private async Task DeleteCard(int id)
        {  //get card to remove from queryable list of cards
            CardDetail SelectedCard = Cards.Where(x => x.Id == id).FirstOrDefault();
            Cards.Remove(SelectedCard); //remove card
            //Get cards from saved list          
            List<string> Cardslst = new List<string>();
            foreach (var item in Cards) //add cards back with updated card
            {
                string cardstring = string.Format("{0}*{1}*{2}*{3}", item.Bank, item.CardNo, item.Expiry, item.CVV);
                Cardslst.Add(cardstring);
            }
            if (Cardslst.Count() > 0)
            {
                ApplicationData.Current.LocalSettings.Values["Cards"] = Cardslst.ToArray(); //save changes
            }
            else
            {
                ApplicationData.Current.LocalSettings.Values.Remove("Cards");
                HasCard = false;
            }

            await UIHelper.ShowAlert(res.GetString("CardRemoved"));

        }

        private void GetCards()
        {
            Cards = new ObservableCollection<CardDetail>();
            //retrieve Cards array and make a list
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("Cards"))
                lstCards = ((string[])ApplicationData.Current.LocalSettings.Values["Cards"]).ToList();

            if (lstCards.Count() > 0)
            {
                HasCard = true;
                for (int i = 0; i < lstCards.Count(); i++)
                {
                    string[] cardArray = lstCards[i].Split('*');
                    Cards.Add(new CardDetail() { Bank = cardArray[0], CardNo = cardArray[1], Expiry = cardArray[2], CVV = cardArray[3], Id = i + 1 });
                }
            }
        }

        void INavigable.Activate(object parameter)
        {
            GetCards();
        }

        void INavigable.Deactivate(object parameter)
        {
        }

    }
}

