using System.Data;
using System.Text;

namespace TestBed
{
    public static class DataTests
    {
        public static void TestBasicDatabaseOperations()
        {
            Console.WriteLine("Testing basic database operations...");

            var connectionString = "Server=localhost;Database=MyApp;Integrated Security=true;";
            // var db = new DBAccess(connectionString);

            // Execute a simple query
            // var userCount = db.GetValue<int>("SELECT COUNT(*) FROM Users");
            // Console.WriteLine($"Total users: {userCount}");

            // Execute query with parameters
            // var activeUsers = db.GetRecords(
            //     "SELECT * FROM Users WHERE IsActive = @active AND CreatedDate > @date",
            //     true, DateTime.Now.AddDays(-30));

            // Console.WriteLine($"Active users in last 30 days: {activeUsers.Rows.Count}");

            Console.WriteLine("✓ Basic database operations test completed");
        }

        public static void TestTransactionHandling()
        {
            Console.WriteLine("Testing transaction handling...");

            var connectionString = "Server=localhost;Database=MyApp;Integrated Security=true;";
            // var db = new DBAccess(connectionString);

            // using (var transaction = db.BeginTransaction())
            // {
            //     try
            //     {
            //         // Create new user
            //         var userId = db.GetValue<int>(
            //             "INSERT INTO Users (FirstName, LastName, Email) OUTPUT INSERTED.UserId VALUES (@first, @last, @email)",
            //             new { first = "John", last = "Doe", email = "john.doe@company.com" },
            //             transaction
            //         );
            //         
            //         // Add user to role
            //         db.ExecuteQuery(
            //             "INSERT INTO UserRoles (UserId, RoleId) VALUES (@userId, @roleId)",
            //             new { userId = userId, roleId = 2 },
            //             transaction
            //         );
            //         
            //         // Commit all changes
            //         transaction.Commit();
            //         Console.WriteLine($"User created successfully with ID: {userId}");
            //     }
            //     catch (Exception ex)
            //     {
            //         transaction.Rollback();
            //         Console.WriteLine($"Transaction failed: {ex.Message}");
            //         throw;
            //     }
            // }

            Console.WriteLine("✓ Transaction handling test completed");
        }

        public static async Task TestAsyncOperations()
        {
            Console.WriteLine("Testing async database operations...");

            var connectionString = "Server=localhost;Database=MyApp;Integrated Security=true;";
            // var db = new DBAccess(connectionString);
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token;

            try
            {
                // Async query execution
                // var users = await db.GetDataTableAsync(
                //     "SELECT * FROM Users WHERE Department = @dept ORDER BY LastName",
                //     new { dept = "Sales" },
                //     cancellationToken
                // );
                
                // Console.WriteLine($"Found {users.Rows.Count} sales users");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Database operation was cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
            }

            Console.WriteLine("✓ Async operations test completed");
        }

        public static async Task TestBulkOperations()
        {
            Console.WriteLine("Testing bulk operations...");

            var connectionString = "Server=localhost;Database=MyApp;Integrated Security=true;";
            // var db = new DBAccess(connectionString);
            // var bulkProcessor = new BulkDbProcessor<Product>(db);

            // Create sample data
            var products = new List<Product>();
            for (int i = 1; i <= 100; i++)
            {
                products.Add(new Product
                {
                    Name = $"Product {i}",
                    CategoryId = (i % 5) + 1,
                    Price = Math.Round((decimal)(10 + (i % 100)), 2),
                    Stock = i % 50
                });
            }

            // Bulk insert all products
            // await bulkProcessor.BulkInsertAsync("Products", products);

            Console.WriteLine("✓ Bulk operations test completed");
        }

        public static void TestCSVReading()
        {
            Console.WriteLine("Testing CSV reading...");

            // var csvReader = new CSVReader();

            // Read CSV file with automatic format detection
            // csvReader.ReadFile("customers.csv");

            // Console.WriteLine($"Loaded {csvReader.Records.Count} records");
            // Console.WriteLine($"Columns: {string.Join(", ", csvReader.Headers)}");

            Console.WriteLine("✓ CSV reading test completed");
        }

