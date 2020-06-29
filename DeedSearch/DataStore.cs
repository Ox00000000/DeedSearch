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

        public List<Deed> DeedSearchResults;
        public HashSet<Deed> DeedResults;

        public List<KeyValuePair<string, string>> InstrumentTypes;
        public List<KeyValuePair<string, string>> Counties;

        public List<KeyValuePair<string, string>> GrantorFormContent;
        public List<KeyValuePair<string, string>> GranteeFormContent;

        public string Username;
        public string Password;

        private int logId = 0;

        private DataStore()
        {
            GrantorResults = new List<NameSearchResult>();
            GranteeResults = new List<NameSearchResult>();
            DeedSearchResults = new List<Deed>();
            DeedResults = new HashSet<Deed>();
            InstrumentTypes = new List<KeyValuePair<string, string>>();
            Counties = new List<KeyValuePair<string, string>>();
            GrantorFormContent = new List<KeyValuePair<string, string>>();
            GranteeFormContent = new List<KeyValuePair<string, string>>();

            SelectedGrantors = new List<NameSearchResult>();
            SelectedGrantees = new List<NameSearchResult>();
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

            if (grantorHits.Count > 0 && granteeHits.Count > 0)
            {
                this.DeedResults.Add(deed);
            }

            return grantorHits.Count > 0 && granteeHits.Count > 0;
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
    }
}
