using System.Windows;
using PortScanner.ViewModels;

namespace PortScanner.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
