using System.Linq.Expressions;

namespace TrackYourDay.Web.Components
{
    public class TableColumn<T>
    {
        public string Title { get; set; }
        public Expression<Func<T, object>> PropertyExpression { get; set; }
        public bool Filterable { get; set; } = true;
    }
}
