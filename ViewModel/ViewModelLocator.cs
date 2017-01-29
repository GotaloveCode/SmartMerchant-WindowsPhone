/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:SmartAgent"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Views;
using Microsoft.Practices.ServiceLocation;

namespace SmartMerchant.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            ////if (ViewModelBase.IsInDesignModeStatic)
            ////{
            ////    // Create design time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DesignDataService>();
            ////}
            ////else
            ////{
            ////    // Create run time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DataService>();
            ////}
            var navigationService = CreateNavigationService();
            SimpleIoc.Default.Register(() => navigationService);
            SimpleIoc.Default.Register<RegisterViewModel>();
            SimpleIoc.Default.Register<LoginViewModel>();
            SimpleIoc.Default.Register<MainViewModel>();
            SimpleIoc.Default.Register<AddUserViewModel>();
            SimpleIoc.Default.Register<AddCardViewModel>();
            SimpleIoc.Default.Register<ManageCardViewModel>();
            SimpleIoc.Default.Register<EditCardViewModel>();
            SimpleIoc.Default.Register<QRViewModel>();
            SimpleIoc.Default.Register<HomeViewModel>();
        }

        public MainViewModel Main
        {
            get
            {
                return ServiceLocator.Current.GetInstance<MainViewModel>();
            }
        }


        public RegisterViewModel Register
        {
            get
            {
                return ServiceLocator.Current.GetInstance<RegisterViewModel>();
            }
        }

        public LoginViewModel Login
        {
            get
            {
                return ServiceLocator.Current.GetInstance<LoginViewModel>();
            }
        }

        public HomeViewModel Home
        {
            get
            {
                return ServiceLocator.Current.GetInstance<HomeViewModel>();
            }
        }

        public AddCardViewModel AddCard
        {
            get
            {
                return ServiceLocator.Current.GetInstance<AddCardViewModel>();
            }
        }

        public EditCardViewModel EditCard
        {
            get
            {
                return ServiceLocator.Current.GetInstance<EditCardViewModel>();
            }
        }

        public ManageCardViewModel ManageCard
        {
            get
            {
                return ServiceLocator.Current.GetInstance<ManageCardViewModel>();
            }
        }

        public AddUserViewModel AddUser
        {
            get
            {
                return ServiceLocator.Current.GetInstance<AddUserViewModel>();
            }
        }

        public QRViewModel QR
        {
            get
            {
                return ServiceLocator.Current.GetInstance<QRViewModel>();
            }
        }

        private INavigationService CreateNavigationService()
        {
            var navigationService = new NavigationService();
            navigationService.Configure("Login", typeof(LoginPage));
            navigationService.Configure("Register", typeof(RegisterPage));
            navigationService.Configure("AddUser", typeof(AddUserPage));
            navigationService.Configure("AddCard", typeof(AddCardPage));
            navigationService.Configure("EditCard", typeof(EditCardPage));
            navigationService.Configure("ManageCard", typeof(ManageCardPage));
            navigationService.Configure("Main", typeof(MainPage));
            navigationService.Configure("AgentHome", typeof(AgentHomePage));
            navigationService.Configure("QR", typeof(QRPage));
            return navigationService;
        }

        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}