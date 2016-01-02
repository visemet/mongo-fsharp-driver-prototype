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

open System

open MongoDB.Bson
open MongoDB.Driver.Core.Operations

module CollectionReadOptions =
    type AggregateOptions =
        { AllowDiskUse : bool option
          BatchSize : int option
          MaxTime : TimeSpan option
          UseCursor : bool option } with

        static member None : AggregateOptions

    type Hint =
       | IndexName of string
       | IndexSpec of BsonDocument

    type CountOptions =
        { Hint : Hint option
          Limit : int64 option
          MaxTime : TimeSpan option
          Skip : int64 option } with

        static member None : CountOptions

    type DistinctOptions =
        { MaxTime : TimeSpan option } with

        static member None : DistinctOptions

    type FindOptions =
        { AllowPartialResults : bool option
          BatchSize : int option
          Comment : string option
          CursorType : CursorType option
          Limit : int option
          MaxTime : TimeSpan option
          Modifiers : BsonDocument option
          NoCursorTimeout : bool option
          Projection : BsonDocument option
          Skip : int option
          Sort : BsonDocument option } with

        static member None : FindOptions
