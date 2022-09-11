using DeedSearch.Data;
using System;
using System.Collections.Generic;

namespace DeedSearch
{
    public sealed class DataStore
    {
        private static readonly Lazy<DataStore> lazy = new Lazy<DataStore>(() => new DataStore());
                
        public List<NameSearchResult> GrantorResults;
        public List<NameSearchResult> GranteeResults;
        public List<NameSearchResult> SelectedGrantors;
        public List<NameSearchResult> SelectedGrantees;

        public HashSet<Deed> DeedHitResults;

        public List<KeyValuePair<string, string>> InstrumentTypes;
        public List<KeyValuePair<string, string>> Counties;


        public string Username;
        public string Password;

        private List<KeyValuePair<string, string>> GrantorFormContent;
        private List<KeyValuePair<string, string>> GranteeFormContent;
        private int logId = 0;

        private DataStore()
        {
            // Loaded from GSCCCA on init
            InstrumentTypes = new List<KeyValuePair<string, string>>();
            Counties = new List<KeyValuePair<string, string>>();

            // Stores HTML form data for subsequent requests
            GrantorFormContent = new List<KeyValuePair<string, string>>();
            GranteeFormContent = new List<KeyValuePair<string, string>>();

            // Search results from initial name queries
            GrantorResults = new List<NameSearchResult>();
            GranteeResults = new List<NameSearchResult>();

            // User selected list to perform search on
            SelectedGrantors = new List<NameSearchResult>();
            SelectedGrantees = new List<NameSearchResult>();

            // Deed hit results
            DeedHitResults = new HashSet<Deed>();
        }

        public static DataStore Instance
        {
            get
            {
                return lazy.Value;
            }
        }

        public bool CheckHit(Deed deed)
        {
            List<NameSearchResult> grantorHits = this.SelectedGrantors.FindAll(g => deed.Grantor.Contains(g.Name));
            List<NameSearchResult> granteeHits = this.SelectedGrantees.FindAll(g => deed.Grantee.Contains(g.Name));

            bool resultFound = grantorHits.Count > 0 && granteeHits.Count > 0;
            if (resultFound)
            {
                this.DeedHitResults.Add(deed);
            }

            return resultFound;
        }

        public List<Deed> QuickHitCheck(List<Deed> grantorDeeds, List<Deed> granteeDeeds)
        {
            // TODO Switch to using this.DeedHitResults?
            List<Deed> results = new List<Deed>();
            foreach (Deed grantorDeed in grantorDeeds)
            {
                List<Deed> hits = granteeDeeds.FindAll(g => grantorDeed.Book == g.Book && grantorDeed.Page == g.Page);
                results.AddRange(hits);
            }

            return results;
        }
        
        public void writeToFile(string html)
        {
            System.IO.File.WriteAllText(@"C:\Users\tsmit\Documents\Sample Code\Results\Result" + logId + ".html", html);
            logId++;
        }

        public List<KeyValuePair<string, string>> GetFormContent(string party)
        {
            List<KeyValuePair<string, string>> retVal = new List<KeyValuePair<string, string>>();

            if (party == Search.PARTY_TYPE_GRANTOR)
            {
                retVal = this.GrantorFormContent;
            }
            else if (party == Search.PARTY_TYPE_GRANTEE)
            {
                retVal = this.GranteeFormContent;
            }

            return retVal;
        }

        public void SetFormContent(List<KeyValuePair<string, string>> formContent, string party)
        {
            if (party == Search.PARTY_TYPE_GRANTOR)
            {
                this.GrantorFormContent = formContent;
            }
            else if (party == Search.PARTY_TYPE_GRANTEE)
            {
                this.GranteeFormContent = formContent;
            }
        }

        public List<KeyValuePair<int, string>> SearchStyle
        {
            get
            {
                return new List<KeyValuePair<int, string>>
                {
                    new KeyValuePair<int, string>(0, "Quick Search"),
                    new KeyValuePair<int, string>(1, "Deep Search"),
                    new KeyValuePair<int, string>(2, "Description Search")
                };
            }
        }
    }
}
