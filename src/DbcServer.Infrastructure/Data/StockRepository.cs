using System.Collections.Concurrent;
using System.Text;
using DbcServer.Core.Interfaces;
using DbcServer.Core.Models;
using DbfDataReader;
using Microsoft.Extensions.Configuration;

namespace DbcServer.Infrastructure.Data;

public class StockRepository : IStockRepository
{
    private readonly string _dbfPath;
    private readonly ConcurrentDictionary<string, Dictionary<string, int>> _ordinalCache = new();
    private int? _cachedTotalCount;
    private DateTime _cacheExpiryTime = DateTime.MinValue;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(15);
    private readonly object _cacheLock = new();
    private Task? _backgroundCacheTask;

    public StockRepository(IConfiguration configuration)
    {
        _dbfPath = configuration["DbfPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "tmp");

        // Resolve relative paths to absolute paths
        if (!Path.IsPathRooted(_dbfPath))
        {
            _dbfPath = Path.Combine(Directory.GetCurrentDirectory(), _dbfPath);
        }

        // Log the resolved path for debugging
        Console.WriteLine($"[StockRepository] DBF Path: {_dbfPath}");
        var stocPath = Path.Combine(_dbfPath, "STOC.DBF");
        Console.WriteLine($"[StockRepository] STOC.DBF Path: {stocPath}");
        Console.WriteLine($"[StockRepository] STOC.DBF Exists: {File.Exists(stocPath)}");
    }

    public async Task<PaginatedResult<StockItem>> GetStockItemsAsync(int pageNumber, int pageSize, string? barcode = null)
    {
        return await Task.Run(() =>
        {
            var dbfFilePath = Path.Combine(_dbfPath, "STOC.DBF");

            if (!File.Exists(dbfFilePath))
            {
                return new PaginatedResult<StockItem>
                {
                    Items = new List<StockItem>(),
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = 0
                };
            }

            // Register the code page provider to support Windows-1252 encoding
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var options = new DbfDataReaderOptions
            {
                SkipDeletedRecords = true,
                Encoding = Encoding.GetEncoding(1252)
            };

            var items = new List<StockItem>();
            int totalCount = 0;

            // If searching by barcode, we need to filter and count
            if (!string.IsNullOrWhiteSpace(barcode))
            {
                using (var dbfReader = new DbfDataReader.DbfDataReader(dbfFilePath, options))
                {
                    var ordinals = GetOrCacheOrdinals(dbfReader, dbfFilePath);
                    int currentIndex = 0;
                    int skipCount = (pageNumber - 1) * pageSize;
                    int itemsAdded = 0;

                    while (dbfReader.Read())
                    {
                        var codBare = GetStringSafe(dbfReader, ordinals, "COD_BARE");
                        if (codBare != null && codBare.Contains(barcode, StringComparison.OrdinalIgnoreCase))
                        {
                            totalCount++;

                            // Only map items for the current page
                            if (currentIndex >= skipCount && itemsAdded < pageSize)
                            {
                                items.Add(MapToStockItem(dbfReader, ordinals));
                                itemsAdded++;
                            }
                            currentIndex++;
                        }
                    }
                }
            }
            else
            {
                // For unfiltered queries, use cached total count
                totalCount = GetCachedTotalCount(dbfFilePath);

                // Now read only the records we need for the current page
                using (var dbfReader = new DbfDataReader.DbfDataReader(dbfFilePath, options))
                {
                    var ordinals = GetOrCacheOrdinals(dbfReader, dbfFilePath);
                    int skipCount = (pageNumber - 1) * pageSize;
                    int currentIndex = 0;
                    int itemsAdded = 0;

                    // Skip records until we reach the desired page
                    while (dbfReader.Read() && currentIndex < skipCount)
                    {
                        currentIndex++;
                    }

                    // Read records for the current page
                    while (dbfReader.Read() && itemsAdded < pageSize)
                    {
                        items.Add(MapToStockItem(dbfReader, ordinals));
                        itemsAdded++;
                    }
                }
            }

            return new PaginatedResult<StockItem>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        });
    }

    public async Task<StockItem?> GetStockItemByCodeAsync(int code)
    {
        return await Task.Run(() =>
        {
            var dbfFilePath = Path.Combine(_dbfPath, "STOC.DBF");

            if (!File.Exists(dbfFilePath))
                return null;

            // Register the code page provider to support Windows-1252 encoding
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var options = new DbfDataReaderOptions
            {
                SkipDeletedRecords = true,
                Encoding = Encoding.GetEncoding(1252)
            };

            using var dbfReader = new DbfDataReader.DbfDataReader(dbfFilePath, options);
            var ordinals = GetOrCacheOrdinals(dbfReader, dbfFilePath);

            // For large files, stop as soon as we find the item
            while (dbfReader.Read())
            {
                var codValue = GetInt32Safe(dbfReader, ordinals, "COD");
                if (codValue == code)
                {
                    return MapToStockItem(dbfReader, ordinals, loadAllFields: true);
                }
            }

            return null;
        });
    }

    public async Task<IEnumerable<StockItem>> SearchByBarcodeAsync(string barcode)
    {
        return await Task.Run(() =>
        {
            var items = new List<StockItem>();
            var dbfFilePath = Path.Combine(_dbfPath, "STOC.DBF");

            if (!File.Exists(dbfFilePath))
                return items;

            // Register the code page provider to support Windows-1252 encoding
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var options = new DbfDataReaderOptions
            {
                SkipDeletedRecords = true,
                Encoding = Encoding.GetEncoding(1252)
            };

            using var dbfReader = new DbfDataReader.DbfDataReader(dbfFilePath, options);
            var ordinals = GetOrCacheOrdinals(dbfReader, dbfFilePath);

            // Limit results to prevent memory issues with large datasets
            const int maxResults = 100;
            int foundCount = 0;

            while (dbfReader.Read() && foundCount < maxResults)
            {
                var codBare = GetStringSafe(dbfReader, ordinals, "COD_BARE");
                if (!string.IsNullOrWhiteSpace(codBare) &&
                    codBare.Contains(barcode, StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(MapToStockItem(dbfReader, ordinals));
                    foundCount++;
                }
            }

            return items;
        });
    }

    public async Task<int> GetTotalCountAsync(string? barcode = null)
    {
        return await Task.Run(() =>
        {
            var dbfFilePath = Path.Combine(_dbfPath, "STOC.DBF");

            if (!File.Exists(dbfFilePath))
                return 0;

            // Register the code page provider to support Windows-1252 encoding
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var options = new DbfDataReaderOptions
            {
                SkipDeletedRecords = true,
                Encoding = Encoding.GetEncoding(1252)
            };

            using var dbfReader = new DbfDataReader.DbfDataReader(dbfFilePath, options);

            // For unfiltered count, we need to count all records
            if (string.IsNullOrWhiteSpace(barcode))
            {
                int count = 0;
                while (dbfReader.Read())
                {
                    count++;
                }
                return count;
            }

            // For filtered count, we need to iterate
            var ordinals = GetOrCacheOrdinals(dbfReader, dbfFilePath);
            int filteredCount = 0;

            while (dbfReader.Read())
            {
                var codBare = GetStringSafe(dbfReader, ordinals, "COD_BARE");
                if (!string.IsNullOrWhiteSpace(codBare) &&
                    codBare.Contains(barcode, StringComparison.OrdinalIgnoreCase))
                {
                    filteredCount++;
                }
            }

            return filteredCount;
        });
    }

    private Dictionary<string, int> GetOrCacheOrdinals(DbfDataReader.DbfDataReader reader, string filePath)
    {
        return _ordinalCache.GetOrAdd(filePath, _ =>
        {
            var ordinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // Cache all column ordinals
            var columnNames = new[]
            {
                "DUBLU", "DATA", "CATEGORIE", "DENUMIRE", "CANTITATE", "CANT_REZER",
                "MOSTRE", "UNIT_MAS", "PRET", "PRET_ACHI", "GARANTIE", "COD",
                "LA_ENGROS", "OBSERVATII", "DEPOZITUL", "BUC", "PROTVAACHI", "PROTVAVINZ",
                "COD_CORESP", "FURNIZOR", "COD_BARE", "ADRESA", "PRO_VAMA", "PRET_B",
                "PRET_C", "SHORT_C", "COD_CUTIE", "PRET_RECEP", "PROL", "PROL_ACHI",
                "KILOGRAME", "GABARIT", "CHEIECMD", "LOT", "CODTXINV", "CODVAMAL",
                "CODCPV", "KILO_NET"
            };

            foreach (var columnName in columnNames)
            {
                try
                {
                    ordinals[columnName] = reader.GetOrdinal(columnName);
                }
                catch
                {
                    // Column might not exist, set to -1
                    ordinals[columnName] = -1;
                }
            }

            return ordinals;
        });
    }

    private StockItem MapToStockItem(DbfDataReader.DbfDataReader reader, Dictionary<string, int> ordinals, bool loadAllFields = false)
    {
        // Map only essential fields for performance
        var item = new StockItem
        {
            Code = GetInt32Safe(reader, ordinals, "COD"),
            Name = GetStringSafe(reader, ordinals, "DENUMIRE"),
            Category = GetStringSafe(reader, ordinals, "CATEGORIE"),
            Barcode = GetStringSafe(reader, ordinals, "COD_BARE"),
            Quantity = GetDecimalSafe(reader, ordinals, "CANTITATE"),
            Price = GetDecimalSafe(reader, ordinals, "PRET"),
            Unit = GetStringSafe(reader, ordinals, "UNIT_MAS"),
            Warehouse = GetInt32Safe(reader, ordinals, "DEPOZITUL")
        };

        // Load additional fields only when requested (e.g., for single item queries)
        if (loadAllFields)
        {
            item.IsDouble = GetBooleanSafe(reader, ordinals, "DUBLU");
            item.Date = GetDateTimeSafe(reader, ordinals, "DATA");
            item.ReservedQuantity = GetDecimalSafe(reader, ordinals, "CANT_REZER");
            item.Samples = GetDecimalSafe(reader, ordinals, "MOSTRE");
            item.PurchasePrice = GetDecimalSafe(reader, ordinals, "PRET_ACHI");
            item.Warranty = GetDateTimeSafe(reader, ordinals, "GARANTIE");
            item.IsWholesale = GetStringSafe(reader, ordinals, "LA_ENGROS");
            item.Notes = GetStringSafe(reader, ordinals, "OBSERVATII");
            item.Pieces = GetStringSafe(reader, ordinals, "BUC");
            item.VatPurchase = GetDecimalSafe(reader, ordinals, "PROTVAACHI");
            item.VatSale = GetDecimalSafe(reader, ordinals, "PROTVAVINZ");
            item.CorrespondingCode = GetInt32Safe(reader, ordinals, "COD_CORESP");
            item.Supplier = GetInt32Safe(reader, ordinals, "FURNIZOR");
            item.Address = GetStringSafe(reader, ordinals, "ADRESA");
            item.CustomsDuty = GetDecimalSafe(reader, ordinals, "PRO_VAMA");
            item.PriceB = GetDecimalSafe(reader, ordinals, "PRET_B");
            item.PriceC = GetDecimalSafe(reader, ordinals, "PRET_C");
            item.ShortCode = GetStringSafe(reader, ordinals, "SHORT_C");
            item.BoxCode = GetStringSafe(reader, ordinals, "COD_CUTIE");
            item.ReceptionPrice = GetDecimalSafe(reader, ordinals, "PRET_RECEP");
            item.Margin = GetDecimalSafe(reader, ordinals, "PROL");
            item.PurchaseMargin = GetDecimalSafe(reader, ordinals, "PROL_ACHI");
            item.Kilograms = GetDecimalSafe(reader, ordinals, "KILOGRAME");
            item.Dimensions = GetStringSafe(reader, ordinals, "GABARIT");
            item.OrderKey = GetStringSafe(reader, ordinals, "CHEIECMD");
            item.Lot = GetStringSafe(reader, ordinals, "LOT");
            item.TaxInvoiceCode = GetStringSafe(reader, ordinals, "CODTXINV");
            item.CustomsCode = GetStringSafe(reader, ordinals, "CODVAMAL");
            item.CpvCode = GetStringSafe(reader, ordinals, "CODCPV");
            item.NetWeight = GetDecimalSafe(reader, ordinals, "KILO_NET");
        }

        return item;
    }

    private string? GetStringSafe(DbfDataReader.DbfDataReader reader, Dictionary<string, int> ordinals, string fieldName)
    {
        try
        {
            if (!ordinals.TryGetValue(fieldName, out var ordinal) || ordinal < 0)
                return null;

            if (reader.IsDBNull(ordinal))
                return null;

            return reader.GetString(ordinal)?.Trim();
        }
        catch
        {
            return null;
        }
    }

    private int GetInt32Safe(DbfDataReader.DbfDataReader reader, Dictionary<string, int> ordinals, string fieldName)
    {
        try
        {
            if (!ordinals.TryGetValue(fieldName, out var ordinal) || ordinal < 0)
                return 0;

            if (reader.IsDBNull(ordinal))
                return 0;

            // Try direct int32 conversion first
            try
            {
                return reader.GetInt32(ordinal);
            }
            catch
            {
                // Fall back to generic value conversion
                var value = reader.GetValue(ordinal);
                if (value == null) return 0;

                var stringValue = value.ToString();
                if (string.IsNullOrWhiteSpace(stringValue)) return 0;

                // Try parsing as decimal first (handles decimal values)
                if (decimal.TryParse(stringValue, out decimal decimalResult))
                    return (int)decimalResult;

                return 0;
            }
        }
        catch
        {
            return 0;
        }
    }

    private decimal GetDecimalSafe(DbfDataReader.DbfDataReader reader, Dictionary<string, int> ordinals, string fieldName)
    {
        try
        {
            if (!ordinals.TryGetValue(fieldName, out var ordinal) || ordinal < 0)
                return 0m;

            if (reader.IsDBNull(ordinal))
                return 0m;

            // Try direct decimal conversion first
            try
            {
                return reader.GetDecimal(ordinal);
            }
            catch
            {
                // Fall back to generic value conversion
                var value = reader.GetValue(ordinal);
                if (value == null) return 0m;

                var stringValue = value.ToString();
                if (string.IsNullOrWhiteSpace(stringValue)) return 0m;

                if (decimal.TryParse(stringValue, out decimal result))
                    return result;

                return 0m;
            }
        }
        catch
        {
            return 0m;
        }
    }

    private bool GetBooleanSafe(DbfDataReader.DbfDataReader reader, Dictionary<string, int> ordinals, string fieldName)
    {
        try
        {
            if (!ordinals.TryGetValue(fieldName, out var ordinal) || ordinal < 0)
                return false;

            if (reader.IsDBNull(ordinal))
                return false;

            // Try direct boolean conversion first
            try
            {
                return reader.GetBoolean(ordinal);
            }
            catch
            {
                // Fall back to generic value conversion
                var value = reader.GetValue(ordinal);
                if (value == null) return false;

                var stringValue = value.ToString()?.ToUpperInvariant();
                return stringValue == "T" || stringValue == "TRUE" || stringValue == "1" ||
                       stringValue == "Y" || stringValue == "YES" || stringValue == "DA";
            }
        }
        catch
        {
            return false;
        }
    }

    private DateTime? GetDateTimeSafe(DbfDataReader.DbfDataReader reader, Dictionary<string, int> ordinals, string fieldName)
    {
        try
        {
            if (!ordinals.TryGetValue(fieldName, out var ordinal) || ordinal < 0)
                return null;

            if (reader.IsDBNull(ordinal))
                return null;

            // Try direct DateTime conversion first
            try
            {
                return reader.GetDateTime(ordinal);
            }
            catch
            {
                // Fall back to generic value conversion
                var value = reader.GetValue(ordinal);
                if (value == null) return null;

                var stringValue = value.ToString();
                if (string.IsNullOrWhiteSpace(stringValue)) return null;

                if (DateTime.TryParse(stringValue, out DateTime result))
                    return result;

                return null;
            }
        }
        catch
        {
            return null;
        }
    }

    private int GetCachedTotalCount(string dbfFilePath)
    {
        lock (_cacheLock)
        {
            // If cache is valid, return it and potentially refresh in background
            if (_cachedTotalCount.HasValue && DateTime.Now < _cacheExpiryTime)
            {
                // If cache is about to expire (within 2 minutes), refresh in background
                if (DateTime.Now.Add(TimeSpan.FromMinutes(2)) > _cacheExpiryTime &&
                    (_backgroundCacheTask == null || _backgroundCacheTask.IsCompleted))
                {
                    _backgroundCacheTask = Task.Run(() => RefreshCacheInBackground(dbfFilePath));
                }
                return _cachedTotalCount.Value;
            }

            // Cache expired or doesn't exist, need to recalculate
            return RefreshCacheSync(dbfFilePath);
        }
    }

    private int RefreshCacheSync(string dbfFilePath)
    {
        // Count records efficiently
        var options = new DbfDataReaderOptions
        {
            SkipDeletedRecords = true,
            Encoding = Encoding.GetEncoding(1252)
        };

        using var dbfReader = new DbfDataReader.DbfDataReader(dbfFilePath, options);
        int count = 0;
        while (dbfReader.Read())
        {
            count++;
        }

        lock (_cacheLock)
        {
            _cachedTotalCount = count;
            _cacheExpiryTime = DateTime.Now.Add(_cacheDuration);
        }

        return count;
    }

    private void RefreshCacheInBackground(string dbfFilePath)
    {
        try
        {
            // Count records in background
            var options = new DbfDataReaderOptions
            {
                SkipDeletedRecords = true,
                Encoding = Encoding.GetEncoding(1252)
            };

            using var dbfReader = new DbfDataReader.DbfDataReader(dbfFilePath, options);
            int count = 0;
            while (dbfReader.Read())
            {
                count++;
            }

            lock (_cacheLock)
            {
                _cachedTotalCount = count;
                _cacheExpiryTime = DateTime.Now.Add(_cacheDuration);
            }
        }
        catch
        {
            // Silently fail background refresh, will retry on next request
        }
    }
}