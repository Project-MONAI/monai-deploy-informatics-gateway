// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using Ardalis.GuardClauses;

namespace Monai.Deploy.InformaticsGateway.CLI.Services
{
    public interface IConfirmationPrompt
    {
        bool ShowConfirmationPrompt(string message);
    }

    internal class ConfirmationPrompt : IConfirmationPrompt
    {
        public bool ShowConfirmationPrompt(string message)
        {
            Guard.Against.NullOrWhiteSpace(message, nameof(message));

            Console.Write($"{message} [y/N]: ");
            var key = Console.ReadKey();
            Console.WriteLine();
            if (key.Key == ConsoleKey.Y)
            {
                return true;
            }
            return false;
        }
    }
}
