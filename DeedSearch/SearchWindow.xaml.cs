using DeedSearch.Data;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
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

        private TimeSpan searchTime;
        private TimeSpan countdownTime;

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

            /*
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.Load(@"C:\Users\tsmit\Documents\Sample Code\deedResultsFromRequest.html");
            GSCCCAPage page = new GSCCCAPage(doc.Text);
            page.DeedResult();
            

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.Load(@"C:\Users\tsmit\Documents\Sample Code\DeedSummaryRegular.html");
            GSCCCAPage page = new GSCCCAPage(doc.Text);
            //page.DeedSummary();
            
            HtmlAgilityPack.HtmlDocument doc2 = new HtmlAgilityPack.HtmlDocument();
            doc2.Load(@"C:\Users\tsmit\Documents\Sample Code\DeedSummary1Line.html");
            GSCCCAPage page2 = new GSCCCAPage(doc2.Text);
            page2.DeedSummary();

            int i = 0;
            */
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

            this.SearchStyle.ItemsSource = DataStore.Instance.SearchStyle;
            this.SearchStyle.SelectedIndex = 0;

            this.GrantorName.Focus();
        }

        private void ResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Deed deed = ((sender as ListBox).SelectedItem as Deed);

            if (null != deed)
            {
                this.GrantorResultList.ItemsSource = deed.Grantor;
                this.GranteeResultList.ItemsSource = deed.Grantee;
                this.CountyResult.Text = deed.County;
                this.BookResult.Text = deed.Book;
                this.PageResult.Text = deed.Page;
                this.IssueResult.Text = deed.DateFiled;
                this.TimeResult.Text = deed.Time;
                this.InstrumentResult.Text = deed.InstrumentType;
            }
        }

        private async void SearchParty_Click(object sender, RoutedEventArgs e)
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
            string result = await Search.GetNamesAsync(name, partyType, fromDate, toDate, instrumentType, county, 1);

            GSCCCAPage page = new GSCCCAPage(result);
            int pageNumbers = page.GetNumberOfPageResults();

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
                    
                    // If there are more pages, parse those too
                    for (int i = 2; i <= pageNumbers; i++)
                    {
                        result = await Search.GetNamesAsync(name, partyType, fromDate, toDate, instrumentType, county, i);
                        page = new GSCCCAPage(result);
                        DataStore.Instance.GrantorResults.AddRange(page.NamesSearch());
                    }
                }
                else if (name == this.GranteeName.Text)
                {
                    DataStore.Instance.GranteeResults = page.NamesSearch();

                    // If there are more pages, parse those too
                    for (int i = 2; i <= pageNumbers; i++)
                    {
                        result = await Search.GetNamesAsync(name, partyType, fromDate, toDate, instrumentType, county, i);
                        page = new GSCCCAPage(result);
                        DataStore.Instance.GranteeResults.AddRange(page.NamesSearch());
                    }
                }
            }
        }

        private async void SearchDeeds_Click(object sender, RoutedEventArgs e)
        {
            KeyValuePair<int, string> searchStyle = (KeyValuePair<int, string>) this.SearchStyle.SelectedItem;

            Log.Information($"Searching deeds with {searchStyle.Value}");

            this.DeedsSearchProgress = await this.ShowProgressAsync("Searching Deeds", "Calculating time the search may take...");
            this.DeedsSearchProgress.SetIndeterminate();

            int errors = 0;
            int recordsCompleted = 0;

            // Check our list of grantor deeds
            List<Deed> grantorSearchResults = new List<Deed>();
            int grantorNumberOfPages = 0;
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
                                                                            Search.PARTY_TYPE_GRANTOR,
                                                                            1);
                    GSCCCAPage page = new GSCCCAPage(deedListResponse);
                    int pageNumbers = page.GetNumberOfPageResults();
                    selected.NumberOfPages = pageNumbers;
                    grantorNumberOfPages += pageNumbers;

                    if (page.IsLoginRequired())
                    {
                        Login login = new Login();
                        login.Owner = this;
                        login.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        login.ShowDialog();

                        this.SearchDeeds_Click(sender, e);
                    }
                    else
                    {
                        // Parse first page of results
                        grantorSearchResults.AddRange(page.DeedSummary());
                        recordsCompleted++;
                    }
                }
            }
            
            // Check our list of grantee deeds
            List<Deed> granteeSearchResults = new List<Deed>();
            int granteeNumberOfPages = 0;
            if (null != this.GranteeList.SelectedItems)
            {
                foreach (NameSearchResult selected in this.GranteeList.SelectedItems)
                {
                    Log.Information($"Search criteria: Grantee {selected}");
                    DataStore.Instance.SelectedGrantees.Add(selected);

                    // Retrieve list of deeds from selected grantee
                    string deedListResponse = await Search.GetDeedListAsync(this.GranteeName.Text,
                                                                            selected.Name,
                                                                            this.Counties.Text,
                                                                            Search.PARTY_TYPE_GRANTEE,
                                                                            1);
                    GSCCCAPage page = new GSCCCAPage(deedListResponse);
                    int pageNumbers = page.GetNumberOfPageResults();
                    selected.NumberOfPages = pageNumbers;
                    granteeNumberOfPages += pageNumbers;

                    // Parse first page of results
                    granteeSearchResults.AddRange(page.DeedSummary());
                    recordsCompleted++;
                }
            }

            // Estimate time required for seach
            int pagesToSearch = grantorNumberOfPages + granteeNumberOfPages;
            int recordsToSearch = 0;
            recordsToSearch += grantorNumberOfPages > 1 ? grantorNumberOfPages * Search.MAX_ROWS : grantorSearchResults.Count;
            recordsToSearch += granteeNumberOfPages > 1 ? granteeNumberOfPages * Search.MAX_ROWS : granteeSearchResults.Count;
            int estimatedRecordsToSearch = 0;
            switch (searchStyle.Key)
            {
                case 0:
                    // Quick search only checks the summary pages
                    estimatedRecordsToSearch = pagesToSearch;
                    break;
                case 1:
                    // Deep search looks at each record
                    estimatedRecordsToSearch = recordsToSearch;
                    break;
                case 2:
                    break;
            }

            // Calculate search time with our artificial delay
            this.searchTime = TimeSpan.FromMilliseconds(Search.REQUEST_DELAY * estimatedRecordsToSearch).Add(new TimeSpan(0, 0, 1));
            this.countdownTime = this.searchTime;

            // Update our message display to let the user know how long it may take
            this.DeedsSearchProgress.SetMessage($"Search will take at least {this.searchTime.ToString(@"mm\:ss")} minutes");
            this.DeedsSearchProgress.Maximum = estimatedRecordsToSearch;
            this.DeedsSearchProgress.SetCancelable(true);
            this.DeedsSearchProgress.Canceled += DeedsSearchProgress_Canceled;
            this.DeedsSearchProgress.SetProgress(recordsCompleted);
            cancelDeedsSearch = false;

            // Start an update timer
            Timer timer = new Timer(1000);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            // Check additional pages of deed summaries
            if (null != this.GrantorList.SelectedItems)
            {
                grantorSearchResults.AddRange(await this.queryDeedSummaryPageAsync(this.GrantorList.SelectedItems.Cast<NameSearchResult>().ToList(),
                                                                                   this.GrantorName.Text,
                                                                                   Search.PARTY_TYPE_GRANTOR,
                                                                                   recordsCompleted));
            }
            Log.Debug($"Found {grantorSearchResults.Count} total grantor results");

            // Check additional pages of deed summaries
            if (null != this.GranteeList.SelectedItems)
            {
                granteeSearchResults.AddRange(await this.queryDeedSummaryPageAsync(this.GranteeList.SelectedItems.Cast<NameSearchResult>().ToList(),
                                                                                   this.GranteeName.Text,
                                                                                   Search.PARTY_TYPE_GRANTEE,
                                                                                   recordsCompleted));
            }
            Log.Debug($"Found {granteeSearchResults.Count} total grantee results");

            recordsToSearch = grantorSearchResults.Count + granteeSearchResults.Count;

            switch (searchStyle.Key)
            {
                case 0:
                    List<Deed> results = DataStore.Instance.QuickHitCheck(grantorSearchResults, granteeSearchResults);
                    
                    await this.queryDeedPageAsync(results, Search.PARTY_TYPE_GRANTOR, recordsCompleted, errors);

                    this.ResultsList.ItemsSource = DataStore.Instance.DeedHitResults;
                    break;
                case 1:
                    this.DeedsSearchProgress.Maximum = grantorSearchResults.Count + 
                                                       granteeSearchResults.Count + 
                                                       grantorNumberOfPages + 
                                                       granteeNumberOfPages;
                    // Query each deed
                    await this.queryDeedPageAsync(grantorSearchResults, Search.PARTY_TYPE_GRANTOR, recordsCompleted, errors);
                    await this.queryDeedPageAsync(granteeSearchResults, Search.PARTY_TYPE_GRANTEE, recordsCompleted, errors);

                    this.ResultsList.ItemsSource = DataStore.Instance.DeedHitResults;
                    break;
                case 2:
                    break;
            }

            // Search completed
            timer.Stop();
            await this.DeedsSearchProgress.CloseAsync();

            this.SearchesTextBlock.Text = $"Searched: {recordsToSearch}";
            this.HitsTextBlock.Text = $"Hits: {DataStore.Instance.DeedHitResults.Count}";
            this.ErrorstextBlock.Text = $"Errors: {errors}";

            Log.Information($"Estimated: {estimatedRecordsToSearch}, To Search: {recordsToSearch}, Pages Searched: {recordsCompleted}, Hits: {DataStore.Instance.DeedHitResults.Count}, Errors: {errors}");

            // Move to our final tab to display results
            this.NavigationTab.SelectedIndex = 2;
        }

        private async Task<List<Deed>> queryDeedSummaryPageAsync(List<NameSearchResult> selectedItems, 
                                                                 string originalSearch,
                                                                 string searchType,
                                                                 int recordsCompleted)
        {
            List<Deed> retVal = new List<Deed>();
            
            foreach (NameSearchResult selected in selectedItems)
            {
                if (cancelDeedsSearch)
                {
                    Log.Information("Deed search cancelled");
                    break;
                }

                // If there are more pages, parse those too
                for (int i = 2; i <= selected.NumberOfPages; i++)
                {
                    string deedListResponse = await Search.GetDeedListAsync(originalSearch,
                                                                            selected.Name,
                                                                            this.Counties.Text,
                                                                            searchType,
                                                                            i);

                    GSCCCAPage page = new GSCCCAPage(deedListResponse);
                    retVal.AddRange(page.DeedSummary());
                    recordsCompleted++;
                    this.DeedsSearchProgress.SetProgress(recordsCompleted);
                }
            }

            return retVal;
        }

        private async Task queryDeedPageAsync(List<Deed> deeds, string searchType, int recordsCompleted, int errors)
        {
            // Query each deed details page
            foreach (Deed deedResult in deeds)
            {
                if (cancelDeedsSearch)
                {
                    Log.Information("Deed search cancelled");
                    break;
                }

                string deedResponse = await Search.GetDeedAsync(deedResult.DeedPageUrl, searchType);
                //DataStore.Instance.writeToFile(deedResponse);
                GSCCCAPage deedPage = new GSCCCAPage(deedResponse);

                // Check if we're been caught and flagged as non-human or a free account
                if (!deedPage.IsRecaptchaPresent() &&
                    !deedPage.IsFreeAccount())
                {
                    Deed deed = deedPage.DeedResult();
                    DataStore.Instance.CheckHit(deed);
                }
                else
                {
                    errors++;
                    this.Notification.Text = $"Failed retrieving {errors} deeds";
                    this.Notification.Foreground = Brushes.Red;

                    Log.Warning($"Failed retrieving deed at {deedResult.DeedPageUrl}");
                }

                recordsCompleted++;
                this.DeedsSearchProgress.SetProgress(recordsCompleted);
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (this.countdownTime > new TimeSpan(0, 0, 0))
            {
                this.countdownTime = this.countdownTime.Subtract(new TimeSpan(0, 0, 1));
            }
            this.DeedsSearchProgress.SetMessage($"Search will take at least {this.searchTime.ToString(@"mm\:ss")} minutes\nEstimated completion: {this.countdownTime.ToString(@"mm\:ss")} minutes");
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
