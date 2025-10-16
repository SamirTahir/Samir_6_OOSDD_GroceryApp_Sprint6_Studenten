using Grocery.Core.Interfaces.Repositories;
using Grocery.Core.Models;
using Grocery.Core.Data.Helpers;
using Microsoft.Data.Sqlite;

namespace Grocery.Core.Data.Repositories
{
    public class GroceryListItemsRepository : DatabaseConnection, IGroceryListItemsRepository
    {
        private readonly List<GroceryListItem> groceryListItems = [];

        public GroceryListItemsRepository()
        {
            CreateTable(
                @"CREATE TABLE IF NOT EXISTS GroceryListItem (
                    [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    [GroceryListId] INTEGER NOT NULL,
                    [ProductId] INTEGER NOT NULL,
                    [Amount] INTEGER NOT NULL
                )");

            // Ensure unique pair and clean duplicates before creating the unique index
            EnsureUniquePairIndex();

            List<string> insertQueries =
            [
                @"INSERT OR IGNORE INTO GroceryListItem(GroceryListId, ProductId, Amount) VALUES(1, 1, 3)",
                @"INSERT OR IGNORE INTO GroceryListItem(GroceryListId, ProductId, Amount) VALUES(1, 2, 1)",
                @"INSERT OR IGNORE INTO GroceryListItem(GroceryListId, ProductId, Amount) VALUES(1, 3, 4)",
                @"INSERT OR IGNORE INTO GroceryListItem(GroceryListId, ProductId, Amount) VALUES(2, 1, 2)",
                @"INSERT OR IGNORE INTO GroceryListItem(GroceryListId, ProductId, Amount) VALUES(2, 2, 5)"
            ];
            InsertMultipleWithTransaction(insertQueries);

            GetAll();
        }

        // Collapse duplicates and add a UNIQUE index on (GroceryListId, ProductId)
        private void EnsureUniquePairIndex()
        {
            OpenConnection();
            using var tx = Connection.BeginTransaction();

            // Merge duplicates: keep the lowest Id, sum amounts into it
            using (var merge = new SqliteCommand(@"
                WITH d AS (
                    SELECT MIN(Id) AS KeepId, GroceryListId, ProductId, SUM(Amount) AS Total
                    FROM GroceryListItem
                    GROUP BY GroceryListId, ProductId
                )
                UPDATE GroceryListItem
                SET Amount = (SELECT Total FROM d WHERE d.KeepId = GroceryListItem.Id)
                WHERE Id IN (SELECT KeepId FROM d);
            ", Connection, tx))
            {
                merge.ExecuteNonQuery();
            }

            // Delete the non-keeper duplicates
            using (var deleteDups = new SqliteCommand(@"
                DELETE FROM GroceryListItem
                WHERE Id NOT IN (
                    SELECT MIN(Id) FROM GroceryListItem GROUP BY GroceryListId, ProductId
                );
            ", Connection, tx))
            {
                deleteDups.ExecuteNonQuery();
            }

            // Create UNIQUE index (idempotent)
            using (var createIdx = new SqliteCommand(@"
                CREATE UNIQUE INDEX IF NOT EXISTS IX_GroceryListItem_GroceryListId_ProductId
                ON GroceryListItem(GroceryListId, ProductId);
            ", Connection, tx))
            {
                createIdx.ExecuteNonQuery();
            }

            tx.Commit();
            CloseConnection();
        }

        public List<GroceryListItem> GetAll()
        {
            groceryListItems.Clear();
            string selectQuery = "SELECT Id, GroceryListId, ProductId, Amount FROM GroceryListItem";
            OpenConnection();
            using (SqliteCommand command = new(selectQuery, Connection))
            using (SqliteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    int groceryListId = reader.GetInt32(1);
                    int productId = reader.GetInt32(2);
                    int amount = reader.GetInt32(3);
                    groceryListItems.Add(new(id, groceryListId, productId, amount));
                }
            }
            CloseConnection();
            return groceryListItems;
        }

        public List<GroceryListItem> GetAllOnGroceryListId(int id)
        {
            List<GroceryListItem> items = [];
            string selectQuery = "SELECT Id, GroceryListId, ProductId, Amount FROM GroceryListItem WHERE GroceryListId = @GroceryListId";
            OpenConnection();
            using (SqliteCommand command = new(selectQuery, Connection))
            {
                command.Parameters.AddWithValue("GroceryListId", id);
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int itemId = reader.GetInt32(0);
                        int groceryListId = reader.GetInt32(1);
                        int productId = reader.GetInt32(2);
                        int amount = reader.GetInt32(3);
                        items.Add(new(itemId, groceryListId, productId, amount));
                    }
                }
            }
            CloseConnection();
            return items;
        }

        public GroceryListItem Add(GroceryListItem item)
        {
            // UPSERT: if the (GroceryListId, ProductId) already exists, increase the amount
            string upsert = @"
                INSERT INTO GroceryListItem(GroceryListId, ProductId, Amount)
                VALUES(@GroceryListId, @ProductId, @Amount)
                ON CONFLICT(GroceryListId, ProductId)
                DO UPDATE SET Amount = GroceryListItem.Amount + excluded.Amount;
            ";

            OpenConnection();
            using (SqliteCommand command = new(upsert, Connection))
            {
                command.Parameters.AddWithValue("GroceryListId", item.GroceryListId);
                command.Parameters.AddWithValue("ProductId", item.ProductId);
                command.Parameters.AddWithValue("Amount", item.Amount);
                command.ExecuteNonQuery();
            }

            // Fetch the current row (Id and Amount)
            using (SqliteCommand select = new(@"
                SELECT Id, Amount FROM GroceryListItem
                WHERE GroceryListId = @GroceryListId AND ProductId = @ProductId;
            ", Connection))
            {
                select.Parameters.AddWithValue("GroceryListId", item.GroceryListId);
                select.Parameters.AddWithValue("ProductId", item.ProductId);
                using var reader = select.ExecuteReader();
                if (reader.Read())
                {
                    item.Id = reader.GetInt32(0);
                    item.Amount = reader.GetInt32(1);
                }
            }

            CloseConnection();
            return item;
        }

        public GroceryListItem? Delete(GroceryListItem item)
        {
            GroceryListItem? existing = Get(item.Id);
            if (existing == null) return null;

            string deleteQuery = "DELETE FROM GroceryListItem WHERE Id = @Id;";
            OpenConnection();
            using (SqliteCommand command = new(deleteQuery, Connection))
            {
                command.Parameters.AddWithValue("Id", item.Id);
                command.ExecuteNonQuery();
            }
            CloseConnection();
            return existing;
        }

        public GroceryListItem? Get(int id)
        {
            GroceryListItem? result = null;
            string selectQuery = "SELECT Id, GroceryListId, ProductId, Amount FROM GroceryListItem WHERE Id = @Id";
            OpenConnection();
            using (SqliteCommand command = new(selectQuery, Connection))
            {
                command.Parameters.AddWithValue("Id", id);
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int itemId = reader.GetInt32(0);
                        int groceryListId = reader.GetInt32(1);
                        int productId = reader.GetInt32(2);
                        int amount = reader.GetInt32(3);
                        result = new(itemId, groceryListId, productId, amount);
                    }
                }
            }
            CloseConnection();
            return result;
        }

        public GroceryListItem? Update(GroceryListItem item)
        {
            string updateQuery = @"UPDATE GroceryListItem 
                                   SET GroceryListId = @GroceryListId, ProductId = @ProductId, Amount = @Amount 
                                   WHERE Id = @Id;";
            OpenConnection();
            using (SqliteCommand command = new(updateQuery, Connection))
            {
                command.Parameters.AddWithValue("GroceryListId", item.GroceryListId);
                command.Parameters.AddWithValue("ProductId", item.ProductId);
                command.Parameters.AddWithValue("Amount", item.Amount);
                command.Parameters.AddWithValue("Id", item.Id);
                command.ExecuteNonQuery();
            }
            CloseConnection();
            return Get(item.Id);
        }
    }
}
