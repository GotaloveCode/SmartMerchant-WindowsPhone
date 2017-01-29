using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Views;
using System.Windows.Input;

namespace SmartMerchant.ViewModel
{
    public class HomeViewModel : ViewModelBase
    {
        //Properties
        #region
        protected INavigationService _navigationService;
        public ICommand LogoutCommand { get; private set; }
        public ICommand AddUserCommand { get; private set; }
        public ICommand AddCardCommand { get; private set; }
        public ICommand PrintQRCommand { get; private set; }

        #endregion

        public HomeViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            if (!IsInDesignMode)
            {
                AddUserCommand = new RelayCommand(() =>{ _navigationService.NavigateTo("AddUser");});
                AddCardCommand = new RelayCommand(() => { _navigationService.NavigateTo("AddCard"); });
                PrintQRCommand = new RelayCommand(() => { _navigationService.NavigateTo("QR"); });
                LogoutCommand = new RelayCommand(async () => await Rest.LogOutAsync());
            }
        }





    }
}
