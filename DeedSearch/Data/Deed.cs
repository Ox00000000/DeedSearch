using System;
using System.Collections.Generic;

namespace DeedSearch.Data
{
    public class Deed
    {
        public Deed()
        {
            this.Grantor = new List<string>();
            this.Grantee = new List<string>();
        }

        public string NameSelected { get; set; }
        public string County { get; set; }
        public string InstrumentType { get; set; }
        public string DateFiled { get; set; }
        public string Time { get; set; }
        public string Book { get; set; }
        public string Page { get; set; }
        public List<string> Grantor { get; set; }
        public List<string> Grantee { get; set; }
        public string ImageUrl { get; set; }

        public override string ToString()
        {
            return $"{Book} - {Page}";
        }

        public override bool Equals(Object obj)
        {
            //Check for null and compare run-time types
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                Deed d = (Deed) obj;

                return (County == d.County) &&
                       (InstrumentType == d.InstrumentType) &&
                       (DateFiled == d.DateFiled) &&
                       (Time == d.Time) &&
                       (Book == d.Book) &&
                       (Page == d.Page);
            }
        }

        public override int GetHashCode()
        {
            int book, page;
            Int32.TryParse(this.Book, out book);
            Int32.TryParse(this.Page, out page);
            return book * page;
        }
    }
}