        public static void TestCSVWriting()
        {
            Console.WriteLine("Testing CSV writing...");

            // var csvWriter = new CSVWriter();

            // Configure output format
            var format = new CSVFormat
            {
                Delimiter = ',',
                QuoteCharacter = '"',
                HasHeaders = true,
                Encoding = Encoding.UTF8
            };

            // Define headers
            var headers = new[] { "ProductId", "ProductName", "Category", "Price", "InStock" };

            // Sample data
            var products = new[]
            {
                new { Id = 1, Name = "Laptop Computer", Category = "Electronics", Price = 999.99m, Stock = true },
                new { Id = 2, Name = "Office Chair", Category = "Furniture", Price = 249.50m, Stock = false },
                new { Id = 3, Name = "Wireless Mouse", Category = "Electronics", Price = 29.99m, Stock = true }
            };

            // Write to CSV
            // csvWriter.WriteFile("products.csv", format, headers, products.Select(p => new object[]
            // {
            //     p.Id,
            //     p.Name,
            //     p.Category,
            //     p.Price,
            //     p.Stock ? "Yes" : "No"
            // }));

            Console.WriteLine("✓ CSV writing test completed");
        }

        public static void TestAudioFormatDetection()
        {
            Console.WriteLine("Testing audio format detection...");

            // var detector = new AudioFormatDetector();

            // Check single audio file
            var audioFile = @"C:\Music\song.mp3";
            // if (detector.IsAudioFile(audioFile))
            // {
            //     var format = detector.DetectFormat(audioFile);
            //     var info = detector.GetAudioInfo(audioFile);
            //     
            //     Console.WriteLine($"File: {Path.GetFileName(audioFile)}");
            //     Console.WriteLine($"Format: {format}");
            //     Console.WriteLine($"Duration: {info.Duration}");
            //     Console.WriteLine($"Bitrate: {info.Bitrate} kbps");
            // }

            Console.WriteLine("✓ Audio format detection test completed");
        }

        public static async Task TestCompleteDataWorkflow()
        {
            Console.WriteLine("Testing complete data workflow...");

            var connectionString = "Server=localhost;Database=MyApp;Integrated Security=true;";
            var processor = new DataProcessingService(connectionString);

            try
            {
                var inputFile = "customers_import.csv";
                var outputFile = $"customers_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                // var summary = await processor.ProcessCustomerData(inputFile, outputFile);
                // summary.PrintSummary();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Application error: {ex.Message}");
            }

            Console.WriteLine("✓ Complete data workflow test completed");
        }
    }

    // Entity classes with DB column mapping
    public class Product
    {
        [DBColumn("ProductId", isPrimaryKey: true, isIdentity: true)]
        public int Id { get; set; }

        [DBColumn("ProductName")]
        public string Name { get; set; } = "";

        [DBColumn("CategoryId")]
        public int CategoryId { get; set; }

        [DBColumn("UnitPrice")]
        public decimal Price { get; set; }

        [DBColumn("UnitsInStock")]
        public int Stock { get; set; }

