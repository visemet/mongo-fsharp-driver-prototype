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

open System.Threading

open FSharp.Control

open MongoDB.Driver
open MongoDB.Driver.Core.Bindings
open MongoDB.Driver.Core.Operations

[<Interface>]
type internal IOperationExecutor =

    abstract member AsyncExecuteReadOperation<'Result> :
        binding:IReadBinding ->
        cancellationToken:CancellationToken ->
        operation:IReadOperation<'Result> ->
            Async<'Result>

    abstract member AsyncExecuteCursorReadOperation<'Result> :
        binding:IReadBinding ->
        cancellationToken:CancellationToken ->
        operation:IReadOperation<IAsyncCursor<'Result>> ->
            AsyncSeq<'Result>

    abstract member AsyncExecuteWriteOperation<'Result> :
        binding:IWriteBinding ->
        cancellationToken:CancellationToken ->
        operation:IWriteOperation<'Result> ->
            Async<'Result>
