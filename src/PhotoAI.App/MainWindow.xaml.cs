using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using PhotoAI.App.ViewModels;

namespace PhotoAI.App;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ViewModel.ExecuteSearchCommand.Execute(null);
        }
    }
}
