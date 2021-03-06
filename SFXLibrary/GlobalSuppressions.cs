#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 GlobalSuppressions.cs is part of SFXLibrary.

 SFXLibrary is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXLibrary is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXLibrary. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the
// Code Analysis results, point to "Suppress Message", and click
// "In Suppression File".
// You do not need to add suppressions to this file manually.

using System.Diagnostics.CodeAnalysis;

#endregion

[assembly:
    SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly", Scope = "type",
        Target = "SFXLibrary.WeakAction")]
[assembly:
    SuppressMessage("Microsoft.Usage", "CA2229:ImplementSerializationConstructors", Scope = "type",
        Target = "SFXLibrary.WeakAction")]
[assembly:
    SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Scope = "member",
        Target = "SFXLibrary.Extensions.StringExtensions.#XmlDeserialize`1(System.String)")]
[assembly:
    SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Scope = "member",
        Target = "SFXLibrary.Extensions.StringExtensions.#XmlSerialize`1(!!0)")]
[assembly:
    SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Scope = "member",
        Target = "SFXLibrary.Extensions.NET.StringExtensions.#XmlDeserialize`1(System.String)")]
[assembly:
    SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Scope = "member",
        Target = "SFXLibrary.Extensions.NET.StringExtensions.#XmlSerialize`1(!!0)")]