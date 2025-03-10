namespace TrackYourDay.Web.Components
{
    public class TrackerColumn<T>
    {
        public string Title { get; set; }
        public Expression<Func<T, object>> PropertyExpression { get; set; }
        public bool Filterable { get; set; } = true;
    }
}