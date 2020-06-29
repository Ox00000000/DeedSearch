using DeedSearch.Data;
using HtmlAgilityPack;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DeedSearch
{
    public class GSCCCAPage
    {
        private const string ID_FORM_LOGIN = "frmLogin";
        private const string ID_INSTRUMENT_TYPE = "txtInstrCode";
        private const string ID_COUNTIES = "intCountyID";
        private const string ID_NAMES_SEARCH = "name_results";
        private const string ID_RECAPTCHA = "frmVerifyCaptcha";
        private const string FREE_ACCOUNT_TEXT = "IMPORTANT ACCOUNT INFORMATION";

        private const string IMAGE_URL = "https://search.gsccca.org/imaging/ImageMain.aspx?";

        private HtmlDocument doc;


        public GSCCCAPage(string httpResponse)
        {
            doc = new HtmlDocument();
            doc.LoadHtml(httpResponse);
        }

        public bool IsLoginRequired()
        {
            bool retVal = doc.DocumentNode.SelectSingleNode($"//form[@name='{ID_FORM_LOGIN}']") != null;

            return retVal;
        }

        public bool IsRecaptchaPresent()
        {
            bool retVal = doc.DocumentNode.SelectSingleNode($"//form[@name='{ID_RECAPTCHA}']") != null;

            return retVal;
        }

        public bool IsFreeAccount()
        {
            bool retVal = doc.DocumentNode.SelectSingleNode($"//*[text()[contains(., '{FREE_ACCOUNT_TEXT}')]]") != null;

            return retVal;
        }

        public void GetSearchItems()
        {
            HtmlNode insType = doc.DocumentNode.SelectSingleNode($"//select[@name='{ID_INSTRUMENT_TYPE}']");
            foreach (HtmlNode type in insType.ChildNodes)
            {
                if (type.HasAttributes)
                {
                    DataStore.Instance.InstrumentTypes.Add(new KeyValuePair<string, string>(type.InnerText.Replace("\r\n", string.Empty), 
                                                                                            type.Attributes[0].Value));
                }
            }
            Log.Debug($"Loaded {DataStore.Instance.InstrumentTypes.Count} instrument types");

            HtmlNode counties = doc.DocumentNode.SelectSingleNode($"//select[@name='{ID_COUNTIES}']");
            foreach (HtmlNode type in counties.ChildNodes)
            {
                if (type.HasAttributes)
                {
                    DataStore.Instance.Counties.Add(new KeyValuePair<string, string>(type.InnerText.Replace("\r\n", string.Empty), 
                                                                                     type.Attributes[0].Value));
                }
            }
            Log.Debug($"Loaded {DataStore.Instance.Counties.Count} counties");
        }

        public List<NameSearchResult> NamesSearch()
        {
            List<NameSearchResult> retVal = new List<NameSearchResult>();

            HtmlNode table = doc.DocumentNode.SelectSingleNode($"//table[@class='{ID_NAMES_SEARCH}']");
            if (null != table)
            {
                HtmlNodeCollection rows = table.SelectNodes("tr");
                foreach (HtmlNode row in rows)
                {
                    HtmlNodeCollection cells = row.SelectNodes("td");
                    if (!String.IsNullOrWhiteSpace(cells[0].InnerText) &&
                        cells[0].InnerHtml.Contains("<strong>"))
                    {
                        continue;
                    }

                    NameSearchResult searchResult = new NameSearchResult();
                    if (cells.Count == 3)
                    {
                        int occurs = Int32.Parse(cells[1].InnerText);
                        searchResult.Occurs = occurs;
                        searchResult.Name = cells[2].InnerText;
                        retVal.Add(searchResult);
                    }
                }
            }
            Log.Debug($"Search resulted in {retVal.Count} names");

            return retVal;
        }

        public List<SelectedNameResult> DeedSearch()
        {
            List<SelectedNameResult> retVal = new List<SelectedNameResult>();

            HtmlNodeCollection linkNodes = doc.DocumentNode.SelectNodes("//a[@href]");
            if (linkNodes != null)
            {
                foreach (HtmlNode link in linkNodes)
                {
                    if (link.InnerHtml.Contains("img"))
                    {
                        HtmlAttribute url = link.Attributes["href"];

                        if (!url.Value.Contains("explanation"))
                        {
                            SelectedNameResult deedResult = new SelectedNameResult();

                            string trimUrl = url.Value.Substring(url.Value.IndexOf("'") + 1);
                            trimUrl = trimUrl.Remove(trimUrl.IndexOf("'"));
                            deedResult.DeedUrl = trimUrl;

                            retVal.Add(deedResult);
                        }
                    }
                }
            }
            Log.Debug($"Search resulted in {retVal.Count} deeds");

            return retVal;
        }

        public Deed DeedResult()
        {
            Deed retVal = new Deed();

            HtmlNode content = doc.DocumentNode.SelectSingleNode($"//div[@id='content']");
            
            if (content != null)
            {
                HtmlNodeCollection rows = content.SelectNodes(".//td");

                HtmlNodeCollection tables = content.SelectNodes(".//table");
                foreach (HtmlNode table in tables)
                {
                    bool containsPage = table.InnerText.Contains("Page");
                    bool containsGrantor = table.InnerText.Contains("Grantor");
                    bool containsGrantee = table.InnerText.Contains("Grantee");

                    if (containsPage &&
                        containsGrantor & 
                        containsGrantee)
                    {
                        continue;
                    }
                    else if (containsPage)
                    {
                        if (table.HasChildNodes)
                        {
                            if (table.ChildNodes[1].HasChildNodes)
                            {
                                if (table.ChildNodes[1].ChildNodes[2].HasChildNodes)
                                {
                                    retVal.County = table.ChildNodes[1].ChildNodes[2].ChildNodes[1].InnerText;
                                    retVal.InstrumentType = table.ChildNodes[1].ChildNodes[2].ChildNodes[5].InnerText;
                                    //retVal.DateFiled = table.ChildNodes[1].ChildNodes[2].ChildNodes[9].InnerText;

                                    retVal.Book = table.ChildNodes[1].ChildNodes[2].ChildNodes[17].InnerText.Trim();
                                    retVal.Page = table.ChildNodes[1].ChildNodes[2].ChildNodes[21].InnerText.Trim();
                                }
                                else
                                {
                                    // Working parse on live results
                                    retVal.County = table.ChildNodes[3].ChildNodes[1].InnerText;
                                    retVal.InstrumentType = table.ChildNodes[3].ChildNodes[5].InnerText;
                                    retVal.DateFiled = table.ChildNodes[3].ChildNodes[9].InnerText;
                                    retVal.Time = table.ChildNodes[3].ChildNodes[13].InnerText.Replace("\n", "").Replace("\t", "").Replace("\r", "");
                                    retVal.Book = table.ChildNodes[3].ChildNodes[17].InnerText.Trim();
                                    retVal.Page = table.ChildNodes[3].ChildNodes[21].InnerText.Trim();
                                }
                            }
                        }
                    }
                    //else if (grantorNodeTest != null)
                    else if (containsGrantor)
                    {
                        if (table.HasChildNodes)
                        {
                            for (int i = 3; i < table.ChildNodes.Count; i += 2)
                            {
                                string grantorName = table.ChildNodes[i].InnerText;
                                if (!String.IsNullOrWhiteSpace(grantorName))
                                {
                                    retVal.Grantor.Add(grantorName.Replace("\n", "").Replace("\t", "").Replace("\r", ""));
                                }
                            }
                        }
                    }
                    else if (containsGrantee)
                    {
                        if (table.HasChildNodes)
                        {
                            for (int i = 3; i < table.ChildNodes.Count; i += 2)
                            {
                                string granteeName = table.ChildNodes[i].InnerText;
                                if (!String.IsNullOrWhiteSpace(granteeName))
                                {
                                    retVal.Grantee.Add(granteeName.Replace("\n", "").Replace("\t", "").Replace("\r", ""));
                                }
                            }
                        }
                    }
                }

                HtmlNode node = content.SelectSingleNode("//*[text()[contains(., 'Name Selected:')]]");
                if (node != null &&
                    node.NextSibling != null &&
                    node.NextSibling.NextSibling != null)
                {
                    retVal.NameSelected = node.NextSibling.NextSibling.InnerText.Replace("\n", "").Replace("\t", "").Replace("\r", "");
                }

                HtmlNode scriptNode = doc.DocumentNode.SelectSingleNode($"//script[text()[contains(., 'ViewImage')]]");
                if (scriptNode != null)
                {
                    // Regex parse image link variables
                    Match user = Regex.Match(scriptNode.InnerText, @"[\n\r].*user =\s*([^\n\r]*)");
                    Match county = Regex.Match(scriptNode.InnerText, @"[\n\r].*county =\s*([^\n\r]*)");
                    Match book = Regex.Match(scriptNode.InnerText, @"[\n\r].*book =\s*([^\n\r]*)");
                    Match page = Regex.Match(scriptNode.InnerText, @"[\n\r].*page =\s*([^\n\r]*)");
                    Match appid = Regex.Match(scriptNode.InnerText, @"[\n\r].*appid =\s*([^\n\r]*)");
                    Match iREID = Regex.Match(scriptNode.InnerText, @"[\n\r].*iREID =\s*([^\n\r]*)");

                    string imageUrl = $"{IMAGE_URL}id={getTrimmedValue(iREID)}&key1={getTrimmedValue(book)}" +
                                      $"&key2={getTrimmedValue(page)}&userid={getTrimmedValue(user)}" +
                                      $"&county={getTrimmedValue(county)}&appid={getTrimmedValue(appid)}";
                    retVal.ImageUrl = imageUrl;
                }
            }

            return retVal;
        }

        private string getTrimmedValue(Match match)
        {
            string retVal = "";
            if (match.Groups.Count >= 2)
            {
                retVal = match.Groups[1].Value.Trim().Replace("\"", "").Replace(@"\", "").Replace(@";", "");
            }
            return retVal;
        }
    }
}
