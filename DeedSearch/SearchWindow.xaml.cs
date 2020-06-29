using DeedSearch.Data;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DeedSearch
{
    /// <summary>
    /// Interaction logic for Search.xaml
    /// </summary>
    public partial class SearchWindow : MetroWindow
    {
        private ProgressDialogController RecordsSearchProgress;
        private ProgressDialogController DeedsSearchProgress;
        private bool cancelDeedsSearch;

        public SearchWindow()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File($"Logs\\{Assembly.GetExecutingAssembly().GetName().Name}.log", 
                    rollingInterval: RollingInterval.Day, outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
            Log.Information("===================");
            Log.Information("Application startup");
            Log.Information("===================");

            this.Loaded += SearchWindow_Loaded;

            InitializeComponent();

            this.ResultsList.SelectionChanged += ResultsList_SelectionChanged;
            this.GrantorName.Focus();

            /*
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.Load(@"C:\Users\tsmit\Documents\Sample Code\deedResultsFromRequest.html");
            GSCCCAPage page = new GSCCCAPage(doc.Text);
            page.DeedResult();
            */


            int i = 0;
        }

        private async void SearchWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string result = await Search.GetSearchItems();

            GSCCCAPage page = new GSCCCAPage(result);
            page.GetSearchItems();

            this.InstrumentType.ItemsSource = DataStore.Instance.InstrumentTypes;
            this.InstrumentType.SelectedItem = DataStore.Instance.InstrumentTypes.Find(i => i.Key.ToLower().Contains("all"));

            this.Counties.ItemsSource = DataStore.Instance.Counties;
            this.Counties.SelectedItem = DataStore.Instance.Counties.Find(i => i.Key.ToLower().Contains("all"));
        }

        private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Deed deed = ((sender as ListBox).SelectedItem as Deed);

            this.GrantorResultList.ItemsSource = deed.Grantor;
            this.GranteeResultList.ItemsSource = deed.Grantee;
            this.CountyResult.Text = deed.County;
            this.BookResult.Text = deed.Book;
            this.PageResult.Text = deed.Page;
            this.IssueResult.Text = deed.DateFiled;
            this.TimeResult.Text = deed.Time;
            this.InstrumentResult.Text = deed.InstrumentType;
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Starting search");
            Log.Information($"Search criteria: Grantor {this.GrantorName.Text}, Grantee {this.GranteeName.Text}, Date {this.FromDate.SelectedDate} - {this.ToDate.SelectedDate}, Instrument {this.InstrumentType.SelectedValue as string}, County {this.Counties.SelectedValue as string}");

            this.RecordsSearchProgress = await this.ShowProgressAsync("Searching Records", "Please wait");
            this.RecordsSearchProgress.SetIndeterminate();

            await this.SearchForName(this.GrantorName.Text,
                                     Search.PARTY_TYPE_GRANTOR,
                                     this.FromDate.SelectedDate,
                                     this.ToDate.SelectedDate,
                                     this.InstrumentType.SelectedValue as string,
                                     this.Counties.SelectedValue as string);
            this.GrantorList.ItemsSource = DataStore.Instance.GrantorResults;

            if (!String.IsNullOrWhiteSpace(this.GranteeName.Text))
            {
                await this.SearchForName(this.GranteeName.Text,
                                         Search.PARTY_TYPE_GRANTEE,
                                         this.FromDate.SelectedDate,
                                         this.ToDate.SelectedDate,
                                         this.InstrumentType.SelectedValue as string,
                                         this.Counties.SelectedValue as string);
                this.GranteeList.ItemsSource = DataStore.Instance.GranteeResults;
            }

            await this.RecordsSearchProgress.CloseAsync();

            this.NavigationTab.SelectedIndex = 1;
        }

        private async Task SearchForName(string name, string partyType, DateTime? fromDate, DateTime? toDate, string instrumentType, string county)
        {
            string result = await Search.GetNamesAsync(name, partyType, fromDate, toDate, instrumentType, county);

            GSCCCAPage page = new GSCCCAPage(result);

            if (page.IsLoginRequired())
            {
                Login login = new Login();
                login.Owner = this;
                login.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                login.ShowDialog();

                await this.SearchForName(name, partyType, fromDate, toDate, instrumentType, county);
            }
            else
            {
                if (name == this.GrantorName.Text)
                {
                    DataStore.Instance.GrantorResults = page.NamesSearch();
                }
                else if (name == this.GranteeName.Text)
                {
                    DataStore.Instance.GranteeResults = page.NamesSearch();
                }
            }
        }

        private void Details_Clicks(object sender, RoutedEventArgs e)
        {
            Log.Information("Searching deeds");

            //List<NameSearchResult> selectedGrantors = this.GrantorList.SelectedItems as List<NameSearchResult>;
            //List<NameSearchResult> selectedGrantees = this.GranteeList.SelectedItems as List<NameSearchResult>;

            //this.GetDetails(selectedGrantors, selectedGrantees);
            this.GetDetails( );
        }

        //private async void GetDetails(List<NameSearchResult> selectedGrantors, List<NameSearchResult> selectedGrantees)
        private async void GetDetails()
        {
            //Log.Information($"Search criteria: Grantors {(selectedGrantors != null ? string.Join(", ", selectedGrantors) : "")} Grantees {(selectedGrantees != null ? string.Join(", ", selectedGrantees) : "")}");

            this.DeedsSearchProgress = await this.ShowProgressAsync("Searching Deeds", "Calculating time the search may take...");
            this.DeedsSearchProgress.SetIndeterminate();

            // Clear any previous searchs
            DataStore.Instance.DeedSearchResults.Clear();
            int errors = 0;

            // Check our list of grantor deeds
            List<SelectedNameResult> grantorSearchResults = new List<SelectedNameResult>();

            if (null != this.GrantorList.SelectedItems)
            {
                foreach (NameSearchResult selected in this.GrantorList.SelectedItems)
                {
                    Log.Information($"Search criteria: Grantor {selected}");
                    DataStore.Instance.SelectedGrantors.Add(selected);

                    // Retrieve list of deeds from selected grantor
                    string deedListResponse = await Search.GetDeedListAsync(this.GrantorName.Text, 
                                                                            selected.Name, 
                                                                            this.Counties.Text, 
                                                                            Search.PARTY_TYPE_GRANTOR);
                    GSCCCAPage page = new GSCCCAPage(deedListResponse);

                    if (page.IsLoginRequired())
                    {
                        Login login = new Login();
                        login.Owner = this;
                        login.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        login.ShowDialog();

                        //this.GetDetails(selectedGrantors, selectedGrantees);
                        this.GetDetails();
                    }
                    else
                    {
                        grantorSearchResults.AddRange(page.DeedSearch());
                    }
                }
            }
            Log.Debug($"Found {grantorSearchResults.Count} total grantor results");

            // Check our list of grantee deeds
            List<SelectedNameResult> granteeSearchResults = new List<SelectedNameResult>();

            if (null != this.GranteeList.SelectedItems)
            {
                foreach (NameSearchResult selected in this.GranteeList.SelectedItems)
                {
                    Log.Information($"Search criteria: Grantor {selected}");
                    DataStore.Instance.SelectedGrantees.Add(selected);

                    // Retrieve list of deeds from selected grantor
                    string deedListResponse = await Search.GetDeedListAsync(this.GranteeName.Text, 
                                                                            selected.Name, 
                                                                            this.Counties.Text, 
                                                                            Search.PARTY_TYPE_GRANTEE);
                    GSCCCAPage page = new GSCCCAPage(deedListResponse);
                    granteeSearchResults.AddRange(page.DeedSearch());
                }
            }
            Log.Debug($"Found {granteeSearchResults.Count} total grantee results");

            // Calculate time required for seach
            int recordsToSearch = grantorSearchResults.Count + granteeSearchResults.Count;
            TimeSpan seachTime = TimeSpan.FromMilliseconds(Search.REQUEST_DELAY * recordsToSearch);

            // Update our message display to let the user know how long it may take
            this.DeedsSearchProgress.SetMessage($"Search will take at least {seachTime.ToString(@"mm\:ss")} minutes");
            this.DeedsSearchProgress.Maximum = recordsToSearch;
            this.DeedsSearchProgress.SetCancelable(true);
            this.DeedsSearchProgress.Canceled += DeedsSearchProgress_Canceled;
            cancelDeedsSearch = false;
            int recordsCompleted = 0;

            // Query each grantor deed
            foreach (SelectedNameResult deedResult in grantorSearchResults)
            {
                if (cancelDeedsSearch)
                {
                    Log.Information("Deed search cancelled");
                    break;
                }

                string deedResponse = await Search.GetDeedAsync(deedResult.DeedUrl, Search.PARTY_TYPE_GRANTOR);
                //DataStore.Instance.writeToFile(deedResponse);
                GSCCCAPage deedPage = new GSCCCAPage(deedResponse);

                // Check if we're been caught and flagged as non-human or a free account
                if (!deedPage.IsRecaptchaPresent() &&
                    !deedPage.IsFreeAccount())
                {
                    Deed deed = deedPage.DeedResult();
                    // TODO Remove? DataStore.Instance.DeedSearchResults.Add(deed);

                    DataStore.Instance.CheckHit(deed);
                }
                else
                {
                    errors++;
                    this.Notification.Text = $"Failed retrieving {errors} deeds";
                    this.Notification.Foreground = Brushes.Red;

                    Log.Warning($"Failed retrieving deed at {deedResult.DeedUrl}");
                }

                recordsCompleted++;
                this.DeedsSearchProgress.SetProgress(recordsCompleted);
            }

            // Query each grantee deed
            foreach (SelectedNameResult deedResult in granteeSearchResults)
            {
                if (cancelDeedsSearch)
                {
                    Log.Information("Deed search cancelled");
                    break;
                }

                string deedResponse = await Search.GetDeedAsync(deedResult.DeedUrl, Search.PARTY_TYPE_GRANTEE);
                GSCCCAPage deedPage = new GSCCCAPage(deedResponse);

                // Check if we're been caught and flagged as non-human or a free account
                if (!deedPage.IsRecaptchaPresent() &&
                    !deedPage.IsFreeAccount())
                {
                    Deed deed = deedPage.DeedResult();
                    DataStore.Instance.DeedSearchResults.Add(deed);

                    DataStore.Instance.CheckHit(deed);
                }
                else
                {
                    errors++;
                    this.Notification.Text = $"Failed retrieving {errors} deeds";
                    this.Notification.Foreground = Brushes.Red;

                    Log.Warning($"Failed retrieving deed at {deedResult.DeedUrl}");
                }

                recordsCompleted++;
                this.DeedsSearchProgress.SetProgress(recordsCompleted);
            }

            await this.DeedsSearchProgress.CloseAsync();

            this.ResultsList.ItemsSource = DataStore.Instance.DeedResults;

            this.SearchesTextBlock.Text = $"Searched: {recordsToSearch}";
            this.HitsTextBlock.Text = $"Hits: {DataStore.Instance.DeedResults.Count}";
            this.ErrorstextBlock.Text = $"Errors: {errors}";

            Log.Information($"Searched: {recordsToSearch}, Hits: {DataStore.Instance.DeedResults.Count}, Errors: {errors}");

            // Move to our final tab to display results
            this.NavigationTab.SelectedIndex = 2;
        }
        
        private void DeedsSearchProgress_Canceled(object sender, EventArgs e)
        {
            this.cancelDeedsSearch = true;
        }

        private void Image_Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.ResultsList.SelectedItem != null)
            {
                Deed deed = this.ResultsList.SelectedItem as Deed;

                if (!String.IsNullOrWhiteSpace(deed.ImageUrl))
                {
                    Log.Information($"Opening deed: {deed.ImageUrl}");

                    DeedImageWindow deedImage = new DeedImageWindow();
                    deedImage.Source = new Uri(deed.ImageUrl);
                    deedImage.Show();

                    //System.Diagnostics.Process.Start(deed.ImageUrl.Replace("&", "^&"));
                }
            }
        }

        private async void About_Click(object sender, RoutedEventArgs e)
        {
            await this.ShowMessageAsync("About", $"Developed by Tommy Smith\n© 2020 - {DateTime.Now.Year}\n\nVersion: {Assembly.GetExecutingAssembly().GetName().Version}");
        }
    }
}
