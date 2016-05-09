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

open System
open System.Collections.Generic

open MongoDB.Bson

open MongoDB.Driver.Core.Operations

open FSharp.Control

open FSharp.MongoDB.Driver
open FSharp.MongoDB.Driver.Operations
open FSharp.MongoDB.Driver.Operations.CollectionReadOptions

open NUnit.Framework
open Swensen.Unquote

module CollectionReadOperations =

    type CollectionWithOperationExecutor<'Document> =
        { Collection : IMongoCollection<'Document>
          OperationExecutor : OperationExecutorSpy }

    let initialize() =
        let exec = new OperationExecutorSpy()
        let client =
            MongoClient(new DummyCluster(), MongoClientSettings.None, exec)
            :> IMongoClient
        let db = client.GetDatabase "test"
        let coll = db.GetCollection<BsonDocument> "fsharp"

        { Collection = coll
          OperationExecutor = exec }

    module AsyncAggregate =

        [<Test>]
        let ``test AggregationOperation is configured by AggregationOptions``() =
            let state = initialize()
            let coll = state.Collection
            use exec = state.OperationExecutor

            let pipeline = [ BsonDocument("$match", BsonDocument("a", BsonInt32 1)) ]
            let options : AggregateOptions =
                { AllowDiskUse = Some false
                  BatchSize = Some 10
                  MaxTime = Some (TimeSpan.FromSeconds 30.0)
                  UseCursor = Some true }

            coll.AsyncAggregate<BsonDocument>(pipeline, options)
            |> AsyncSeq.iter ignore
            |> Async.RunSynchronously
            |> ignore

            match exec.PopOldestCall<BsonDocument>() with
            | CursorReadOperation (readPref, (:? AggregateOperation<BsonDocument> as op)) ->
                test <@ op.CollectionNamespace = coll.CollectionNamespace @>
                test <@ List.ofSeq op.Pipeline = pipeline @>
                test <@ op.AllowDiskUse.Value = options.AllowDiskUse.Value @>
                test <@ op.BatchSize.Value = options.BatchSize.Value @>
                test <@ op.MaxTime.Value = options.MaxTime.Value @>
                test <@ op.UseCursor.Value = options.UseCursor.Value @>
            | call -> failwithf "Expected AsyncAggregate to do a cursor read operation, but got %A" call

    module AsyncCount =

        [<Test>]
        let ``test CountOperation is configured by CountOptions``() =
            let state = initialize()
            let coll = state.Collection
            use exec = state.OperationExecutor

            let filter = BsonDocument("a", BsonInt32 1)
            let options : CountOptions =
                { Hint = None
                  Limit = Some 10L
                  MaxTime = Some (TimeSpan.FromSeconds 30.0)
                  Skip = Some 20L }

            coll.AsyncCount(filter, options)
            |> Async.RunSynchronously
            |> ignore

            match exec.PopOldestCall<int64>() with
            | ReadOperation (readPref, (:? CountOperation as op)) ->
                test <@ op.CollectionNamespace = coll.CollectionNamespace @>
                test <@ op.Filter = filter @>
                test <@ op.Hint = null @>
                test <@ op.Limit.Value = options.Limit.Value @>
                test <@ op.MaxTime.Value = options.MaxTime.Value @>
                test <@ op.Skip.Value = options.Skip.Value @>
            | call -> failwithf "Expected AsyncCount to do a read operation, but got %A" call

        [<Test>]
        let ``test specifying Hint as an IndexName in CountOptions``() =
            let state = initialize()
            let coll = state.Collection
            use exec = state.OperationExecutor

            let filter = BsonDocument()
            let indexName = "a_1"
            let options : CountOptions =
                { CountOptions.None with Hint = Some (IndexName indexName) }

            coll.AsyncCount(filter, options)
            |> Async.RunSynchronously
            |> ignore

            match exec.PopOldestCall<int64>() with
            | ReadOperation (readPref, (:? CountOperation as op)) ->
                test <@ op.CollectionNamespace = coll.CollectionNamespace @>
                test <@ op.Filter = filter @>
                test <@ op.Hint = (BsonString indexName :> BsonValue) @>
                test <@ op.Limit = Nullable() @>
                test <@ op.MaxTime = Nullable() @>
                test <@ op.Skip = Nullable() @>
            | call -> failwithf "Expected AsyncCount to do a read operation, but got %A" call

        [<Test>]
        let ``test specifying Hint as an IndexSpec in CountOptions``() =
            let state = initialize()
            let coll = state.Collection
            use exec = state.OperationExecutor

            let filter = BsonDocument()
            let indexSpec = BsonDocument("a", BsonInt32 1)
            let options : CountOptions =
                { CountOptions.None with Hint = Some (IndexSpec indexSpec) }

            coll.AsyncCount(filter, options)
            |> Async.RunSynchronously
            |> ignore

            match exec.PopOldestCall<int64>() with
            | ReadOperation (readPref, (:? CountOperation as op)) ->
                test <@ op.CollectionNamespace = coll.CollectionNamespace @>
                test <@ op.Filter = filter @>
                test <@ op.Hint = (indexSpec :> BsonValue) @>
                test <@ op.Limit = Nullable() @>
                test <@ op.MaxTime = Nullable() @>
                test <@ op.Skip = Nullable() @>
            | call -> failwithf "Expected AsyncCount to do a read operation, but got %A" call

    module AsyncDistinct =

        [<Test>]
        let ``test DistinctOperation is configured by DistinctOptions``() =
            let state = initialize()
            let coll = state.Collection
            use exec = state.OperationExecutor

            let fieldName = "b"
            let filter = BsonDocument("a", BsonInt32 1)
            let options : DistinctOptions =
                { MaxTime = Some (TimeSpan.FromSeconds 30.0) }

            coll.AsyncDistinct<float>(fieldName, filter, options)
            |> AsyncSeq.iter ignore
            |> Async.RunSynchronously
            |> ignore

            match exec.PopOldestCall<float>() with
            | CursorReadOperation (readPref, (:? DistinctOperation<float> as op)) ->
                test <@ op.CollectionNamespace = coll.CollectionNamespace @>
                test <@ op.FieldName = fieldName @>
                test <@ op.Filter = filter @>
                test <@ op.MaxTime.Value = options.MaxTime.Value @>
            | call -> failwithf "Expected AsyncDistinct to do a cursor read operation, but got %A" call

    module AsyncFind =

        [<Test>]
        let ``test FindOperation is configured by FindOptions``() =
            let state = initialize()
            let coll = state.Collection
            use exec = state.OperationExecutor

            let filter = BsonDocument("a", BsonInt32 1)
            let options : FindOptions =
                { AllowPartialResults = Some false
                  BatchSize = Some 10
                  Comment = Some "comment"
                  CursorType = Some CursorType.Tailable
                  Limit = Some 20
                  MaxTime = Some (TimeSpan.FromSeconds 30.0)
                  Modifiers = Some (BsonDocument("$explain", BsonBoolean true))
                  NoCursorTimeout = Some true
                  Projection = Some (BsonDocument("b", BsonInt32 1))
                  Skip = Some 5
                  Sort = Some (BsonDocument("c", BsonInt32 -1)) }

            coll.AsyncFind<BsonDocument>(filter, options)
            |> AsyncSeq.iter ignore
            |> Async.RunSynchronously
            |> ignore

            match exec.PopOldestCall<BsonDocument>() with
            | CursorReadOperation (readPref, (:? FindOperation<BsonDocument> as op)) ->
                test <@ op.CollectionNamespace = coll.CollectionNamespace @>
                test <@ op.Filter = filter @>
                test <@ op.AllowPartialResults = options.AllowPartialResults.Value @>
                test <@ op.BatchSize.Value = options.BatchSize.Value @>
                test <@ op.Comment = options.Comment.Value @>
                test <@ op.CursorType = options.CursorType.Value @>
                test <@ op.Limit.Value = options.Limit.Value @>
                test <@ op.MaxTime.Value = options.MaxTime.Value @>
                test <@ op.Modifiers = options.Modifiers.Value @>
                test <@ op.NoCursorTimeout = options.NoCursorTimeout.Value @>
                test <@ op.Projection = options.Projection.Value @>
                test <@ op.Skip.Value = options.Skip.Value @>
                test <@ op.Sort = options.Sort.Value @>
            | call -> failwithf "Expected AsyncFind to do a cursor read operation, but got %A" call
