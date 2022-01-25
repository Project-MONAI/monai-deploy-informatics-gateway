// Copyright 2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

/*
 * Apache License, Version 2.0
 * Copyright 2019-2021 NVIDIA Corporation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Monai.Deploy.InformaticsGateway.Common
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Extension method for checking a IEnumerable collection is null or empty.
        /// </summary>
        /// <returns>true if null or empty; false otherwise.</returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable is null)
            {
                return true;
            }

            if (enumerable is ICollection<T> collection)
            {
                return collection.Count == 0;
            }

            return !enumerable.Any();
        }

        /// <summary>
        /// Removes characters that cannot be used in file paths.
        /// </summary>
        /// <param name="input">string to be scanned</param>
        /// <returns><c>input</c> with invalid path characters removed.</returns>
        public static string RemoveInvalidPathChars(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            foreach (var c in System.IO.Path.GetInvalidPathChars())
            {
                input = input.Replace(c.ToString(), "");
            }
            return input;
        }

        /// <summary>
        /// Extension for ActionBlock to delay post of an object to be processed.
        /// </summary>
        /// <typeparam name="TInput">Type of object to be post to the actio. block</typeparam>
        /// <param name="actionBlock">Instance of <c>ActionBlock</c></param>
        /// <param name="input">Object to be posted</param>
        /// <param name="delay">Time to wait before posting</param>
        /// <returns></returns>
        public static async Task<bool> Post<TInput>(this ActionBlock<TInput> actionBlock, TInput input, TimeSpan delay)
        {
            await Task.Delay(delay);
            return actionBlock.Post(input);
        }
    }
}
