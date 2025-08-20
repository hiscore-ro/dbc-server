using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace ExtractSchema
{
    class Program
    {
        static int Main(string[] args)
        {
            // Expand wildcards and collect all DBF files
            var dbfFiles = new List<string>();
            string outputPath = "config/schema.sql";
            
            // Default to tmp/*.DBF if no arguments provided
            if (args.Length == 0)
            {
                args = new[] { "tmp/*.DBF" };
                Console.WriteLine("Using default: tmp/*.DBF");
            }
            
            foreach (var arg in args)
            {
                // Check if it's the output file (doesn't end with .DBF)
                if (!arg.EndsWith(".DBF", StringComparison.OrdinalIgnoreCase) && 
                    !arg.Contains("*") && !arg.Contains("?"))
                {
                    outputPath = arg;
                    continue;
                }
                
                // Handle wildcards
                if (arg.Contains("*") || arg.Contains("?"))
                {
                    string directory = Path.GetDirectoryName(arg) ?? ".";
                    string pattern = Path.GetFileName(arg);
                    
                    if (Directory.Exists(directory))
                    {
                        var files = Directory.GetFiles(directory, pattern);
                        dbfFiles.AddRange(files.Where(f => f.EndsWith(".DBF", StringComparison.OrdinalIgnoreCase)));
                    }
                }
                else if (File.Exists(arg))
                {
                    dbfFiles.Add(arg);
                }
                else
                {
                    Console.WriteLine($"Warning: File not found: {arg}");
                }
            }

            if (dbfFiles.Count == 0)
            {
                Console.WriteLine("Error: No DBF files found");
                return 1;
            }

            try
            {
                var allSchemas = new StringBuilder();
                allSchemas.AppendLine("-- Generated from DBF schemas");
                allSchemas.AppendLine($"-- Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                allSchemas.AppendLine($"-- Total tables: {dbfFiles.Count}");
                allSchemas.AppendLine();

                foreach (var dbfPath in dbfFiles.OrderBy(f => f))
                {
                    Console.WriteLine($"\nProcessing: {dbfPath}");
                    
                    var header = ReadDbfHeader(dbfPath);
                    string tableName = Path.GetFileNameWithoutExtension(dbfPath).ToUpper();

                    string mdxPath = Path.ChangeExtension(dbfPath, ".MDX");
                    var indexes = new List<MdxTag>();
                    if (File.Exists(mdxPath))
                    {
                        Console.WriteLine($"  Reading MDX file: {mdxPath}");
                        indexes = ParseMdxFile(mdxPath);
                    }

                    Console.WriteLine($"  Found {header.Fields.Count} fields and {indexes.Count} indexes");

                    string sql = GenerateSqlSchema(tableName, header, indexes);
                    allSchemas.AppendLine(sql);
                    allSchemas.AppendLine();
                }

                string? outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                File.WriteAllText(outputPath, allSchemas.ToString());
                Console.WriteLine($"\nSchema exported to: {outputPath}");
                Console.WriteLine($"Total tables processed: {dbfFiles.Count}");

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        static DbfHeader ReadDbfHeader(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);

            byte version = reader.ReadByte();

            int year = 1900 + reader.ReadByte();
            int month = reader.ReadByte();
            int day = reader.ReadByte();
            var lastUpdate = new DateTime(year, month, day);

            int recordCount = reader.ReadInt32();
            short headerLength = reader.ReadInt16();
            short recordLength = reader.ReadInt16();

            reader.BaseStream.Seek(32, SeekOrigin.Begin);

            var fields = new List<DbfField>();
            int fieldStart = 32;

            while (fieldStart < headerLength - 1)
            {
                reader.BaseStream.Seek(fieldStart, SeekOrigin.Begin);

                byte[] nameBytes = reader.ReadBytes(11);
                int nullIndex = Array.FindIndex(nameBytes, b => b == 0);
                string name = nullIndex >= 0 
                    ? Encoding.ASCII.GetString(nameBytes, 0, nullIndex)
                    : Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');

                if (name.Length > 0)
                {
                    char fieldTypeChar = (char)reader.ReadByte();
                    reader.BaseStream.Seek(4, SeekOrigin.Current);
                    byte length = reader.ReadByte();
                    byte decimalCount = reader.ReadByte();

                    DbfFieldType fieldType = fieldTypeChar switch
                    {
                        'C' => new DbfFieldType.Character(length),
                        'N' => new DbfFieldType.Numeric(length, decimalCount),
                        'D' => new DbfFieldType.Date(),
                        'L' => new DbfFieldType.Logical(),
                        'M' => new DbfFieldType.Memo(),
                        'F' => new DbfFieldType.Float(length, decimalCount),
                        'Y' => new DbfFieldType.Currency(),
                        'I' => new DbfFieldType.Integer(),
                        'T' => new DbfFieldType.DateTime(),
                        _ => new DbfFieldType.Unknown(fieldTypeChar)
                    };

                    fields.Add(new DbfField
                    {
                        Name = name,
                        FieldType = fieldType,
                        Length = length,
                        DecimalCount = decimalCount
                    });
                }

                fieldStart += 32;
            }

            return new DbfHeader
            {
                Version = version,
                LastUpdate = lastUpdate,
                RecordCount = recordCount,
                HeaderLength = headerLength,
                RecordLength = recordLength,
                Fields = fields
            };
        }

        static string ParseDbfFieldTypeToSql(DbfField field)
        {
            return field.FieldType switch
            {
                DbfFieldType.Character c => c.Length <= 255 ? $"VARCHAR({c.Length})" : "TEXT",
                DbfFieldType.Numeric n => n.DecimalCount == 0
                    ? n.Length <= 4 ? "SMALLINT"
                    : n.Length <= 9 ? "INTEGER"
                    : "BIGINT"
                    : $"DECIMAL({n.Length},{n.DecimalCount})",
                DbfFieldType.Date => "DATE",
                DbfFieldType.Logical => "BOOLEAN",
                DbfFieldType.Memo => "TEXT",
                DbfFieldType.Float f => $"DECIMAL({f.Length},{f.DecimalCount})",
                DbfFieldType.Currency => "DECIMAL(19,4)",
                DbfFieldType.Integer => "INTEGER",
                DbfFieldType.DateTime => "TIMESTAMP",
                DbfFieldType.Unknown u => $"VARCHAR(255) /* Unknown type: {u.TypeChar} */",
                _ => "VARCHAR(255)"
            };
        }

        static List<MdxTag> ParseMdxFile(string mdxPath)
        {
            if (!File.Exists(mdxPath))
                return new List<MdxTag>();

            try
            {
                using var fs = new FileStream(mdxPath, FileMode.Open, FileAccess.Read);
                using var reader = new BinaryReader(fs);

                byte signature = reader.ReadByte();
                if (signature != 0x02)
                {
                    Console.WriteLine($"Warning: MDX file signature not recognized (0x{signature:X2})");
                    return new List<MdxTag>();
                }

                reader.BaseStream.Seek(28, SeekOrigin.Begin);
                short tagCount = reader.ReadInt16();

                var tags = new List<MdxTag>();

                reader.BaseStream.Seek(544, SeekOrigin.Begin);

                for (int i = 0; i < tagCount; i++)
                {
                    long tagStart = 544 + (i * 32);
                    reader.BaseStream.Seek(tagStart, SeekOrigin.Begin);

                    byte[] nameBytes = reader.ReadBytes(11);
                    int nullIndex = Array.FindIndex(nameBytes, b => b == 0);
                    string name = nullIndex >= 0
                        ? Encoding.ASCII.GetString(nameBytes, 0, nullIndex)
                        : Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');

                    if (name.Length > 0 && name != "DELETED")
                    {
                        tags.Add(new MdxTag
                        {
                            Name = name,
                            KeyExpression = name,
                            ForExpression = null,
                            Unique = false,
                            Descending = false
                        });
                    }
                }

                return tags;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not parse MDX file: {ex.Message}");
                return new List<MdxTag>();
            }
        }

        static string GenerateSqlSchema(string tableName, DbfHeader header, List<MdxTag> indexes)
        {
            var sb = new StringBuilder();

            sb.AppendLine("-- Generated from DBF schema");
            sb.AppendLine($"-- Table: {tableName}");
            sb.AppendLine($"-- Last Updated: {header.LastUpdate:yyyy-MM-dd}");
            sb.AppendLine($"-- Record Count: {header.RecordCount}");
            sb.AppendLine();

            sb.AppendLine($"CREATE TABLE {tableName} (");

            var fieldLines = header.Fields.Select(field =>
            {
                string sqlType = ParseDbfFieldTypeToSql(field);
                return $"    {field.Name} {sqlType}";
            });

            sb.AppendLine(string.Join(",\n", fieldLines));
            sb.AppendLine(");");

            if (indexes.Any())
            {
                sb.AppendLine();
                sb.AppendLine("-- Indexes from MDX file");
                foreach (var index in indexes)
                {
                    string indexType = index.Unique ? "UNIQUE " : "";
                    string direction = index.Descending ? " DESC" : "";
                    sb.AppendLine($"CREATE {indexType}INDEX idx_{tableName}_{index.Name} ON {tableName} ({index.KeyExpression}{direction});");
                }
            }

            return sb.ToString();
        }
    }

    class DbfHeader
    {
        public byte Version { get; set; }
        public DateTime LastUpdate { get; set; }
        public int RecordCount { get; set; }
        public int HeaderLength { get; set; }
        public int RecordLength { get; set; }
        public List<DbfField> Fields { get; set; } = new List<DbfField>();
    }

    class DbfField
    {
        public string Name { get; set; } = string.Empty;
        public DbfFieldType FieldType { get; set; } = new DbfFieldType.Unknown('?');
        public int Length { get; set; }
        public int DecimalCount { get; set; }
    }

    abstract class DbfFieldType
    {
        public class Character : DbfFieldType
        {
            public int Length { get; }
            public Character(int length) => Length = length;
        }

        public class Numeric : DbfFieldType
        {
            public int Length { get; }
            public int DecimalCount { get; }
            public Numeric(int length, int decimalCount)
            {
                Length = length;
                DecimalCount = decimalCount;
            }
        }

        public class Date : DbfFieldType { }
        public class Logical : DbfFieldType { }
        public class Memo : DbfFieldType { }

        public class Float : DbfFieldType
        {
            public int Length { get; }
            public int DecimalCount { get; }
            public Float(int length, int decimalCount)
            {
                Length = length;
                DecimalCount = decimalCount;
            }
        }

        public class Currency : DbfFieldType { }
        public class Integer : DbfFieldType { }
        public class DateTime : DbfFieldType { }

        public class Unknown : DbfFieldType
        {
            public char TypeChar { get; }
            public Unknown(char typeChar) => TypeChar = typeChar;
        }
    }

    class MdxTag
    {
        public string Name { get; set; } = string.Empty;
        public string KeyExpression { get; set; } = string.Empty;
        public string? ForExpression { get; set; }
        public bool Unique { get; set; }
        public bool Descending { get; set; }
    }
}