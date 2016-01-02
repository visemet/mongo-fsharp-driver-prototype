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

open NUnit.Framework
open Swensen.Unquote

module ClientWriteOperations =

    type ClientWithOperationExecutor =
        { Client : IMongoClient
          OperationExecutor : OperationExecutorSpy }

    let initialize() =
        let exec = new OperationExecutorSpy()
        let client =
            MongoClient(new DummyCluster(), MongoClientSettings.None, exec)
            :> IMongoClient

        { Client = client
          OperationExecutor = exec }

    module AsyncDropDatabase =

        [<Test>]
        let ``test DropDatabaseOperation is configured``() =
            let state = initialize()
            let client = state.Client
            use exec = state.OperationExecutor

            let dbName = "test"
            client.AsyncDropDatabase(dbName)
            |> Async.RunSynchronously
            |> ignore

            match exec.PopOldestCall<BsonDocument>() with
            | WriteOperation (:? DropDatabaseOperation as op) ->
                test <@ op.DatabaseNamespace = DatabaseNamespace(dbName) @>
            | call -> failwithf "Expected AsyncDropDatabase to do a write operation, but got %A" call
