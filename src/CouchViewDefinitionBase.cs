namespace Divan
{
    public abstract class CouchViewDefinitionBase 
    {
        public CouchDesignDocument Doc { get; set; }
        public string Name { get; set; }

        protected CouchViewDefinitionBase(string name, CouchDesignDocument doc)
        {
            Doc = doc;
            Name = name;
        }

        public CouchDatabase Db()
        {
            return Doc.Owner;
        }

        public CouchRequest Request()
        {
            return Db().Request(Path());
        }

        public virtual string Path()
        {
            if (Doc.Id == "_design/")
            {
                return Name;
            }
            return Doc.Id + "/_view/" + Name;
        }
    }
}