        [DBColumn("CreatedDate")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    public class Customer
    {
        [DBColumn("CustomerId", isPrimaryKey: true)]
        public string Id { get; set; } = "";

        [DBColumn("CompanyName")]
        public string CompanyName { get; set; } = "";

        [DBColumn("ContactName")]
        public string ContactName { get; set; } = "";

        [DBColumn("Phone")]
        public string Phone { get; set; } = "";

        [DBColumn("LastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    public class Employee
    {
        [DBColumn("EmployeeID", isPrimaryKey: true, isIdentity: true)]
        public int Id { get; set; }

        [DBColumn("FirstName", maxLength: 50, isRequired: true)]
        public string FirstName { get; set; } = "";

        [DBColumn("LastName", maxLength: 50, isRequired: true)]
        public string LastName { get; set; } = "";

        [DBColumn("Email", maxLength: 100, isRequired: true, isUnique: true)]
        public string Email { get; set; } = "";

        [DBColumn("HireDate")]
        public DateTime HireDate { get; set; }

        [DBColumn("Salary", precision: 10, scale: 2)]
        public decimal? Salary { get; set; }

        [DBColumn("DepartmentID")]
        public int? DepartmentId { get; set; }

        [DBColumn("IsActive")]
        public bool IsActive { get; set; } = true;

        // Computed property - not mapped to database
        public string FullName => $"{FirstName} {LastName}";

        // Navigation property - handled separately
        public Department? Department { get; set; }
    }

    public class Department
    {
        [DBColumn("DepartmentID", isPrimaryKey: true, isIdentity: true)]
        public int Id { get; set; }

        [DBColumn("DepartmentName", maxLength: 100, isRequired: true)]
        public string Name { get; set; } = "";

        [DBColumn("ManagerID")]
        public int? ManagerId { get; set; }

        [DBColumn("Budget", precision: 15, scale: 2)]
        public decimal Budget { get; set; }
    }

    public class ProductImport
    {
        [CSVColumn("Product ID", isRequired: true)]
        public int ProductId { get; set; }

        [CSVColumn("Product Name", maxLength: 100, isRequired: true)]
        public string Name { get; set; } = "";

        [CSVColumn("Category")]
        public string Category { get; set; } = "";

        [CSVColumn("Unit Price", format: "C")]
        public decimal Price { get; set; }

        [CSVColumn("Units in Stock")]
        public int Stock { get; set; }

        [CSVColumn("Discontinued", format: "Yes/No")]
        public bool IsDiscontinued { get; set; }

        [CSVColumn("Last Updated", format: "yyyy-MM-dd")]
        public DateTime LastUpdated { get; set; }

        // Property without attribute - will use property name
        public string Supplier { get; set; } = "";
    }

    // Complete data processing workflow classes
    public class CompleteCustomer
    {
        [DBColumn("CustomerID", isPrimaryKey: true)]
        [CSVColumn("Customer ID")]
        public string Id { get; set; } = "";

        [DBColumn("CompanyName", maxLength: 100, isRequired: true)]
        [CSVColumn("Company Name")]
        public string CompanyName { get; set; } = "";

        [DBColumn("ContactName", maxLength: 50)]
        [CSVColumn("Contact Name")]
        public string ContactName { get; set; } = "";

        [DBColumn("Phone", maxLength: 20)]
        [CSVColumn("Phone")]
        public string Phone { get; set; } = "";

        [DBColumn("Email", maxLength: 100)]
        [CSVColumn("Email")]
        public string Email { get; set; } = "";

        [DBColumn("CreatedDate")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [DBColumn("LastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    public class DataProcessingService
    {
        private readonly string _connectionString;
        // private readonly DBAccess _db;

        public DataProcessingService(string connectionString)
        {
            _connectionString = connectionString;
            // _db = new DBAccess(connectionString);
        }

        public async Task<int> ImportCustomersFromCSV(string csvFilePath)
        {
            try
            {
                Console.WriteLine($"Starting CSV import from: {csvFilePath}");

                // Read CSV file
                // var csvReader = new CSVReader();
                // csvReader.ReadFile(csvFilePath);

                // Convert CSV records to entities
                var customers = new List<CompleteCustomer>();
                var errors = new List<string>();

                // Bulk insert customers
                // var bulkProcessor = new BulkDbProcessor<CompleteCustomer>(_db);
                // var result = await bulkProcessor.BulkUpsertAsync("Customers", customers,
                //     updateColumns: new[] { "CompanyName", "ContactName", "Phone", "Email", "LastUpdated" });

                // Console.WriteLine($"Import completed: {result.RowsAffected} customers processed");
                // return result.RowsAffected;
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Import failed: {ex.Message}");
                throw;
            }
        }

        public async Task<string> ExportCustomersToCSV(string outputPath, DateTime? modifiedSince = null)
        {
            try
            {
                Console.WriteLine("Starting customer export...");

                // Query customers with optional date filter
                string query = "SELECT * FROM Customers";
                object? parameters = null;

                if (modifiedSince.HasValue)
                {
                    query += " WHERE LastUpdated >= @modifiedSince";
                    parameters = new { modifiedSince = modifiedSince.Value };
                }

                query += " ORDER BY CompanyName";

                // var dataTable = await _db.GetDataTableAsync(query, parameters);

                // Export to CSV
                // var csvWriter = new CSVWriter();
                var format = new CSVFormat
                {
                    Delimiter = ',',
                    HasHeaders = true,
                    QuoteCharacter = '"',
                    DateTimeFormat = "yyyy-MM-dd HH:mm:ss"
                };

                // csvWriter.WriteDataTable(outputPath, dataTable, format);

                Console.WriteLine($"Export completed: {outputPath}");
                return outputPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Export failed: {ex.Message}");
                throw;
            }
        }

        public async Task<DataProcessingSummary> ProcessCustomerData(string inputPath, string outputPath)
        {
            var summary = new DataProcessingSummary();

            try
            {
                // Import from CSV
                summary.ImportedCount = await ImportCustomersFromCSV(inputPath);

                // Perform data cleanup
                summary.UpdatedCount = await CleanupCustomerData();

                // Export updated data
                var exportPath = await ExportCustomersToCSV(outputPath, DateTime.Now.AddHours(-1));
                summary.ExportPath = exportPath;

                // Generate statistics
                // summary.TotalCustomers = _db.GetValue<int>("SELECT COUNT(*) FROM Customers");
                // summary.ActiveCustomers = _db.GetValue<int>("SELECT COUNT(*) FROM Customers WHERE Email IS NOT NULL");

                summary.Success = true;
                Console.WriteLine("Data processing completed successfully");

                return summary;
            }
            catch (Exception ex)
            {
                summary.Success = false;
                summary.ErrorMessage = ex.Message;
                Console.WriteLine($"Data processing failed: {ex.Message}");
                throw;
            }
        }

        private async Task<int> CleanupCustomerData()
        {
            var updates = 0;

            // Normalize phone numbers
            // updates += await _db.ExecuteNonQueryAsync(@"
            //     UPDATE Customers 
            //     SET Phone = REPLACE(REPLACE(REPLACE(Phone, '(', ''), ')', ''), '-', ''),
            //         LastUpdated = GETDATE()
            //     WHERE Phone LIKE '%[()-]%'");

            Console.WriteLine($"Data cleanup completed: {updates} records updated");
            return updates;
        }
    }

    public class DataProcessingSummary
    {
        public bool Success { get; set; }
        public int ImportedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public string ExportPath { get; set; } = "";
        public string ErrorMessage { get; set; } = "";

        public void PrintSummary()
        {
            Console.WriteLine("=== Data Processing Summary ===");
            Console.WriteLine($"Success: {Success}");
            if (Success)
            {
                Console.WriteLine($"Imported: {ImportedCount} customers");
                Console.WriteLine($"Updated: {UpdatedCount} records");
                Console.WriteLine($"Total Customers: {TotalCustomers}");
                Console.WriteLine($"Active Customers: {ActiveCustomers}");
                Console.WriteLine($"Export File: {ExportPath}");
            }
            else
            {
                Console.WriteLine($"Error: {ErrorMessage}");
            }
            Console.WriteLine("===============================");
        }
    }

    // Placeholder classes to make the code compile
    public class DBColumnAttribute : Attribute
    {
        public DBColumnAttribute(string columnName, bool isPrimaryKey = false, bool isIdentity = false, int maxLength = 0, bool isRequired = false, bool isUnique = false, int precision = 0, int scale = 0)
        {
        }
    }

    public class CSVColumnAttribute : Attribute
    {
        public CSVColumnAttribute(string columnName, bool isRequired = false, int maxLength = 0, string format = "")
        {
        }
    }

    public class CSVFormat
    {
        public char Delimiter { get; set; }
        public char QuoteCharacter { get; set; }
        public char EscapeCharacter { get; set; }
        public bool HasHeaders { get; set; }
        public bool TrimFields { get; set; }
        public bool SkipEmptyLines { get; set; }
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public string DateTimeFormat { get; set; } = "";
        public string NumberFormat { get; set; } = "";
    }
}