/*
 * Copyright 2022 MONAI Consortium
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
 */

using System;

namespace Monai.Deploy.InformaticsGateway.Api
{
    public abstract class MongoDBEntityBase
    {
        /// <summary>
        /// Gets or set the MongoDB associated identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or set the date and time the objects first created.
        /// </summary>
        public DateTime DateTimeCreated { get; set; }

        protected MongoDBEntityBase()
        {
            Id = Guid.NewGuid();
            DateTimeCreated = DateTime.UtcNow;
        }
    }
}
