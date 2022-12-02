# SqlInsertScriptTool2
This small utility connects to a SQL Server database to get table and column data, then generates a batch of insert statements for the specified tables.
The source for the insert statements can either be the database itself or CSV files in a specified directory.

(The 2 at the end is because I started work on the tool in Go and made a lot of progress but ran into some weirdness with drivers and decide to fall back to C#.)


### Command-line options:
#### --server
IP address or name of the database server where the metadata, and optionally the source data, is stored

#### --port
(optional) port number of the server

#### --database
Database name where the metadata, and optionally the source data, is stored

#### --userID
User ID for connecting to the database (Windows auth not currently supported)

#### --password
Password for connecting to the database

#### --tables
Comma-separated list of tables to create insert statements for.
Statements will be created in the order of this list.

#### --includeDeletes
`true` to include a list of delete statements before the inserts.
Delete statements will be created in reverse order of `--tables`.
Defaults to `false`

#### --sourceMode
`DB` (default) to pull source data from the database; `CSV` to pull it from files in `CsvDirectory`.

#### --csvDirectory
Required if `--sourceMode` is `CSV`. 
A directory containing CSV files to generate insert statements from.
The CSV file should be [tableName].csv.
The CSV should include column headers.
