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

open MongoDB.Bson
open MongoDB.Bson.Serialization

open MongoDB.Driver
open MongoDB.Driver.Core
open MongoDB.Driver.Core.Bindings
open MongoDB.Driver.Core.Operations
open MongoDB.Driver.Core.WireProtocol.Messages.Encoders

open FSharp.MongoDB.Driver.Operations
open FSharp.MongoDB.Driver.Operations.DatabaseReadOptions
open FSharp.MongoDB.Driver.Operations.DatabaseWriteOptions

type internal NonOptionalMongoDatabaseSettings =
    { GuidRepresentation : GuidRepresentation
      ReadEncoding : UTF8Encoding
      ReadPreference : ReadPreference
      WriteConcern : WriteConcern
      WriteEncoding : UTF8Encoding }

type internal MongoDatabase(client:IMongoClient,
                            databaseNamespace:DatabaseNamespace,
                            settings:NonOptionalMongoDatabaseSettings,
                            operationExecutor:IOperationExecutor) =

    let databaseSettings = settings

    let messageEncoderSettings =
        let add name value (messageEncoderSettings:MessageEncoderSettings) =
            messageEncoderSettings.Add(name, value)

        MessageEncoderSettings()
        |> add MessageEncoderSettingsName.GuidRepresentation settings.GuidRepresentation
        |> add MessageEncoderSettingsName.ReadEncoding settings.ReadEncoding
        |> add MessageEncoderSettingsName.WriteEncoding settings.WriteEncoding

    interface IMongoDatabase with

        member __.Client = client

        member __.DatabaseNamespace = databaseNamespace

        member db.GetCollection<'Document> (name, ?settings) =
            let collectionOverrides = defaultArg settings MongoCollectionSettings.None
            let collectionSettings : NonOptionalMongoCollectionSettings =
                { AssignIdOnInsert = defaultArg collectionOverrides.AssignIdOnInsert true
                  GuidRepresentation = (defaultArg collectionOverrides.GuidRepresentation
                                                   databaseSettings.GuidRepresentation)
                  ReadEncoding = (defaultArg collectionOverrides.ReadEncoding
                                             databaseSettings.ReadEncoding)
                  ReadPreference = (defaultArg collectionOverrides.ReadPreference
                                               databaseSettings.ReadPreference)
                  WriteConcern = (defaultArg collectionOverrides.WriteConcern
                                             databaseSettings.WriteConcern)
                  WriteEncoding = (defaultArg collectionOverrides.WriteEncoding
                                              databaseSettings.WriteEncoding) }

            let collectionNamespace = CollectionNamespace(databaseNamespace, name)
            MongoCollection<'Document>(db,
                                       collectionNamespace,
                                       collectionSettings,
                                       operationExecutor)
            :> IMongoCollection<'Document>

        member __.AsyncCreateCollection (name, ?options, ?cancellationToken) =
            let createCollectionOptions = defaultArg options CreateCollectionOptions.None
            let token = defaultArg cancellationToken Async.DefaultCancellationToken

            let collectionNamespace = CollectionNamespace(databaseNamespace, name)
            let operation = CreateCollectionOperation(collectionNamespace, messageEncoderSettings)

            operation.AutoIndexId <- Option.toNullable createCollectionOptions.AutoIndexId
            operation.Capped <- Option.toNullable createCollectionOptions.Capped
            operation.MaxDocuments <- Option.toNullable createCollectionOptions.MaxDocuments
            operation.MaxSize <- Option.toNullable createCollectionOptions.MaxSize

            createCollectionOptions.StorageEngine
            |> Option.iter (fun storageEngine -> operation.StorageEngine <- storageEngine)

            operation.UsePowerOf2Sizes <- Option.toNullable createCollectionOptions.UsePowerOf2Sizes

            use binding = new WritableServerBinding(client.Cluster)
            operation
            |> operationExecutor.AsyncExecuteWriteOperation binding token
            |> Async.Ignore

        member __.AsyncDropCollection (name, ?cancellationToken) =
            let token = defaultArg cancellationToken Async.DefaultCancellationToken

            let collectionNamespace = CollectionNamespace(databaseNamespace, name)
            let operation = DropCollectionOperation(collectionNamespace, messageEncoderSettings)

            use binding = new WritableServerBinding(client.Cluster)
            operation
            |> operationExecutor.AsyncExecuteWriteOperation binding token
            |> Async.Ignore

        member __.AsyncListCollections (?options, ?cancellationToken) =
            let listCollectionsOptions = defaultArg options ListCollectionsOptions.None
            let token = defaultArg cancellationToken Async.DefaultCancellationToken

            let operation = ListCollectionsOperation(databaseNamespace, messageEncoderSettings)

            listCollectionsOptions.Filter
            |> Option.iter (fun filter -> operation.Filter <- filter)

            use binding = new ReadPreferenceBinding(client.Cluster, settings.ReadPreference)
            operation
            |> operationExecutor.AsyncExecuteCursorReadOperation binding token

        member __.AsyncRenameCollection (oldName, newName, ?options, ?cancellationToken) =
            let renameCollectionOptions = defaultArg options RenameCollectionOptions.None
            let token = defaultArg cancellationToken Async.DefaultCancellationToken

            let oldCollectionNamespace = CollectionNamespace(databaseNamespace, oldName)
            let newCollectionNamespace = CollectionNamespace(databaseNamespace, newName)
            let operation = RenameCollectionOperation(oldCollectionNamespace,
                                                      newCollectionNamespace,
                                                      messageEncoderSettings)

            operation.DropTarget <- Option.toNullable renameCollectionOptions.DropTarget

            use binding = new WritableServerBinding(client.Cluster)
            operation
            |> operationExecutor.AsyncExecuteWriteOperation binding token
            |> Async.Ignore

        member __.AsyncRunCommand<'Result> (command, ?readPreference, ?cancellationToken) =
            // According to the Server Selection specification, the generic "runCommand" method must
            // act as a read operation and default to using a read preference of "primary".
            let commandReadPreference = defaultArg readPreference ReadPreference.Primary
            let token = defaultArg cancellationToken Async.DefaultCancellationToken

            let resultSerializer = BsonSerializer.SerializerRegistry.GetSerializer<'Result>()
            let operation = ReadCommandOperation<'Result>(databaseNamespace,
                                                          command,
                                                          resultSerializer,
                                                          messageEncoderSettings)

            use binding = new ReadPreferenceBinding(client.Cluster, commandReadPreference)
            operation
            |> operationExecutor.AsyncExecuteReadOperation binding token
