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
open System.Net.Security
open System.Net.Sockets
open System.Text

open MongoDB.Bson
open MongoDB.Bson.IO

open MongoDB.Driver
open MongoDB.Driver.Core.Bindings
open MongoDB.Driver.Core.Clusters
open MongoDB.Driver.Core.Clusters.ServerSelectors
open MongoDB.Driver.Core.Configuration
open MongoDB.Driver.Core.Operations
open MongoDB.Driver.Core.WireProtocol.Messages.Encoders

open FSharp.MongoDB.Driver.Operations

type internal NonOptionalMongoClientSettings =
    { GuidRepresentation : GuidRepresentation
      ReadEncoding : UTF8Encoding
      ReadPreference : ReadPreference
      WriteConcern : WriteConcern
      WriteEncoding : UTF8Encoding }

type internal MongoClient(cluster:ICluster,
                          settings:MongoClientSettings,
                          operationExecutor:IOperationExecutor) =

    let clientSettings : NonOptionalMongoClientSettings =
        { GuidRepresentation = (defaultArg settings.GuidRepresentation
                                           BsonDefaults.GuidRepresentation)
          ReadEncoding = defaultArg settings.ReadEncoding Utf8Encodings.Strict
          ReadPreference = defaultArg settings.ReadPreference ReadPreference.Primary
          WriteConcern = defaultArg settings.WriteConcern WriteConcern.Acknowledged
          WriteEncoding = defaultArg settings.WriteEncoding Utf8Encodings.Strict }

    let messageEncoderSettings =
        let add name value (messageEncoderSettings:MessageEncoderSettings) =
            messageEncoderSettings.Add(name, value)

        MessageEncoderSettings()
        |> add MessageEncoderSettingsName.GuidRepresentation clientSettings.GuidRepresentation
        |> add MessageEncoderSettingsName.ReadEncoding clientSettings.ReadEncoding
        |> add MessageEncoderSettingsName.WriteEncoding clientSettings.WriteEncoding

    interface IMongoClient with

        member __.Cluster = cluster

        member client.GetDatabase (name, ?settings) =
            let databaseOverrides = defaultArg settings MongoDatabaseSettings.None
            let databaseSettings : NonOptionalMongoDatabaseSettings =
                { GuidRepresentation = (defaultArg databaseOverrides.GuidRepresentation
                                                   clientSettings.GuidRepresentation)
                  ReadEncoding = (defaultArg databaseOverrides.ReadEncoding
                                             clientSettings.ReadEncoding)
                  ReadPreference = (defaultArg databaseOverrides.ReadPreference
                                               clientSettings.ReadPreference)
                  WriteConcern = (defaultArg databaseOverrides.WriteConcern
                                             clientSettings.WriteConcern)
                  WriteEncoding = (defaultArg databaseOverrides.WriteEncoding
                                              clientSettings.WriteEncoding) }

            let databaseNamespace = DatabaseNamespace name
            MongoDatabase(client, databaseNamespace, databaseSettings, operationExecutor)
            :> IMongoDatabase

        member __.AsyncDropDatabase (name, ?cancellationToken) =
            let token = defaultArg cancellationToken Async.DefaultCancellationToken

            let databaseNamespace = DatabaseNamespace name
            let operation = DropDatabaseOperation(databaseNamespace, messageEncoderSettings)

            use binding = new WritableServerBinding(cluster)
            operation
            |> operationExecutor.AsyncExecuteWriteOperation binding token
            |> Async.Ignore

        member __.AsyncListDatabases (?cancellationToken) =
            let token = defaultArg cancellationToken Async.DefaultCancellationToken

            let operation = ListDatabasesOperation(messageEncoderSettings)

            use binding = new ReadPreferenceBinding(cluster, clientSettings.ReadPreference)
            operation
            |> operationExecutor.AsyncExecuteCursorReadOperation binding token

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module MongoClient =

    [<CompiledName("Create")>]
    let create (settings:MongoClientSettings) =
        let cluster =
            let toOptional = function
            | Some x -> Optional x
            | None -> Optional()

            let builder = ClusterBuilder()

            builder.ConfigureCluster (fun clusterSettings ->
                let connectionMode = settings.ConnectionMode |> toOptional
                let endPoints =
                    settings.Servers
                    |> Option.map List.toSeq
                    |> toOptional
                let replicaSetName = settings.ReplicaSetName |> toOptional
                let maxServerSelectionWaitQueueSize = settings.WaitQueueSize |> toOptional
                let postServerSelector =
                    settings.LocalThreshold
                    |> Option.map (fun x -> LatencyLimitingServerSelector x :> IServerSelector)
                    |> toOptional

                clusterSettings.With(
                    connectionMode = connectionMode,
                    endPoints = endPoints,
                    replicaSetName = replicaSetName,
                    maxServerSelectionWaitQueueSize = maxServerSelectionWaitQueueSize,
                    postServerSelector = postServerSelector))
            |> ignore

            builder.ConfigureConnectionPool (fun connectionPoolSettings ->
                let maxConnections = settings.MaxConnectionPoolSize |> toOptional
                let minConnections = settings.MinConnectionPoolSize |> toOptional
                let waitQueueSize = settings.WaitQueueSize |> toOptional
                let waitQueueTimeout = settings.WaitQueueTimeout |> toOptional

                connectionPoolSettings.With(
                    maxConnections = maxConnections,
                    minConnections = minConnections,
                    waitQueueSize = waitQueueSize,
                    waitQueueTimeout = waitQueueTimeout))
            |> ignore

            builder.ConfigureConnection (fun connectionSettings ->
                let authenticators =
                    settings.Credentials
                    |> Option.map List.toSeq
                    |> toOptional
                let maxIdleTime = settings.MaxConnectionIdleTime |> toOptional
                let maxLifeTime = settings.MaxConnectionLifeTime |> toOptional

                connectionSettings.With(
                    authenticators = authenticators,
                    maxIdleTime = maxIdleTime,
                    maxLifeTime = maxLifeTime))
            |> ignore

            builder.ConfigureTcp (fun tcpStreamSettings ->
                let addressFamily =
                    match defaultArg settings.IPv6 false with
                    | true -> Optional AddressFamily.InterNetworkV6
                    | false -> Optional()
                let connectTimeout = settings.ConnectionTimeout |> toOptional
                let readTimeout =
                    settings.SocketTimeout
                    |> Option.map (fun x -> Nullable x)
                    |> toOptional
                let writeTimeout =
                    settings.SocketTimeout
                    |> Option.map (fun x -> Nullable x)
                    |> toOptional

                tcpStreamSettings.With(
                    addressFamily = addressFamily,
                    connectTimeout = connectTimeout,
                    readTimeout = readTimeout,
                    writeTimeout = writeTimeout))
            |> ignore

            let useSsl = defaultArg settings.UseSsl false
            match (useSsl, settings.SslSettings) with
            | (true, Some sslSettings) ->
                builder.ConfigureSsl (fun sslStreamSettings ->
                    let clientCertificates =
                        sslSettings.ClientCertificateCollection
                        |> Option.map List.toSeq
                        |> toOptional
                    let checkCertificateRevocation =
                        sslSettings.CheckCertificateRevocation
                        |> toOptional
                    let clientCertificateSelectionCallback =
                        sslSettings.ClientCertificateSelectionCallback
                        |> toOptional
                    let enabledProtocols = sslSettings.EnabledSslProtocols |> toOptional
                    let serverCertificateValidationCallback =
                        let verifySslCertificate = defaultArg settings.VerifySslCertificate true
                        let validationCallback = sslSettings.ServerCertificateValidationCallback
                        match (verifySslCertificate, validationCallback) with
                        | (false, None) ->
                            Optional (RemoteCertificateValidationCallback (fun _ _ _ _ -> true))
                        | _ -> sslSettings.ServerCertificateValidationCallback |> toOptional

                    sslStreamSettings.With(
                        clientCertificates = clientCertificates,
                        checkCertificateRevocation = checkCertificateRevocation,
                        clientCertificateSelectionCallback = clientCertificateSelectionCallback,
                        enabledProtocols = enabledProtocols,
                        serverCertificateValidationCallback = serverCertificateValidationCallback))
                |> ignore
            | _ -> ()

            settings.ClusterConfigurator
            |> Option.iter (fun clusterConfigurator -> clusterConfigurator builder)

            let cluster = builder.BuildCluster()
            cluster.Initialize()
            cluster

        let operationExecutor = OperationExecutor()
        MongoClient(cluster, settings, operationExecutor) :> IMongoClient
