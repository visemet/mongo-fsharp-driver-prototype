(* Copyright (c) 2015 MongoDB, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *)

namespace FSharp.MongoDB.Driver.Tests

open MongoDB.Bson

open MongoDB.Driver
open MongoDB.Driver.Core.Operations

open FSharp.Control

open FSharp.MongoDB.Driver
open FSharp.MongoDB.Driver.Operations
open FSharp.MongoDB.Driver.Operations.DatabaseWriteOptions

open NUnit.Framework
open Swensen.Unquote

module DatabaseWriteOperations =

    type DatabaseWithOperationExecutor =
        { Database : IMongoDatabase
          OperationExecutor : OperationExecutorSpy }

    let initialize() =
        let exec = new OperationExecutorSpy()
        let client =
            MongoClient(new DummyCluster(), MongoClientSettings.None, exec)
            :> IMongoClient
        let db = client.GetDatabase "test"

        { Database = db
          OperationExecutor = exec }

    module AsyncCreateCollection =

        [<Test>]
        let ``test CreateCollectionOperation is configured by CreateCollectionOptions``() =
            let state = initialize()
            let db = state.Database
            use exec = state.OperationExecutor

            let collName = "fsharp"
            let options : CreateCollectionOptions =
                { AutoIndexId = Some false
                  Capped = Some true
                  MaxDocuments = Some 100L
                  MaxSize = Some 4096L
                  StorageEngine = Some (BsonDocument ("fake", BsonBoolean true))
                  UsePowerOf2Sizes = Some false }

            db.AsyncCreateCollection(collName, options)
            |> Async.RunSynchronously
            |> ignore

            match exec.PopOldestCall<BsonDocument>() with
            | WriteOperation (:? CreateCollectionOperation as op) ->
                test <@ op.CollectionNamespace = CollectionNamespace(db.DatabaseNamespace, collName) @>
                test <@ op.AutoIndexId.Value = options.AutoIndexId.Value @>
                test <@ op.Capped.Value = options.Capped.Value @>
                test <@ op.MaxDocuments.Value = options.MaxDocuments.Value @>
                test <@ op.MaxSize.Value = options.MaxSize.Value @>
                test <@ op.StorageEngine = options.StorageEngine.Value @>
                test <@ op.UsePowerOf2Sizes.Value = options.UsePowerOf2Sizes.Value @>
            | call -> failwithf "Expected AsyncCreateCollection to do a write operation, but got %A" call

    module AsyncRenameCollection =

        [<Test>]
        let ``test RenameCollectionOperation is configured by RenameCollectionOptions``() =
            let state = initialize()
            let db = state.Database
            use exec = state.OperationExecutor

            let oldName = "before"
            let newName = "after"
            let options : RenameCollectionOptions =
                { DropTarget = Some true }

            db.AsyncRenameCollection(oldName, newName, options)
            |> Async.RunSynchronously
            |> ignore

            match exec.PopOldestCall<BsonDocument>() with
            | WriteOperation (:? RenameCollectionOperation as op) ->
                test <@ op.CollectionNamespace = CollectionNamespace(db.DatabaseNamespace, oldName) @>
                test <@ op.NewCollectionNamespace = CollectionNamespace(db.DatabaseNamespace, newName) @>
                test <@ op.DropTarget.Value = options.DropTarget.Value @>
            | call -> failwithf "Expected AsyncRenameCollection to do a write operation, but got %A" call
