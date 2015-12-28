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
