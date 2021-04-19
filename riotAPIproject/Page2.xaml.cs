using RiotSharp;
using RiotSharp.Caching;
using RiotSharp.Endpoints.Interfaces.Static;
using RiotSharp.Endpoints.SpectatorEndpoint;
using RiotSharp.Endpoints.StaticDataEndpoint;
using RiotSharp.Endpoints.StaticDataEndpoint.Champion;
using RiotSharp.Endpoints.StaticDataEndpoint.SummonerSpell;
using RiotSharp.Endpoints.SummonerEndpoint;
using RiotSharp.Misc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace riotAPIproject
{
    /*
     * This is the meat of the program. 
     * TERMIMOLOGY: champion = character, summmoner = player, summoner spell = a special spell a champion/player can have 
     *  (these are the kind of spells tracked by the program), riot games = the company that makes this game (league of legends),
     *  cooldown = the time it takes for a spell to be available again
     * 
     * Page 2 takes in the inputted information from page 1 to load the enemy champion and spells from a live/current game.
     *  each champion picture and their respective spell pictures are then displayed (each row is one enemy champion/spells combo)
     * The user can then click the button next to a spell to start the countdown timer for that spell. Input can also be taken from 
     *  a text file when the text file is updated and saved. (this can be used in game, where using a command you can write a message to
     *  a MyNotes.txt file which will then be read and interpereted by the programm.
     *     
     * This is still a developer version, so the developer API key expires every 24 hours. In a registered app, the API key would not expire 
     *  and would not be embedded in the program.
     * 
     * UML: (IN DOWNLOADED FILES OF THIS SOLUTION)
     * link to where you can find live games to test: https://www.twitch.tv/trackingthepros 
     * video presentation: https://youtu.be/bHfb6avhOyA
     */
    public partial class Page2 : Page
    {
        // load the api using a unique API key (EXPIRES EVERY 24H)
        // this is used for getting the summoner data and live game data   
        private const string apiKey = "RGAPI-992f7638-241b-4da2-9564-f4c67b191b2e";
        private RiotApi api = RiotApi.GetDevelopmentInstance(apiKey);
        private string returnApiKey = apiKey; // string to be returned for page 1's apikey box because the original apikey is non mutable
        private int returnRegion = 1; // number for region to be returned to page 1

        // this is the location of the text file where the user can start the spell cooldowns from
        private string noteFileLocation = @"C:\Riot Games\League of Legends\MyNotes.txt";
        //string noteFileLocation = @"C:\Users\offic\OneDrive - 绥化市教育学院\Fall 2020\OOPL\repos\riotAPIproject\My Notes.txt";

        // load data dragon, which is the API for retrieving static information about the game's 
        // champions like champion names, spell names and cooldowns
        private const string staticVersion = "10.22.1"; //current data dragon api version
        private Cache cache = new Cache();
        private IDataDragonEndpoints staticApi = DataDragonEndpoints.GetInstance(true);


        // list of a combo of a champion and it's spells, used to display the pictures 
        private List<SummonerGroup> summonerGroups = new List<SummonerGroup>(5);

        // list of 'participants' or players, of the enemy team. This is taken straight from the API, then is parsed into summonerGroups
        private List<CurrentGameParticipant> enemyTeam = new List<CurrentGameParticipant>(5);

        //brush colors for the buttons
        private Brush redColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFF64242"));
        private Brush greenColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF09BA3E"));

        // string for putting the cooldowns into the clipboard, and list of cooldownstring objects to manage live cooldown information
        private string cooldownsString = "";
        private List<CooldownString> cooldownStrings = new List<CooldownString>();


        // this is the class whos data is used to display information on the GUI
        private class SummonerGroup
        {
            public string champName { get; set; } // name of champion
            public ImageSource champIcon { get; set; } // Image of champion's icon, can be set to image objects in the UI
            public ImageSource spell1 { get; set; } // Image of spell1's icon
            public ImageSource spell2 { get; set; } // Image of spell2's icon
            public float spell1Cd { get; set; } // cooldown of spell1
            public float spell2Cd { get; set; } // cooldown of spell2
            public string spell1Name { get; set; } // name of spell1
            public string spell2Name { get; set; } // name of spell2


            // constructor takes in champion object, 2 spell objects, and a string for the static api verson
            public SummonerGroup(ChampionStatic champion, SummonerSpellStatic spell1, SummonerSpellStatic spell2, string staticVersion)
            {
                // set the champion name by taking the champion object's name and making it lower case
                // and taking out any non letter characters (LINQ usage)              
                this.champName = new string(champion.Name.ToLower().Where(c => Char.IsLetter(c)).ToArray());

                // name of the champion image file name from champion object
                string champImgName = champion.Image.Full;

                // the teleoprt (tp) spell has a variable cooldown based on live champion level
                // that can't be accessed in real time, so I took an average based on average game time and average level at half game time             
                float avgTpCd = 324;

                // spell name from spell1 object, made lower for comparability
                this.spell1Name = spell1.Name.ToLower();
                // spell1 image name from spell1 object
                string spell1ImgName = spell1.Image.Full;

                // if spell is teleport, make it the approximate teleport cooldown, otherwise just use spell object cooldwon value
                float spell1Cd = spell1.Cooldowns[0];
                if (spell1Cd == 0)
                {
                    spell1Cd = avgTpCd;
                }
                this.spell1Cd = spell1Cd;

                // spell2 name and image names from spell2 object
                this.spell2Name = spell2.Name.ToLower();
                string spell2ImgName = spell2.Image.Full;

                // if spell is teleport, make it the approximate teleport cooldown, otherwise just use spell object cooldwon value
                float spell2Cd = spell2.Cooldowns[0];
                if (spell2Cd == 0)
                {
                    spell2Cd = avgTpCd;
                }
                this.spell2Cd = spell2Cd;

                // get the ImageSource objects for the champion and spell icons using a 
                // static api url link using our variables that gets converted into a bitmapimage
                this.champIcon = new BitmapImage(new Uri($@"http://ddragon.leagueoflegends.com/cdn/{staticVersion}/img/champion/{champImgName}"));
                this.spell1 = new BitmapImage(new Uri($@"http://ddragon.leagueoflegends.com/cdn/{staticVersion}/img/spell/{spell1ImgName}"));
                this.spell2 = new BitmapImage(new Uri($@"http://ddragon.leagueoflegends.com/cdn/{staticVersion}/img/spell/{spell2ImgName}"));


            }
        }


        // class for holding a champion name, spell name, and the cooldown for that spell to be turned into a string
        private class CooldownString
        {
            public string champName { get; set; } // champion name
            public string spellName { get; set; }
            public string cooldown { get; set; }


            // constructor
            public CooldownString(string champName, string spellName, string coolDown)
            {
                this.champName = champName;
                this.spellName = spellName;
                this.cooldown = coolDown;
            }


            // turn object into string in the form champion name + spell name + spell cooldown
            public string toString()
            {
                return $"{champName} {spellName} {cooldown} seconds";
            }
        }


        // the api calls have to be asynchronous methods, so they can't be used in a constructor. Instead, I created a public static async 
        // method that in turn creates and returns an instance of Page2, so it functions as kind of an async constructor
        public static async Task<Page2> Create(string summonerName, Region region, string apiKey, string txtFileLoc)
        {
            // create an instance of this class (Page2) to be returned
            var page = new Page2(); 

            // set the api from the input in page 1 if it isn't the default or empty          
            if (!apiKey.Equals("api key") && !apiKey.Equals(""))
            {               
                page.api = RiotApi.GetDevelopmentInstance(apiKey);
                page.returnApiKey = apiKey;
            }

            // set the file location from page 1 input if it isn't the default or empty
            if(!txtFileLoc.Equals("txt file") && !txtFileLoc.Equals(""))
            {           
                    page.noteFileLocation = txtFileLoc;
                
            }

            // get the region for returning to page 1
            // Na = 3, Euw = 2, Kr = 4
            page.returnRegion = (int) region; 
            
            // get summoner using input and riot API
            var summoner = page.getSummoner(summonerName, region);
            // display name of summoner
            page.summonerNameDisplay.Text = summoner.Name;

            //get the info for the current game of the summoner
            await page.getCurrentGame(summoner, region);

            // set the pictures for the summonerGroups
            page.setImageGroups();

            // watch the file for updates, and if detected, update the app accordingly
            page.runWatch();

            // return the instance of Page2
            return page;
        }

        // normal constructor for Page2
        private Page2()
        {
            InitializeComponent();

        }

        // watch the file for updates
        private void runWatch()
        {
            // create and set up FileSystemWatcher object
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = System.IO.Path.GetDirectoryName(noteFileLocation);     
            watcher.NotifyFilter = NotifyFilters.LastWrite; // set watcher to look for writes to the file
            watcher.Filter = System.IO.Path.GetFileName(noteFileLocation);

            // Add event handlers.
            watcher.Changed += new FileSystemEventHandler(onFileChanged);
            watcher.Created += new FileSystemEventHandler(onFileChanged);

            // Begin watching.
            watcher.EnableRaisingEvents = true;
        }

        // method to be executed when file change detected
        private void onFileChanged(object sender, FileSystemEventArgs e)
        {
            // wait one second to make sure the watcher can't be overloaded by edits that are too quick
            Thread.Sleep(TimeSpan.FromSeconds(0.5));

            // read the text file
            readFromTxt();
        }

        // read the text file to find the most recent line addition, then parse that line
        private void readFromTxt()
        {
            try
            {
                // read last line, then split it into an array based on the spaces
                string[] lastLine = System.IO.File.ReadLines(noteFileLocation).Last().Split(' ');

                // parse the data taken from the last line of the file
                parseLastLine(lastLine);
 

            }
            // if something doesn't work in parsing the line of the file, catch the error
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return;
            }
        }

        // take the input in the form of [champion name] and [spell name] and convert 
        // that to a champion and spell number using summonerGroups
        private void parseLastLine(string[] line) //EXAMPLE INPUT: [annie] [flash]
        {

            // if the command is "n", send all the summoner cooldowns in game chat
            if (line[0].Equals("n"))
            {
                
                foreach (CooldownString cds in cooldownStrings)
                {
                    //send each cooldown in chat individually, since you can only send one line at a time in the game
                    sendString(cds.toString()); 
                }
                          
            }

            // make input into all lowercase for the sake of comparison
            string inputName = line[0].ToLower();
            string inputSpell = line[1].ToLower();

            // find the summoner group associated with this champion name (LINQ IMPLEMENTATION)
            SummonerGroup summonerGroup = summonerGroups.First(sg => sg.champName.Equals(inputName));

            // find the champion number by finding the index of the found summonerGroup
            int champNumber = summonerGroups.FindIndex(sg => sg == summonerGroup)+1;

            // find if the spell number is 1 or 2 based on which one it matches in the summonerGroup
            int spellNumber = 0;
            if(inputSpell == summonerGroup.spell1Name)
            {
                spellNumber = 1;
            } else if (inputSpell == summonerGroup.spell2Name)
            {
                spellNumber = 2;
            }

            // find if the player inputted a cooldown modifier as their 
            // 3rd input (number to be subtracted from the original cooldown)
            int cdModifier = 0;
            if (line.Length >= 3)
            {
                Int32.TryParse(line[2], out cdModifier);
                
            }

            // find if the player inputted a champion level
            // in the case of teleport (which has a special cooldown based on level)
            int champLevel = 0;
            if (inputSpell.Equals("teleport"))
            {
                // if you want to put teleport level and modifier, the formula is champion, spell, champLevel, modifier
                if (line.Length >= 4)
                {
                    Int32.TryParse(line[2], out champLevel);

                    Int32.TryParse(line[3], out cdModifier);
                    
                }
                // if you only want to put teleport level OR modifier, you indicate by making the 3rd input negative, 
                // which then it will be the modifier. ex: "annie flash 10" = cdModifier 10, "annie flash -10" = champLevel 10
                else if (line.Length == 3)
                {
                    int thirdInput;
                    Int32.TryParse(line[2], out thirdInput);
                    if (thirdInput > 0) { cdModifier = thirdInput; }
                    else { champLevel = Math.Abs(thirdInput); }
                    
                }
            }

            if (cdModifier < 0) cdModifier = 0; //if modifier was inputted as negative, just make it 0

            // trigger the respective button based on the champNumber and spellNumber
            updateSpellButton(champNumber, spellNumber, cdModifier, champLevel);

        }

        //send a string in chat by pressing enter to open chat, virtually typing the string, and pressing enter to send
        private void sendString(string input)
        {
            System.Threading.Thread.Sleep(20);
            SendKeys.SendWait("{ENTER}");
            System.Threading.Thread.Sleep(20);
            SendKeys.SendWait(input);
            SendKeys.SendWait("{ENTER}");
        }

        //find the correct button for which trigger the timer
        private void updateSpellButton(int champNum, int spellNum, int cdModifier, int champLevel)
        {
            this.Dispatcher.Invoke(() => // since this is updating the UI from outside the UI thread, it has to use Dispatcher.Invoke()
            {
                //trigger timer using champion number, spell number, and button 
                countDownButton(champNum, spellNum, findSpellButton(champNum,spellNum), cdModifier, champLevel);
            });

        }

        // return button from 2 dimensional array of the spell buttons to make it easy to reference them 
        private System.Windows.Controls.Button findSpellButton(int champNum, int spellNum)
        {
            System.Windows.Controls.Button[,] spellButtons = {
                {champion1_spell1_cd, champion1_spell2_cd},
                {champion2_spell1_cd, champion2_spell2_cd},
                {champion3_spell1_cd, champion3_spell2_cd},
                {champion4_spell1_cd, champion4_spell2_cd},
                {champion5_spell1_cd, champion5_spell2_cd},
            };

            return spellButtons[champNum - 1, spellNum - 1];
        }

      
        // return a Summoner object from the API by searching based on summoner name and region
        private Summoner getSummoner(string summonerName, Region region)
        {
            try
            {
                //return suummoner name
                return api.Summoner.GetSummonerByNameAsync(region, summonerName).Result;

            }
            // if the summoner can't be found, throw an error, which brings us back to Page 1 with the message box updated
            catch (Exception )
            {
                Console.WriteLine("SUMMONER COULD NOT BE FOUND");
                throw new Exception("summoner could not be found or api key is incorrect");             

            }
        }

        // get the current game data from a summoner object and region, then parse that data into showing on the UI
        private async Task getCurrentGame(Summoner summoner, Region region)
        {
            try
            {
                // get current game object
                var game = await api.Spectator.GetCurrentGameAsync(region, summoner.Id);

                // get the participants list of the enemy team
                getEnemyTeam(game, summoner); 

                // turn the enemy team list into summonerGroups to be displayed in the GUI
                await parseEnemyTeam(enemyTeam);

            }
            // if summoner isn't in a live game or there was an issue parsing, throw an exception, bringing us back to page 1 
            catch (Exception)
            {
                Console.WriteLine("COULD NOT FIND CURRENT GAME");
               
                throw new Exception("summoner is not in live game or text file could not be found");
                
            }
        }

        // from a currentGame object and knowing the summoner of the player in question, find who is
        // on the enemy team, opposite the input summoner
        private void getEnemyTeam(CurrentGame currentGame, Summoner summoner)
        {
            // get full participants list
            var participants = currentGame.Participants;

            // find the ID for the ally team by finding the team ID of the input summoner (LINQ USAGE)
            long allyTeam = participants.First(s => s.SummonerId == summoner.Id).TeamId;

            // add players to the enemyTeam list based on that participant having a different team ID than the input summoner
            foreach (var player in participants)
            {
                if (player.TeamId != allyTeam)
                {
                    enemyTeam.Add(player);
                }
            }

        }

        // take in a list of participants and turn them into summonerGroup objects, add them to summonerGroups list
        private async Task parseEnemyTeam(List<CurrentGameParticipant> enemyTeam) 
        {
            // get the list of all summoner spell objects from the static data dragon api
            var spellsList = await staticApi.SummonerSpells.GetAllAsync(staticVersion);

            // parse each participant in enemyTeam into a summonerGroup object and add that to list
            foreach (var enemyPlayer in enemyTeam)
            {
                // the champion ID in a participant object is a long, but it needs to be an int to use in the static api
                int champId = unchecked((int)enemyPlayer.ChampionId);
                // find a champion object based on an ID
                var champion = await staticApi.Champions.GetByIdAsync(champId, staticVersion);    
       
                // find a spell object based on the ID of enemyPlayer's spell ID for spell 1 
                var spell1 = spellsList.SummonerSpells.First(s => s.Value.Id == enemyPlayer.SummonerSpell1).Value;

                // find a spell object based on the ID of enemyPlayer's spell ID for spell 2 
                var spell2 = spellsList.SummonerSpells.First(s => s.Value.Id == enemyPlayer.SummonerSpell2).Value;
               
                // take champion and spell objects + version of the static (class property) and make a SummonerGroup object to add to list
                summonerGroups.Add(new SummonerGroup(champion, spell1, spell2, staticVersion));

            }
        }

      
        // update a wpf control in the manner of a countdown timer using DispatchTimer and an anonymous action function
        // got this one online: https://social.msdn.microsoft.com/Forums/vstudio/en-US/25705cf9-693b-4c15-a549-ad198986406e/countdown-timer-in-wpf?forum=wpf
        private void countdown(float timerLength, TimeSpan interval, Action<float> ts)
        {
            // create dispatch timer and set interval
            var dispatchTimer = new System.Windows.Threading.DispatcherTimer();
            dispatchTimer.Interval = interval;

            // each tick of the timer subtract one until 0
            dispatchTimer.Tick += (_, a) =>
            {
                if (timerLength-- <= 0) { 
                timerLength = 0;
                dispatchTimer.Stop();
            }
                else
                    ts(timerLength);
            };
            ts(timerLength);
            dispatchTimer.Start();
        }

        // update button with countdown using Countdown function and information to find summonerGroup and button and cooldown
        private void countDownButton(int championNumber, int spellNumber, System.Windows.Controls.Button button, int modifier = 0, int champLevel = 0)
        {
            SummonerGroup summonerGroup = summonerGroups[championNumber - 1]; // find SummonerGroup based on champNumber
            float timerLength = 0;
            string spellName = "";

            
            // get the champ level if it's from 1-18, and set the timer length based on that
            if (champLevel > 0 && champLevel <= 18)
            {
                timerLength = (int)(430.588 - (10.588 * champLevel))-modifier;
                
            } else
            {
                // get the timer length and spell name based on the spell number
                switch (spellNumber)
                {
                    case 1:
                        timerLength = summonerGroup.spell1Cd - modifier;
                        spellName = summonerGroup.spell1Name;
                        break;
                    case 2:
                        timerLength = summonerGroup.spell2Cd - modifier;
                        spellName = summonerGroup.spell2Name;
                        break;

                }
            }

            if (button.Content.Equals("Activate")) // only run if the countdown isn't already in progress
            {
                //set color of button
                button.Background = redColor;

                //start to countdown on the button
                countdown(timerLength, TimeSpan.FromSeconds(1), time =>
                {
                    if (time > 0)
                    {
                        button.Content = time.ToString(); //update content of button to be the time left in the timer
                        //update the cooldown strings list by sending a cooldown string object created with
                        // champ name, spell name, and time left in the cooldown
                        updateCooldownStrings(new CooldownString(summonerGroup.champName, spellName, time.ToString()));
                    }
                    else
                    {
                        // when the timer runs out, make the button change to say 'activate' and be green
                        button.Content = "Activate";
                        button.Background = greenColor;
                    }
                });
                
            }
            
        }

        // set the images of champions and spells based on summonerGroups list
        private void setImageGroups()
        {
            // loop through summonerGroups and set elements accordingly
            for (int i = 0; i < summonerGroups.Count; i++)
            {
                switch (i)
                {
                    case 0:
                        champion1.Source = summonerGroups[i].champIcon;
                        champion1_spell1.Source = summonerGroups[i].spell1;
                        champion1_spell2.Source = summonerGroups[i].spell2;
                        break;
                    case 1:
                        champion2.Source = summonerGroups[i].champIcon;
                        champion2_spell1.Source = summonerGroups[i].spell1;
                        champion2_spell2.Source = summonerGroups[i].spell2;
                        break;
                    case 2:
                        champion3.Source = summonerGroups[i].champIcon;
                        champion3_spell1.Source = summonerGroups[i].spell1;
                        champion3_spell2.Source = summonerGroups[i].spell2;
                        break;
                    case 3:
                        champion4.Source = summonerGroups[i].champIcon;
                        champion4_spell1.Source = summonerGroups[i].spell1;
                        champion4_spell2.Source = summonerGroups[i].spell2;
                        break;
                    case 4:
                        champion5.Source = summonerGroups[i].champIcon;
                        champion5_spell1.Source = summonerGroups[i].spell1;
                        champion5_spell2.Source = summonerGroups[i].spell2;
                        break;
                    default:
                        Console.WriteLine("invalid champion number");
                        break;

                }
            }

        }


        // update the list of cooldown strings to be put into game chat and/or clipboard
        private void updateCooldownStrings(CooldownString cooldownString)
        {
           
             try
            {
                // check if there is a cooldownstring that matches the champion name and spell name,
                // and if so, make currCooldownString equal that element
                var currCooldownString = cooldownStrings.First(cds => 
                cds.champName.Equals(cooldownString.champName) && 
                cds.spellName.Equals(cooldownString.spellName));

                // find index of found element
                int index = cooldownStrings.IndexOf(currCooldownString);
                

                if (cooldownString.cooldown == "1") 
                {
                    // if the timer runs out, remove the element
                    cooldownStrings.RemoveAt(index);
                } else
                {
                    // if the timer is still going, update the cooldown of that element
                    cooldownStrings[index].cooldown = cooldownString.cooldown; 
                }
            }
            catch (Exception)
            {
              
                try
                {
                    // see if there is a cooldownstring that just matches the name of the inputted cooldownstring
                    // this is to find elements with the same name
                    var currCooldownString = cooldownStrings.First(cds =>
                        cds.champName.Equals(cooldownString.champName));

                    // get index of that found element
                    int index = cooldownStrings.IndexOf(currCooldownString);

                    // insert the new cooldown one below the found element, making it so elements with the same champion name 
                    // are next to each other
                    cooldownStrings.Insert(index + 1, cooldownString);
                }
                catch (Exception)
                {
                    // if there is not another cooldownstring with the same name as the new one, just add the new one to the end
                    cooldownStrings.Add(cooldownString);
                }
            }

            
             
            // convert the cooldownstrings list into a string
            cooldownsString = "";

             foreach (CooldownString cds in cooldownStrings)
             {
                // add each cooldown to the string
                cooldownsString += cds.toString() + "\n";
            }

            // add the cooldownstrings list to the clipboard for pasting, sometimes the function fails, so to avoid crashing, catch 
            // the exception
            try {
                System.Windows.Forms.Clipboard.SetText(cooldownsString);
            } catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            

        }


        // button event for the back button to bring us back to page 1 with the appropriate summoner name and status message and apikey and note file location
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Page1(summonerNameDisplay.Text, "summoner and game found successfully", returnApiKey, noteFileLocation, returnRegion));

        }

        // all the events for the spell cooldown buttons to start the timer for that button on click
        // there is probably a better way to do this
        private void Champion1_spell1_cd_Click(object sender, RoutedEventArgs e)
        {
            countDownButton(1, 1, champion1_spell1_cd);
        }

        private void Champion1_spell2_cd_Click(object sender, RoutedEventArgs e)
        {
            countDownButton(1, 2, champion1_spell2_cd);
        }

        private void Champion2_spell1_cd_Click(object sender, RoutedEventArgs e)
        {
            countDownButton(2, 1, champion2_spell1_cd);
        }

        private void Champion2_spell2_cd_Click(object sender, RoutedEventArgs e)
        {
            countDownButton(2, 2, champion2_spell2_cd);
        }

        private void Champion3_spell1_cd_Click(object sender, RoutedEventArgs e)
        {
            countDownButton(3, 1, champion3_spell1_cd);
        }

        private void Champion3_spell2_cd_Click(object sender, RoutedEventArgs e)
        {
            countDownButton(3, 2, champion3_spell2_cd);
        }

        private void Champion4_spell1_cd_Click(object sender, RoutedEventArgs e)
        {
            countDownButton(4, 1, champion4_spell1_cd);
        }

        private void Champion4_spell2_cd_Click(object sender, RoutedEventArgs e)
        {
            countDownButton(4, 2, champion4_spell2_cd);
        }

        private void Champion5_spell1_cd_Click(object sender, RoutedEventArgs e)
        {
            countDownButton(5, 1, champion5_spell1_cd);
        }

        private void Champion5_spell2_cd_Click(object sender, RoutedEventArgs e)
        {
            countDownButton(5, 2, champion5_spell2_cd);
        }
    }
}
