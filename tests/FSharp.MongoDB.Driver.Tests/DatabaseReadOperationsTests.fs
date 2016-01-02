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

open MongoDB.Driver.Core.Operations

open FSharp.Control

open FSharp.MongoDB.Driver
open FSharp.MongoDB.Driver.Operations
open FSharp.MongoDB.Driver.Operations.DatabaseReadOptions

open NUnit.Framework
open Swensen.Unquote

module DatabaseReadOperations =

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

    module AsyncListCollections =

        [<Test>]
        let ``test ListCollectionsOperation is configured by ListCollectionsOptions``() =
            let state = initialize()
            let db = state.Database
            use exec = state.OperationExecutor

            let filter = BsonDocument("name", BsonString "fsharp")
            let options : ListCollectionsOptions =
                { Filter = Some filter }

            db.AsyncListCollections(options)
            |> AsyncSeq.iter ignore
            |> Async.RunSynchronously
            |> ignore

            match exec.PopOldestCall<BsonDocument>() with
            | CursorReadOperation (readPref, (:? ListCollectionsOperation as op)) ->
                test <@ op.DatabaseNamespace = db.DatabaseNamespace @>
                test <@ op.Filter = filter @>
            | call -> failwithf "Expected AsyncListCollections to do a cursor read operation, but got %A" call
