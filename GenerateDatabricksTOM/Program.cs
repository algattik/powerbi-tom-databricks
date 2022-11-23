using Microsoft.AnalysisServices.Tabular;
using Newtonsoft.Json;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace GenerateDatabricksTOM
{
    static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 5)
            {
                throw new ArgumentException("Wrong usage");
            }

            // create the connect string
            var connectString = args[0];
            var databaseName = args[1];
            var modelDefinitionFile = args[2];
            var databricksServer = args[3];
            var httpPath = args[4];

            // connect to the Power BI workspace referenced in connect string
            var server = new Server();
            server.Connect(connectString);

            // enumerate through datasets in workspace to display their names
            foreach (Database database in server.Databases)
            {
                Console.WriteLine(database.Name);
            }

            var db = server.Databases.FindByName(databaseName);
            if (db is null)
            {
                // Attempt creating a new Database, but model.SaveChanges() fails in this case later on
                // see https://community.powerbi.com/t5/Service/Can-I-create-Dataset-using-XMLA-Endpoint/m-p/2310718
                db = new Database
                {
                    Name = databaseName,
                    Model = new Model { }
                };
                server.Databases.Add(db);
            }

            var model = db.Model;

            var modelDefinition = ReadModelDefinitionFile(modelDefinitionFile);

            model.Relationships.Clear();
            model.Tables.Clear();
            foreach (var tableDefinition in modelDefinition.Tables)
            {
                var table = new Table
                {
                    Name = tableDefinition.Name,
                    Description = $"{tableDefinition.Name} table",
                    Partitions =
                    {
                        new Partition
                        {
                            Name = "All Data",
                            Mode = ModeType.DirectQuery,
                            Source = new MPartitionSource
                            {
                                // M code for query
                                Expression =
                                    $@"let
    Source = Databricks.Catalogs(""{databricksServer}"", ""{httpPath}"", [Catalog=null, Database=null, EnableAutomaticProxyDiscovery=null]),
    database = Source{{[Name=""{tableDefinition.Database}"",Kind=""Database""]}}[Data],
    schema = database{{[Name=""{tableDefinition.Schema}"",Kind=""Schema""]}}[Data],
    table = schema{{[Name=""{tableDefinition.SourceTable}"",Kind=""Table""]}}[Data]
in
    table"
                            }
                        }
                    }
                };

                foreach (var columnDefinition in tableDefinition.Columns)
                {
                    table.Columns.Add(new DataColumn
                    {
                        Name = columnDefinition.Name,
                        DataType = columnDefinition.DataType,
                        SourceColumn = columnDefinition.SourceColumn
                    });
                }

                model.Tables.Add(table);
            }

            foreach (var relationshipDefinition in modelDefinition.Relationships)
            {
                model.Relationships.Add(new SingleColumnRelationship
                {
                    Name = relationshipDefinition.Name,
                    FromColumn = model.Tables.Find(relationshipDefinition.ToTable).Columns
                        .Find(relationshipDefinition.ToColumn),
                    FromCardinality = relationshipDefinition.ToCardinality,
                    ToColumn = model.Tables.Find(relationshipDefinition.FromTable).Columns
                        .Find(relationshipDefinition.FromColumn),
                    ToCardinality = relationshipDefinition.FromCardinality,
                });
            }

            model.SaveChanges();
        }

        private static ModelDefinition ReadModelDefinitionFile(string modelDefinitionFile)
        {
            var serializer = new JsonSerializer();
            using var sr = new StreamReader(modelDefinitionFile);
            using var jsonTextReader = new JsonTextReader(sr);
            return serializer.Deserialize<ModelDefinition>(jsonTextReader)
                ?? throw new NullReferenceException("Error reading model definition file");
        }
    }

    internal class ModelDefinition
    {
        public List<TableDefinition> Tables { get; set; }
        public List<RelationshipDefinition> Relationships { get; set; }
    }


    internal class TableDefinition
    {
        public string Database { get; set; }
        public string Schema { get; set; }
        public string Name { get; set; }
        public string SourceTable { get; set; }
        public List<ColumnDefinition> Columns { get; set; }
    }

    internal class ColumnDefinition
    {
        public string Name { get; set; }
        public DataType DataType { get; set; }
        public string SourceColumn { get; set; }
    }

    internal class RelationshipDefinition
    {
        public string Name { get; set; }
        public List<ColumnDefinition> Columns { get; set; }
        public string FromTable { get; set; }
        public string FromColumn { get; set; }
        public RelationshipEndCardinality FromCardinality { get; set; }
        public string ToTable { get; set; }
        public string ToColumn { get; set; }
        public RelationshipEndCardinality ToCardinality { get; set; }
    }
}