using System.Windows;
using WpfMefApp.ViewModels;

namespace WpfMefApp.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
