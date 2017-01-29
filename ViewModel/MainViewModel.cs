using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Views;
using System.Windows.Input;

namespace SmartMerchant.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
      
        public ICommand LogoutCommand { get; private set; }
        
        public MainViewModel()
        {
            if (!IsInDesignMode)
            {
                LogoutCommand = new RelayCommand(async () => await Rest.LogOutAsync());
            }
        }

    }
}