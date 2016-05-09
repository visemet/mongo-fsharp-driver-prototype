(* Copyright (c) 2013 MongoDB, Inc.
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

namespace FSharp.MongoDB.Driver

open System
open System.Text
open System.Threading

open FSharp.Control

open MongoDB.Bson
open MongoDB.Bson.IO
open MongoDB.Bson.Serialization

open MongoDB.Driver
open MongoDB.Driver.Core.Bindings
open MongoDB.Driver.Core.Clusters
open MongoDB.Driver.Core.Operations
open MongoDB.Driver.Core.WireProtocol.Messages.Encoders

open FSharp.MongoDB.Driver.Operations
open FSharp.MongoDB.Driver.Operations.CollectionReadOptions

type internal NonOptionalMongoCollectionSettings =
    { AssignIdOnInsert : bool
      GuidRepresentation : GuidRepresentation
      ReadEncoding : UTF8Encoding
      ReadPreference : ReadPreference
      WriteConcern : WriteConcern
      WriteEncoding : UTF8Encoding }

type internal MongoCollection<'Document>(database:IMongoDatabase,
                                         collectionNamespace:CollectionNamespace,
                                         settings:NonOptionalMongoCollectionSettings,
                                         operationExecutor:IOperationExecutor) =

    let cluster = database.Client.Cluster

    let messageEncoderSettings =
        let add name value (messageEncoderSettings:MessageEncoderSettings) =
            messageEncoderSettings.Add(name, value)

        MessageEncoderSettings()
        |> add MessageEncoderSettingsName.GuidRepresentation settings.GuidRepresentation
        |> add MessageEncoderSettingsName.ReadEncoding settings.ReadEncoding
        |> add MessageEncoderSettingsName.WriteEncoding settings.WriteEncoding

    interface IMongoCollection<'Document> with

        member __.Database = database

        member __.CollectionNamespace = collectionNamespace

        member __.WithReadPreference readPreference =
            let newSettings = { settings with ReadPreference = readPreference }

            MongoCollection<'Document>(database,
                                       collectionNamespace,
                                       newSettings,
                                       operationExecutor)
            :> IMongoCollection<'Document>

        member __.AsyncAggregate<'Result> (pipeline, ?options, ?cancellationToken) =
            let opts = defaultArg options AggregateOptions.None
            let token = defaultArg cancellationToken Async.DefaultCancellationToken

            let resultSerializer = BsonSerializer.SerializerRegistry.GetSerializer<'Result>()
            let operation = AggregateOperation<'Result>(collectionNamespace,
                                                        pipeline,
                                                        resultSerializer,
                                                        messageEncoderSettings)

            opts.AllowDiskUse
            |> Option.iter (fun allowDiskUse -> operation.AllowDiskUse <- Nullable allowDiskUse)

            opts.BatchSize
            |> Option.iter (fun batchSize -> operation.BatchSize <- Nullable batchSize)

            opts.MaxTime
            |> Option.iter (fun maxTime -> operation.MaxTime <- Nullable maxTime)

            opts.UseCursor
            |> Option.iter (fun useCursor -> operation.UseCursor <- Nullable useCursor)

            use binding = new ReadPreferenceBinding(cluster, settings.ReadPreference)
            operation
            |> operationExecutor.AsyncExecuteCursorReadOperation binding token

        member __.AsyncCount (filter, ?options, ?cancellationToken) =
            let opts = defaultArg options CountOptions.None
            let token = defaultArg cancellationToken Async.DefaultCancellationToken

            let operation = CountOperation(collectionNamespace, messageEncoderSettings)
            operation.Filter <- filter

            opts.Hint
            |> Option.iter (function
                | IndexName idxName -> operation.Hint <- BsonString idxName
                | IndexSpec idxSpec -> operation.Hint <- idxSpec)

            opts.Limit
            |> Option.iter (fun limit -> operation.Limit <- Nullable limit)

            opts.MaxTime
            |> Option.iter (fun maxTime -> operation.MaxTime <- Nullable maxTime)

            opts.Skip
            |> Option.iter (fun skip -> operation.Skip <- Nullable skip)

            use binding = new ReadPreferenceBinding(cluster, settings.ReadPreference)
            operation
            |> operationExecutor.AsyncExecuteReadOperation binding token

        member __.AsyncDistinct<'Result> (fieldName, filter, ?options, ?cancellationToken) =
            let opts = defaultArg options DistinctOptions.None
            let token = defaultArg cancellationToken Async.DefaultCancellationToken

            let resultSerializer = BsonSerializer.SerializerRegistry.GetSerializer<'Result>()
            let operation = DistinctOperation<'Result>(collectionNamespace,
                                                       resultSerializer,
                                                       fieldName,
                                                       messageEncoderSettings)
            operation.Filter <- filter

            opts.MaxTime
            |> Option.iter (fun maxTime -> operation.MaxTime <- Nullable maxTime)

            use binding = new ReadPreferenceBinding(cluster, settings.ReadPreference)
            operation
            |> operationExecutor.AsyncExecuteCursorReadOperation binding token

        member coll.AsyncFind (filter, ?options:FindOptions, ?cancellationToken) =
            let opts = defaultArg options FindOptions.None
            let token = defaultArg cancellationToken Async.DefaultCancellationToken
            (coll :> IMongoCollection<'Document>).AsyncFind<'Document> (filter, opts, token)

        member __.AsyncFind<'Projection> (filter, ?options:FindOptions, ?cancellationToken) =
            let opts = defaultArg options FindOptions.None
            let token = defaultArg cancellationToken Async.DefaultCancellationToken

            let resultSerializer = BsonSerializer.SerializerRegistry.GetSerializer<'Projection>()
            let operation = FindOperation(collectionNamespace,
                                          resultSerializer,
                                          messageEncoderSettings)
            operation.Filter <- filter

            opts.AllowPartialResults
            |> Option.iter (fun allowPartialResults ->
                operation.AllowPartialResults <- allowPartialResults)

            opts.BatchSize
            |> Option.iter (fun batchSize -> operation.BatchSize <- Nullable batchSize)

            opts.Comment
            |> Option.iter (fun comment -> operation.Comment <- comment)

            opts.CursorType
            |> Option.iter (fun cursorType -> operation.CursorType <- cursorType)

            opts.Limit
            |> Option.iter (fun limit -> operation.Limit <- Nullable limit)

            opts.MaxTime
            |> Option.iter (fun maxTime -> operation.MaxTime <- Nullable maxTime)

            opts.Modifiers
            |> Option.iter (fun modifiers -> operation.Modifiers <- modifiers)

            opts.NoCursorTimeout
            |> Option.iter (fun noCursorTimeout -> operation.NoCursorTimeout <- noCursorTimeout)

            opts.Projection
            |> Option.iter (fun projection -> operation.Projection <- projection)

            opts.Skip
            |> Option.iter (fun skip -> operation.Skip <- Nullable skip)

            opts.Sort
            |> Option.iter (fun sort -> operation.Sort <- sort)

            use binding = new ReadPreferenceBinding(cluster, settings.ReadPreference)
            operation
            |> operationExecutor.AsyncExecuteCursorReadOperation binding token
