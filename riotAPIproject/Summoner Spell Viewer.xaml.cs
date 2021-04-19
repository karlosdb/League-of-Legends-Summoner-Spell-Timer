using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace riotAPIproject
{
    // this is just the main windown and a frame for which Page 1 and Page 2 can function in
    // in depth explanation of app in Page2 class
    // method for this found from: https://www.youtube.com/watch?v=YoZcAx_0rNM
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MyWindow_Loaded; //load main window
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            frame.NavigationService.Navigate(new Page1()); //when main window is loaded, navigate to page 1, so program starts @ page 1
        }

        //this is simply needed to navigate the frame, it doen't need anything in it
        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {

        }
    }
}
