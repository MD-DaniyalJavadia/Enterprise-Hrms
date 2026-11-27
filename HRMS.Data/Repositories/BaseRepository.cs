using System.Data;
using Dapper;
using HRMS.Data.Database;

namespace HRMS.Data.Repositories;

public abstract class BaseRepository
{
    protected readonly IDbConnectionFactory ConnectionFactory;

    protected BaseRepository(IDbConnectionFactory connectionFactory)
    {
        ConnectionFactory = connectionFactory;
    }

    protected async Task<T?> QueryFirstOrDefaultSpAsync<T>(string storedProcedure, DynamicParameters? parameters = null, int? commandTimeout = null)
    {
        using var connection = ConnectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<T>(
            storedProcedure,
            parameters,
            commandTimeout: commandTimeout,
            commandType: CommandType.StoredProcedure);
    }

    protected async Task<IEnumerable<T>> QuerySpAsync<T>(string storedProcedure, DynamicParameters? parameters = null, int? commandTimeout = null)
    {
        using var connection = ConnectionFactory.CreateConnection();
        return await connection.QueryAsync<T>(
            storedProcedure,
            parameters,
            commandTimeout: commandTimeout,
            commandType: CommandType.StoredProcedure);
    }

    protected async Task<int> ExecuteSpAsync(string storedProcedure, DynamicParameters? parameters = null, int? commandTimeout = null)
    {
        using var connection = ConnectionFactory.CreateConnection();
        return await connection.ExecuteAsync(
            storedProcedure,
            parameters,
            commandTimeout: commandTimeout,
            commandType: CommandType.StoredProcedure);
    }

    protected async Task<T?> ExecuteScalarSpAsync<T>(string storedProcedure, DynamicParameters? parameters = null, int? commandTimeout = null)
    {
        using var connection = ConnectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<T>(
            storedProcedure,
            parameters,
            commandTimeout: commandTimeout,
            commandType: CommandType.StoredProcedure);
    }

    protected async Task ExecuteInTransactionAsync(Func<IDbConnection, IDbTransaction, Task> action)
    {
        using var connection = ConnectionFactory.CreateConnection();
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        using var transaction = connection.BeginTransaction();
        try
        {
            await action(connection, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
