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
using RiotSharp.Misc;

namespace riotAPIproject
{
    /// Page1 is for inputting the name of the player and region the player is in 
    public partial class Page1 : Page
    {
        //initialize page 1 no params
        public Page1()
        {
            InitializeComponent(); 
        }

        //this is for when navigating back to page 1 from page 2
        public Page1(string summonerName, string message, string apiKey, string txtFileLoc, int regionNumber)
        {
            InitializeComponent();
            summonerNameInput.Text = summonerName; //make the summoner search box have the name of the found summoner
            messageBox.Text = message; //make the message box have whatever message from page 2
            apiKeyBox.Text = apiKey; //set the apikey box
            textFileLocationBox.Text = txtFileLoc; //set the text file box

            // check the button corresponding to the region number
            switch (regionNumber)
            {
                case 2:
                    EUWButton.IsChecked = true;
                    break;
                case 3:
                    NAButton.IsChecked = true;
                    break;
                case 4:
                    KRButton.IsChecked = true;
                    break;
            }
        }

        // for allowing pressing enter to search
        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            // watch if the enter key is pressed, and have that press the 'search' button
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                messageBox.Text = "Loading..."; //make the message box convey that the program is executing the search
                Button_Click(new object(), new RoutedEventArgs());
            }
        }

        //execute the search based on inputted summoner name and selected region
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            messageBox.Text = "Loading..."; //make the message box convey that the program is executing the search
            string summonerName = summonerNameInput.Text; //set the summoner name to be searched from what the user inputted

            if (summonerName != "") // if the input box isn't empty
            {
 
                Region region = Region.Na; //default region is Na (North America)

                //set the server region based on what radio button is selected (Na = North America, Euw = Western Europe, Kr = South Korea)
                if (NAButton.IsChecked == true)
                {
                    region = Region.Na;
                }
                else if (EUWButton.IsChecked == true)
                {
                    region = Region.Euw;
                }
                else if (KRButton.IsChecked == true)
                {
                    region = Region.Kr;
                }

                //try to execute the search using the summoner name and region
                try
                {
                    this.NavigationService.Navigate(await Page2.Create(summonerName, region, apiKeyBox.Text, textFileLocationBox.Text)); //if successful, page 2 will be navigated to


                }//if search fails, update message box to say why based on exception received
                catch (Exception ex)
                {
                    messageBox.Text = ex.Message;
                    Console.WriteLine(ex.StackTrace);
                }

            } else
            {
                messageBox.Text = ""; //clear message box if box was empty
            }

            
        }

      
    }
}
