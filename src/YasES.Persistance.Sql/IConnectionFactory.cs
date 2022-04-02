using System.Data;

namespace YasES.Persistance.Sql
{
    public interface IConnectionFactory
    {
        IDbConnection Open();
    }
}
