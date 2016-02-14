#### 0.0.1 - Async-client interface prototype (Not released)

  * Added support for serializing and deserializing `internal` F# types.
  * Changed `MongoCollection<'Document>` to return results of queries as `AsyncSeq` instances.
  * Added helpers to `MongoClient` for executing the "dropDatabase" and "listDatabases" commands.
  * Added helpers to `MongoDatabase` for executing the "create", "drop", "listCollections", and
    "renameCollection" commands.
  * Added helpers to `MongoCollection<'Document>` for executing the "aggregate", "count", and
    "distinct" commands.
  * Removed support for executing inserts, updates, and deletes against a collection.
  * Removed support for executing queries and updates using a `mongo { ... }` computation
    expression.
  * Removed support for specifying queries and updates as code quotations that are converted to
    `BsonDocument` instances.

#### 0.0.0 - End of intern project (Not released)

  * Support for serializing and deserializing F# types:
      * lists
      * maps
      * options
      * records
      * sets
      * discriminated unions
  * Support for executing basic CRUD operations against a collection.
  * Support for executing queries and updates using a `mongo { ... }` computation expression.
  * Support for specifying queries and updates as code quotations (in both type-checked and
    -unchecked forms) that are convertible to `BsonDocument` instances.
