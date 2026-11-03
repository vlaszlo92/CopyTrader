using System.Data.SqlClient;

namespace CopyTrader.Worker
{
    public class DatabaseContext : IDisposable
    {
        private readonly SqlConnection connection;

        public DatabaseContext(string connectionString)
        {
            connection = new SqlConnection(connectionString);
            connection.Open();
        }

        public void Dispose()
        {
            connection.Dispose();
        }

        public List<Trader> GetTraders(int id)
        {
            var traders = new List<Trader>();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT DISTINCT uas.Id, uas.FullName, uas.APIKey, uas.Secure FROM UserAPIs uas JOIN Traders t ON t.UserAPIId = uas.Id WHERE uas.IsActive = 1";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                traders.Add(new Trader
                {
                    Id = id++,
                    UserApiId = reader.GetInt32(0),
                    Name = reader.GetString(1).Trim(),
                    ApiKey = reader.GetString(2).Trim(),
                    SecKey = reader.GetString(3).Trim(),
                    GroupIds = GetTraderGroupIds(reader.GetInt32(0)),
                    Balance = GetBalance(reader.GetInt32(0)),
                    IsActive = true,
                    AccountInfos = new List<AccountInfo>()
                });
            }

            return traders;
        }
        public List<int> GetTraderGroupIds(int userapiid)
        {
            List<int> groupIds = new List<int>();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT TraderGroupId FROM Traders WHERE UserAPIId = @UserAPIId";
            command.Parameters.AddWithValue("@UserAPIId", userapiid);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                int groupId = reader.GetInt32(0);
                groupIds.Add(groupId);
            }

            return groupIds;
        }
        public Balance GetBalance(int userapiid)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT CurrencyType, CurrencyAmount, CurrentAmount FROM Balances WHERE UserAPIId = @UserAPIId";
            command.Parameters.AddWithValue("@UserAPIId", userapiid);

            using var reader = command.ExecuteReader();
            Balance balance = new Balance();
            while (reader.Read())
            {
                balance = new Balance
                {
                    Type = reader.GetString(0).Trim(),
                    MaxValue = reader.GetDouble(1),
                    CurrentValue = reader.GetDouble(2)
                };
            }
            return balance;
        }

        public List<CopyTrader> GetCopyTraders(int id)
        {
            var copyTraders = new List<CopyTrader>();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT DISTINCT Id, FullName, APIKey, Secure, TraderGroupId FROM UserAPIs WHERE TraderGroupId IS NOT NULL AND IsActive = 1;";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var ct = new CopyTrader
                {
                    Id = id++,
                    UserApiId = reader.GetInt32(0),
                    Name = reader.GetString(1).Trim(),
                    ApiKey = reader.GetString(2).Trim(),
                    SecKey = reader.GetString(3).Trim(),
                    TraderGroupId = reader.GetInt32(4),
                    Balance = GetBalance(reader.GetInt32(0)),
                    IsActive = true,
                    AccountInfos = new List<AccountInfo>()
                };

                if (IsTraderGroupIdActive(ct.TraderGroupId))
                {
                    copyTraders.Add(ct);
                }
            }

            return copyTraders;
        }

        public bool IsTraderGroupIdActive(int id)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM Traders t JOIN UserAPIs uas ON t.UserAPIId = uas.Id WHERE t.TraderGroupId = @TraderGroupId AND uas.IsActive = 1";
            command.Parameters.AddWithValue("@TraderGroupId", id);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var ret = reader.GetInt32(0);
                if (ret == 1)
                {
                    return true;
                }
            }

            return false;
        }

        public void ExecuteTransactionInsertUpdate(
           int userApiId,
           long? transactionIdTrader,
           string symbol,
           string clientOrderId,
           long orderId,
           string side,
           string orderType,
           decimal quantity,
           decimal averagePrice,
           string orderStatus,
           decimal? commissionAsset,
           string workingType,
           string originalOrderType,
           string positionSide,
           decimal? realizedProfit,
           string? json
       )
        {
            // MEGJ: egy service fél óránként csekkolja a trader - copy trader desyncet a positions / order bookban, és ha talál - copyzza + logolja.
            try
            {
                using (SqlCommand command = new SqlCommand("transaction_insert_update", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    // Add parameters to the command
                    command.Parameters.AddWithValue("@user_api_id", userApiId);
                    command.Parameters.AddWithValue("@order_id_trader", transactionIdTrader.HasValue ? transactionIdTrader : DBNull.Value);
                    command.Parameters.AddWithValue("@symbol", symbol);
                    command.Parameters.AddWithValue("@client_order_id", clientOrderId);
                    command.Parameters.AddWithValue("@order_id", orderId);
                    command.Parameters.AddWithValue("@side", side);
                    command.Parameters.AddWithValue("@order_type", orderType);
                    command.Parameters.AddWithValue("@quantity", quantity);
                    command.Parameters.AddWithValue("@average_price", averagePrice);
                    command.Parameters.AddWithValue("@order_status", orderStatus);
                    command.Parameters.AddWithValue("@commission_asset", commissionAsset.HasValue ? commissionAsset : DBNull.Value);
                    command.Parameters.AddWithValue("@working_type", workingType);
                    command.Parameters.AddWithValue("@original_order_type", originalOrderType);
                    command.Parameters.AddWithValue("@position_side", positionSide);
                    command.Parameters.AddWithValue("@realized_profit", realizedProfit.HasValue ? realizedProfit : DBNull.Value);
                    command.Parameters.AddWithValue("@json", !string.IsNullOrEmpty(json) ? json : DBNull.Value);

                    // Execute the stored procedure
                    int rowsAffected = command.ExecuteNonQuery();

                    // Check if rows were affected (optional)
                    if (rowsAffected > 0)
                    {
                    }
                    Console.WriteLine("db saved");
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occurred during the process
                // Log or display the error as needed
            }
        }
    }
}