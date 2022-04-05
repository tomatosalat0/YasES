namespace YasES.Persistance.Sqlite
{
    public class SqliteConfiguration
    {
        /// <summary>
        /// Defines the name of the event store table within the database file. 
        /// Defaults to <c>Events</c>.
        /// </summary>
        public string TableName { get; set; } = "Events";

        /// <summary>
        /// Defines the pagination size when reading events. Lower values
        /// will reduce the read lock within the database but increases
        /// the number of queries executed when more items than the specified
        /// page size would get returned.
        /// </summary>
        public int PaginationSize { get; set; } = 500;
    }
}
