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

open System.Threading

open FSharp.Control

open MongoDB.Bson
open MongoDB.Driver
open MongoDB.Driver.Core.Clusters

open FSharp.MongoDB.Driver.Operations
open FSharp.MongoDB.Driver.Operations.CollectionReadOptions
open FSharp.MongoDB.Driver.Operations.DatabaseReadOptions
open FSharp.MongoDB.Driver.Operations.DatabaseWriteOptions

[<Interface>]
type IMongoClient =
    abstract member Cluster :
        ICluster

    abstract member GetDatabase :
        name:string *
        ?settings:MongoDatabaseSettings ->
            IMongoDatabase

    abstract member AsyncDropDatabase :
        name:string *
        ?cancellationToken:CancellationToken ->
            Async<unit>

    abstract member AsyncListDatabases :
        ?cancellationToken:CancellationToken ->
            AsyncSeq<BsonDocument>

and IMongoDatabase =
    abstract member Client :
        IMongoClient

    abstract member DatabaseNamespace :
        DatabaseNamespace

    abstract member GetCollection<'Document> :
        name:string *
        ?settings:MongoCollectionSettings ->
            IMongoCollection<'Document>

    abstract member AsyncCreateCollection :
        name:string *
        ?options:CreateCollectionOptions *
        ?cancellationToken:CancellationToken ->
            Async<unit>

    abstract member AsyncDropCollection :
        name:string *
        ?cancellationToken:CancellationToken ->
            Async<unit>

    abstract member AsyncListCollections :
        ?options:ListCollectionsOptions *
        ?cancellationToken:CancellationToken ->
            AsyncSeq<BsonDocument>

    abstract member AsyncRenameCollection :
        oldName:string *
        newName:string *
        ?options:RenameCollectionOptions *
        ?cancellationToken:CancellationToken ->
            Async<unit>

    abstract member AsyncRunCommand<'Result> :
        command:BsonDocument *
        ?readPreference:ReadPreference *
        ?cancellationToken:CancellationToken ->
            Async<'Result>

and IMongoCollection<'Document> =
    abstract member Database :
        IMongoDatabase

    abstract member CollectionNamespace :
        CollectionNamespace

    abstract member WithReadPreference :
        readPreference:ReadPreference ->
            IMongoCollection<'Document>

    abstract member AsyncAggregate<'Result> :
        pipeline:seq<BsonDocument> *
        ?options:AggregateOptions *
        ?cancellationToken:CancellationToken ->
            AsyncSeq<'Result>

    abstract member AsyncCount :
        filter:BsonDocument *
        ?options:CountOptions *
        ?cancellationToken:CancellationToken ->
            Async<int64>

    abstract member AsyncDistinct<'Result> :
        fieldName:string *
        filter:BsonDocument *
        ?options:DistinctOptions *
        ?cancellationToken:CancellationToken ->
            AsyncSeq<'Result>

    abstract member AsyncFind :
        filter:BsonDocument *
        ?options:FindOptions *
        ?cancellationToken:CancellationToken ->
            AsyncSeq<'Document>

    abstract member AsyncFind<'Projection> :
        filter:BsonDocument *
        ?options:FindOptions *
        ?cancellationToken:CancellationToken ->
            AsyncSeq<'Projection>
