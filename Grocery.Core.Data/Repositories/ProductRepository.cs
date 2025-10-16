using Grocery.Core.Interfaces.Repositories;
using Grocery.Core.Models;
using Grocery.Core.Data.Helpers;
using Microsoft.Data.Sqlite;

namespace Grocery.Core.Data.Repositories
{
    public class ProductRepository : DatabaseConnection, IProductRepository
    {
        private readonly List<Product> products = [];

        public ProductRepository()
        {
            CreateTable(
                @"CREATE TABLE IF NOT EXISTS Product (
                    [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    [Name] NVARCHAR(80) UNIQUE NOT NULL,
                    [Stock] INTEGER NOT NULL,
                    [ShelfLife] DATE NULL,
                    [Price] NUMERIC NOT NULL
                )");

            List<string> insertQueries =
            [
                // Sample seed data (ignored if already present due to UNIQUE Name)
                @"INSERT OR IGNORE INTO Product(Name, Stock, ShelfLife, Price) VALUES('Melk', 300, '2025-09-25', 0.95)",
                @"INSERT OR IGNORE INTO Product(Name, Stock, ShelfLife, Price) VALUES('Kaas', 100, '2025-09-30', 7.98)",
                @"INSERT OR IGNORE INTO Product(Name, Stock, ShelfLife, Price) VALUES('Brood', 400, '2025-09-12', 2.19)",
                @"INSERT OR IGNORE INTO Product(Name, Stock, ShelfLife, Price) VALUES('Cornflakes', 0,  '2025-12-31', 1.48)"
            ];
            InsertMultipleWithTransaction(insertQueries);

            GetAll();
        }

        public List<Product> GetAll()
        {
            products.Clear();

            string selectQuery = "SELECT Id, Name, Stock, ShelfLife, Price FROM Product";
            OpenConnection();
            using (SqliteCommand command = new(selectQuery, Connection))
            using (SqliteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string name = reader.GetString(1);
                    int stock = reader.GetInt32(2);
                    DateOnly shelfLife = reader.IsDBNull(3)
                        ? default
                        : DateOnly.FromDateTime(reader.GetDateTime(3));

                    // Use GetDecimal if mapped; otherwise convert defensively.
                    decimal price = reader.IsDBNull(4)
                        ? 0m
                        : reader.GetFieldType(4) == typeof(decimal)
                            ? reader.GetDecimal(4)
                            : Convert.ToDecimal(reader.GetValue(4));

                    products.Add(new Product(id, name, stock, shelfLife, price));
                }
            }
            CloseConnection();
            return products;
        }

        public Product? Get(int id)
        {
            Product? result = null;

            string selectQuery = "SELECT Id, Name, Stock, ShelfLife, Price FROM Product WHERE Id = @Id";
            OpenConnection();
            using (SqliteCommand command = new(selectQuery, Connection))
            {
                command.Parameters.AddWithValue("Id", id);
                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int productId = reader.GetInt32(0);
                        string name = reader.GetString(1);
                        int stock = reader.GetInt32(2);
                        DateOnly shelfLife = reader.IsDBNull(3)
                            ? default
                            : DateOnly.FromDateTime(reader.GetDateTime(3));
                        decimal price = reader.IsDBNull(4)
                            ? 0m
                            : reader.GetFieldType(4) == typeof(decimal)
                                ? reader.GetDecimal(4)
                                : Convert.ToDecimal(reader.GetValue(4));

                        result = new Product(productId, name, stock, shelfLife, price);
                    }
                }
            }
            CloseConnection();
            return result;
        }

        public Product Add(Product item)
        {
            string insertQuery = @"INSERT INTO Product(Name, Stock, ShelfLife, Price) 
                                   VALUES(@Name, @Stock, @ShelfLife, @Price) 
                                   Returning RowId;";
            OpenConnection();
            using (SqliteCommand command = new(insertQuery, Connection))
            {
                command.Parameters.AddWithValue("Name", item.Name);
                command.Parameters.AddWithValue("Stock", item.Stock);
                if (item.ShelfLife == default)
                    command.Parameters.AddWithValue("ShelfLife", DBNull.Value);
                else
                    command.Parameters.AddWithValue("ShelfLife", item.ShelfLife);
                command.Parameters.AddWithValue("Price", item.Price);

                var rowId = (long?)command.ExecuteScalar();
                item.Id = (int)(rowId ?? 0);
            }
            CloseConnection();
            return Get(item.Id)!;
        }

        public Product? Delete(Product item)
        {
            Product? existing = Get(item.Id);
            if (existing == null) return null;

            string deleteQuery = "DELETE FROM Product WHERE Id = @Id;";
            OpenConnection();
            using (SqliteCommand command = new(deleteQuery, Connection))
            {
                command.Parameters.AddWithValue("Id", item.Id);
                command.ExecuteNonQuery();
            }
            CloseConnection();
            return existing;
        }

        public Product? Update(Product item)
        {
            string updateQuery = @"UPDATE Product 
                                   SET Name = @Name, Stock = @Stock, ShelfLife = @ShelfLife, Price = @Price
                                   WHERE Id = @Id;";
            OpenConnection();
            using (SqliteCommand command = new(updateQuery, Connection))
            {
                command.Parameters.AddWithValue("Name", item.Name);
                command.Parameters.AddWithValue("Stock", item.Stock);
                if (item.ShelfLife == default)
                    command.Parameters.AddWithValue("ShelfLife", DBNull.Value);
                else
                    command.Parameters.AddWithValue("ShelfLife", item.ShelfLife);
                command.Parameters.AddWithValue("Price", item.Price);
                command.Parameters.AddWithValue("Id", item.Id);
                command.ExecuteNonQuery();
            }
            CloseConnection();
            return Get(item.Id);
        }
    }
}
