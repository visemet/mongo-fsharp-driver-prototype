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

open FSharp.Control

open MongoDB.Driver
open MongoDB.Driver.Core.Operations

open FSharp.MongoDB.Driver.Operations

type OperationExecutorCall<'Result> =
   | ReadOperation of ReadPreference * IReadOperation<'Result>
   | CursorReadOperation of ReadPreference * IReadOperation<IAsyncCursor<'Result>>
   | WriteOperation of IWriteOperation<'Result>

type OperationExecutorSpy() =

    let calls = Queue<obj>()

    member __.PopOldestCall<'Result>() =
        if calls.Count > 0 then
            match calls.Dequeue() with
            | :? OperationExecutorCall<'Result> as call -> call
            | call ->
                let format : Printf.StringFormat<_, _> =
                    "Expected function call on the operation executor to have type %A, but got %A"
                failwithf format typeof<OperationExecutorCall<'Result>> call
        else failwith ("Expected at least one function call on the operation executor, but there \
                        were none")

    interface IDisposable with

        member __.Dispose() =
            if calls.Count > 0 then
                failwithf "Unchecked function calls on the operation executor: %A" calls

    interface IOperationExecutor with

        member __.AsyncExecuteReadOperation<'Result> binding _ operation =
            calls.Enqueue (ReadOperation (binding.ReadPreference, operation))
            async {
                return Unchecked.defaultof<'Result>
            }

        member __.AsyncExecuteCursorReadOperation<'Result> binding _ operation =
            calls.Enqueue (CursorReadOperation (binding.ReadPreference, operation))
            asyncSeq {
                yield Unchecked.defaultof<'Result>
            }

        member __.AsyncExecuteWriteOperation<'Result> _ _ operation =
            calls.Enqueue (WriteOperation operation)
            async {
                return Unchecked.defaultof<'Result>
            }
