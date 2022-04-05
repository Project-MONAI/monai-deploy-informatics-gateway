// SPDX-FileCopyrightText: © 2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

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

        public static async Task<T> WaitUntilDataIsReady<T>(Func<Task<T>> action, Func<T, bool> condition, TimeSpan timeout, int frequency = 250)
        {
            var waitTask = Task.Run(async () =>
            {
                T result;
                do
                {
                    result = await action();
                } while (!condition(result));
                return result;
            });

            if (waitTask == await Task.WhenAny(waitTask, Task.Delay(timeout)))
            {
                return waitTask.Result;
            }
            else
            {
                return default(T);
            }
        }
    }
}
