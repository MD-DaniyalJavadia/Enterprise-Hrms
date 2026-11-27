using System.Data;

namespace HRMS.Data.Database;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
