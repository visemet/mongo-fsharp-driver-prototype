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

open MongoDB.Driver.Core.Clusters

type DummyCluster() =

    interface ICluster with

        member __.add_DescriptionChanged _ = invalidOp "not implemented"

        member __.remove_DescriptionChanged _ = invalidOp "not implemented"

        member __.ClusterId = invalidOp "not implemented"

        member __.Description = invalidOp "not implemented"

        member __.Settings = invalidOp "not implemented"

        member __.Initialize() = invalidOp "not implemented"

        member __.SelectServerAsync(_, _) = invalidOp "not implemented"

        member __.Dispose() = invalidOp "not implemented"
