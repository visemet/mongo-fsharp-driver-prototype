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

namespace FSharp.MongoDB.Driver

open System
open System.Net
open System.Net.Security
open System.Text
open System.Security.Authentication
open System.Security.Cryptography.X509Certificates

open MongoDB.Bson

open MongoDB.Driver
open MongoDB.Driver.Core.Authentication
open MongoDB.Driver.Core.Clusters
open MongoDB.Driver.Core.Configuration

type SslSettings =
    { CheckCertificateRevocation : bool option
      ClientCertificateCollection : X509Certificate list option
      ClientCertificateSelectionCallback : LocalCertificateSelectionCallback option
      EnabledSslProtocols : SslProtocols option
      ServerCertificateValidationCallback : RemoteCertificateValidationCallback option } with

    static member None : SslSettings

type MongoClientSettings =
    { ClusterConfigurator : (ClusterBuilder -> unit) option
      ConnectionMode : ClusterConnectionMode option
      ConnectionTimeout : TimeSpan option
      Credentials : IAuthenticator list option
      GuidRepresentation : GuidRepresentation option
      IPv6 : bool option
      LocalThreshold : TimeSpan option
      MaxConnectionIdleTime : TimeSpan option
      MaxConnectionLifeTime : TimeSpan option
      MaxConnectionPoolSize : int option
      MinConnectionPoolSize : int option
      ReadEncoding : UTF8Encoding option
      ReadPreference : ReadPreference option
      ReplicaSetName : string option
      Servers : EndPoint list option
      SocketTimeout : TimeSpan option
      SslSettings : SslSettings option
      UseSsl : bool option
      VerifySslCertificate : bool option
      WaitQueueSize : int option
      WaitQueueTimeout : TimeSpan option
      WriteConcern : WriteConcern option
      WriteEncoding : UTF8Encoding option } with

    static member None : MongoClientSettings

type MongoDatabaseSettings =
    { GuidRepresentation : GuidRepresentation option
      ReadEncoding : UTF8Encoding option
      ReadPreference : ReadPreference option
      WriteConcern : WriteConcern option
      WriteEncoding : UTF8Encoding option } with

    static member None : MongoDatabaseSettings

type MongoCollectionSettings =
    { AssignIdOnInsert : bool option
      GuidRepresentation : GuidRepresentation option
      ReadEncoding : UTF8Encoding option
      ReadPreference : ReadPreference option
      WriteConcern : WriteConcern option
      WriteEncoding : UTF8Encoding option } with

    static member None : MongoCollectionSettings
