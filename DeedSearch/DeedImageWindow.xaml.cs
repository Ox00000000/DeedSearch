using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DeedSearch
{
    /// <summary>
    /// Interaction logic for DeedImageWindow.xaml
    /// </summary>
    public partial class DeedImageWindow : NavigationWindow
    {
        public DeedImageWindow()
        {
            InitializeComponent();
        }

        public void SetSource(string url)
        {
            this.Source = new Uri(url);
            
        }
    }
}
