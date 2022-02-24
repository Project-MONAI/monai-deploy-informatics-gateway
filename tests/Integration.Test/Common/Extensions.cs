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

namespace Monai.Deploy.InformaticsGateway.Integration.Test.Drivers
{
    public static class Extensions
    {
        public static long NextLong(this Random random, long minValue, long maxValue)
        {
            byte[] buf = new byte[8];
            random.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);

            return (Math.Abs(longRand % (maxValue - minValue)) + minValue);
        }

        public static async Task<bool> WaitUntil(Func<bool> condition, TimeSpan timeout, int frequency = 250)
        {
            var waitTask = Task.Run(async () =>
            {
                while (!condition())
                {
                    await Task.Delay(frequency);
                }
            });

            return waitTask == await Task.WhenAny(waitTask, Task.Delay(timeout));
        }
    }
}
