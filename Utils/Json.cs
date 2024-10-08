/*
 *      This file is part of SlashBot distribution (https://github.com/sysvdev/slashbot).
 *      Copyright (c) 2024 contributors
 *
 *      SlashBot is free software: you can redistribute it and/or modify
 *      it under the terms of the GNU General Public License as published by
 *      the Free Software Foundation, either version 3 of the License, or
 *      (at your option) any later version.
 *
 *      SlashBot is distributed in the hope that it will be useful,
 *      but WITHOUT ANY WARRANTY; without even the implied warranty of
 *      MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *      GNU General Public License for more details.
 *
 *      You should have received a copy of the GNU General Public License
 *      along with SlashBot.  If not, see <https://www.gnu.org/licenses/>.
 */

namespace SlashBot.Utils;

public class Json
{
    /// <summary>
    /// Given the JSON string, validates if it's a correct
    /// JSON string.
    /// </summary>
    /// <param name="json_string">JSON string to validate.</param>
    /// <returns>true or false.</returns>
    internal static bool IsValid(string json_string)
    {
        try
        {
            _ = JsonNode.Parse(json_string);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}