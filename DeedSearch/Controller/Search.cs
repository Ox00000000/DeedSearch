using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace DeedSearch
{
    public class Search
    {
        public static readonly int REQUEST_DELAY = 2000;
        public static readonly string PARTY_TYPE_GRANTOR = "1";
        public static readonly string PARTY_TYPE_GRANTEE = "0";

        private static HttpClient client = new HttpClient()
        { 
            Timeout = new TimeSpan(0, 0, 30)
        };
        private static Dictionary<string, string> cookies = new Dictionary<string, string>();

        public static async Task<string> GetSearchItems()
        {
            string retVal = "";
            try
            {
                HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, "https://search.gsccca.org/RealEstate/namesearch.asp");
                HttpResponseMessage response = await client.SendAsync(msg).ConfigureAwait(false);
                retVal = await response.Content.ReadAsStringAsync();

                Log.Debug($"{msg.RequestUri} status response: {response.StatusCode}");
            }
            catch (Exception e)
            {
                Log.Error(e, "Login exception");
            }

            return retVal;
        }

        public static async Task<string> GetNamesAsync(string searchName, 
                                                       string partyType, 
                                                       DateTime? fromDate, 
                                                       DateTime? toDate, 
                                                       string instrumentType, 
                                                       string county,
                                                       int pageNumber)
        {
            DataStore.Instance.SetFormContent(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("txtSearchType", "0"),
                new KeyValuePair<string, string>("txtPartyType", partyType),
                new KeyValuePair<string, string>("txtInstrCode", instrumentType),
                new KeyValuePair<string, string>("intCountyID", county),
                new KeyValuePair<string, string>("bolInclude", "0"),
                new KeyValuePair<string, string>("txtSearchName", searchName),
                new KeyValuePair<string, string>("txtFromDate", fromDate == null ? "" : ((DateTime)fromDate).ToShortDateString()),
                new KeyValuePair<string, string>("txtToDate", toDate == null ? "" : ((DateTime)toDate).ToShortDateString()),
                new KeyValuePair<string, string>("MaxRows", "100"),
                new KeyValuePair<string, string>("TableType", "1"),
                new KeyValuePair<string, string>("ShowCaptcha", "False"),

                new KeyValuePair<string, string>("dtSystemEnd", DateTime.Today.AddDays(-1).ToShortDateString()),
                new KeyValuePair<string, string>("dtSystemStart", "12/31/1871"),
                new KeyValuePair<string, string>("dtSysGoodFrom", "1/1/1990"),
                new KeyValuePair<string, string>("dtSysGoodThru", "3/12/2020 4:31 PM"),
                //new KeyValuePair<string, string>("dtCurrSearchTime", "5/5/2020 8:11:04 PM")
                new KeyValuePair<string, string>("dtCurrSearchTime", $"{DateTime.UtcNow.ToShortDateString()} {DateTime.UtcNow.ToShortTimeString()}")
            }, partyType);

            string retVal = "";

            try
            {
                HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Post, $"https://search.gsccca.org/RealEstate/names.asp?Type=0maxrows=100&page={pageNumber}")
                {
                    Content = new FormUrlEncodedContent(DataStore.Instance.GetFormContent(partyType))
                };

                AddCookies(msg);

                HttpResponseMessage response = await client.SendAsync(msg).ConfigureAwait(false);
                retVal = await response.Content.ReadAsStringAsync();

                Log.Debug($"{msg.RequestUri} status response: {response.StatusCode}");
            }
            catch (Exception e)
            {
                Log.Error(e, "Login exception");
            }

            return retVal;
        }

        public static async Task<string> LoginAsync(string username, string password, bool createNewClient = false)
        {
            string retVal = "";

            // Possibly need a new client if previous logins failed
            if (createNewClient)
            {
                client = new HttpClient()
                {
                    Timeout = new TimeSpan(0, 0, 30)
                };
            }

            try
            {
                HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Post, "https://apps.gsccca.org/login.asp")
                {
                    Content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
                    {
                        new KeyValuePair<string, string>("txtUserID", username),
                        new KeyValuePair<string, string>("txtPassword", password)
                    })
                };

                HttpResponseMessage response = await client.SendAsync(msg).ConfigureAwait(false);
                retVal = await response.Content.ReadAsStringAsync();

                foreach (string cookieValue in response.Headers.GetValues("Set-Cookie"))
                {
                    string[] items = cookieValue.Split("; ");
                    string[] cookie = items[0].Split("=");
                    cookies.Add(cookie[0], cookie[1]);
                }

                Log.Debug($"{msg.RequestUri} status response: {response.StatusCode}");
            }
            catch (Exception e)
            {
                Log.Error(e, "Login exception");
            }

            return retVal;
        }

        public static async Task<string> GetDeedListAsync(string originalSearch, string name, string countyName, string partyType, int pageNumber)
        {
            // Clean up any previous searches
            DataStore.Instance.GetFormContent(partyType).Remove(DataStore.Instance.GetFormContent(partyType).Find(i => i.Key == "rdoEntityName"));
            DataStore.Instance.GetFormContent(partyType).Add(new KeyValuePair<string, string>("rdoEntityName", name));

            // Change search dates if they already exist
            if (DataStore.Instance.GetFormContent(partyType).Exists(i => i.Key == "txtFromDate"))
            {
                KeyValuePair<string, string> fromDate = DataStore.Instance.GetFormContent(partyType).Find(i => i.Key == "txtFromDate");
                DataStore.Instance.GetFormContent(partyType).Add(new KeyValuePair<string, string>("dtStartDate", fromDate.Value));
                DataStore.Instance.GetFormContent(partyType).Remove(fromDate);
            }
            if (DataStore.Instance.GetFormContent(partyType).Exists(i => i.Key == "txtFromDate"))
            {
                KeyValuePair<string, string> toDate = DataStore.Instance.GetFormContent(partyType).Find(i => i.Key == "txtToDate");
                DataStore.Instance.GetFormContent(partyType).Add(new KeyValuePair<string, string>("dtEndDate", toDate.Value));
                DataStore.Instance.GetFormContent(partyType).Remove(toDate);
            }

            string retVal = "";
            try
            {
                HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Post, $"https://search.gsccca.org/RealEstate/nameselected.asp?page={pageNumber}&maxrows=100")
                {
                    Content = new FormUrlEncodedContent(DataStore.Instance.GetFormContent(partyType))
                };
                AddCookies(msg);

                HttpResponseMessage response = await client.SendAsync(msg).ConfigureAwait(false);
                retVal = await response.Content.ReadAsStringAsync();
                
                Log.Debug($"{msg.RequestUri} status response: {response.StatusCode}");
            }
            catch (Exception e)
            {
                Log.Error(e, "Login exception");
            }


            return retVal;
        }

        public static async Task<string> GetDeedAsync(string url, string partyType)
        {
            //DataStore.Instance.FormContent.Add(new KeyValuePair<string, string>("Redirect", "/realestate/nameselected.asp"));
            //DataStore.Instance.FormContent.Add(new KeyValuePair<string, string>("sFormAction", "DeedNames"));

            // Delay put in place to not get flagged as a robot
            await Task.Delay(REQUEST_DELAY);

            string retVal = "";
            try
            {
                HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, "https://search.gsccca.org/RealEstate/" + url)
                {
                    Content = new FormUrlEncodedContent(DataStore.Instance.GetFormContent(partyType))
                };

                AddCookies(msg);

                HttpResponseMessage response = await client.SendAsync(msg).ConfigureAwait(false);
                retVal = await response.Content.ReadAsStringAsync();

                Log.Debug($"{msg.RequestUri} status response: {response.StatusCode}");
            }
            catch (Exception e)
            {
                Log.Error(e, "Login exception");
            }

            return retVal;
        }

        private static void AddCookies(HttpRequestMessage msg)
        {
            string cookieList = "";
            foreach (KeyValuePair<string, string> cookie in cookies)
            {
                cookieList += $"{cookie.Key}={cookie.Value}; ";
            }
            msg.Headers.Add("Cookie", cookieList);
        }
    }
}
