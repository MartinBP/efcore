// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using JetBrains.Annotations;

namespace Microsoft.EntityFrameworkCore.Metadata
{
    /// <summary>
    ///     Represents a table-valued function in the database.
    /// </summary>
    public interface ITableValuedFunction : IDbFunction, ITableBase
    {
        /// <summary>
        ///     Gets the entity type mappings.
        /// </summary>
        new IEnumerable<IFunctionMapping> EntityTypeMappings { get; }

        /// <summary>
        ///     Gets the columns defined for this function.
        /// </summary>
        new IEnumerable<IFunctionColumn> Columns { get; }

        /// <summary>
        ///     Gets the column with the given name. Returns <see langword="null" /> if no column with the given name is defined.
        /// </summary>
        new IFunctionColumn FindColumn([NotNull] string name);
    }
}
