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

namespace FSharp.MongoDB.Driver.Operations

open MongoDB.Bson

module DatabaseWriteOptions =
    type CreateCollectionOptions =
        { AutoIndexId : bool option
          Capped : bool option
          MaxDocuments : int64 option
          MaxSize : int64 option
          StorageEngine : BsonDocument option
          UsePowerOf2Sizes : bool option } with

        static member None =
            { AutoIndexId = None
              Capped = None
              MaxDocuments = None
              MaxSize = None
              StorageEngine = None
              UsePowerOf2Sizes = None }

    type RenameCollectionOptions =
        { DropTarget : bool option } with

        static member None =
            { DropTarget = None }
