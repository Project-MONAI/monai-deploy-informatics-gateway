// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-FileCopyrightText: © 2019-2020 NVIDIA Corporation
// SPDX-License-Identifier: Apache License 2.0

using System.Collections.Generic;

namespace Monai.Deploy.InformaticsGateway.DicomWeb.Client.Common
{
    public static class Extensions
    {
        /// <summary>
        /// Trim() removes null values from the input array.
        /// </summary>
        /// <typeparam name="T">Any data type.</typeparam>
        /// <param name="input">Array to be trimmed.</param>
        /// <returns></returns>
        public static T[] Trim<T>(this T[] input)
        {
            if (input is null)
            {
                return input;
            }

            var list = new List<T>();
            foreach (var item in input)
            {
                if (item != null)
                {
                    list.Add(item);
                }
            }
            return list.ToArray();
        }
    }
}
