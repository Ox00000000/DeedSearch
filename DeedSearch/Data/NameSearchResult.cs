namespace DeedSearch.Data
{
    public class NameSearchResult
    {
        public string Name { get; set; }

        public int Occurs { get; set; }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
