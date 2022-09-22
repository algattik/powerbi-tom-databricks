# powerbi-tom-databricks
Programming the Power BI Table Object Model to connect to Databricks tables and automate the generation of a tabular model.

This demo uses the Azure Databricks [TPC-H sample dataset](https://learn.microsoft.com/azure/databricks/dbfs/databricks-datasets). The dataset is available out of the box in Databricks, as Delta Parquet files as well as a table in the Hive metastore.

The TPC-H dataset contains 8 tables related by foreign keys. The sample data contains data for the tables with up to 30M rows per table, but foreign keys are not available in Databricks.

The program uses a file [datamodel.json](GenerateDatabricksTOM/datamodel.json) in a custom format, containing the definition of the tabular model (tables, columns and relationships) and its mapping to Databricks tables and columns.

Note: In the TPC-H dataset, the relationship from `lineitem` to `partsupp` is a foreign key over two columns. As the tabular model only supports single column relationships, only the first column is used (`partkey`).

The images below compare the `partsupp` table in Databricks and Power BI:

| ![partsupp-databricks](docs/partsupp-databricks.png) | ![partsupp-powerbi](docs/partsupp-powerbi.png) |
| ---------------------------------------------------- | ---------------------------------------------- |

## Prerequisites

- .Net SDK 6.0
- Power BI Desktop
- Azure Databricks with a cluster running (tested with Databricks Runtime 9.1)

## How to

Compile the program.

```
dotnet build
```

Update the  `modelgen.pbitool.json` :

- Adapt the paths into your installation path (replacing the `C:\\path\\to` strings).
- Use the paths in your Databricks cluster settings, Configuration -> Advanced options -> JDBC/ODBC (`Server Hostname` and `HTTP Path`) to update the value in the `arguments` string

Copy the file to your `C:\Program Files (x86)\Common Files\Microsoft Shared\Power BI Desktop\External Tools` directory.

Restart Power BI Desktop if it's already open.

In Power BI Desktop:

1. Click Home -> Enter Data and enter some random data, then click Load. This step serves no purpose, but is required as otherwise the next step fails with an error:

   ![empty-model-error](docs/empty-model-error.png)

2. click External Tools -> Model Generator:

   ![Power BI Toolbar](docs/powerbi-toolbar.png)

   This populates the model and relationships from [datamodel.json](GenerateDatabricksTOM/datamodel.json).

   ![tabular-model](docs/tabular-model.png)

3. When prompted for Databricks credentials, select Azure Active Directory.

4. You can now use the data in report visuals:

   ![powerbi-map](docs/powerbi-map.png)

## Issues

- Can't populate an empty model (error `unknown variable or function Partition_`*`guid`*, see workaround with Home -> Enter Data above)
- Can't save resulting file (the banner `There are pending changes in your queries that haven't been applied` appears, and clicking `Apply` reverts the model updates)

## References

- [Microsoft Learn: Programming Power BI datasets with the Tabular Object Model (TOM)](https://learn.microsoft.com/analysis-services/tom/tom-pbi-datasets)
- [Power BI Dev Camp: Programming Datasets using the Tabular Object Model](https://powerbidevcamp.powerappsportals.com/sessions/session04/)
