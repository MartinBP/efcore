// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.SqlServer.Internal
{
    /// <summary>
    ///     <para>
    ///         This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///         the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///         any release. You should only use it directly in your code with extreme caution and knowing that
    ///         doing so can result in application failures when updating to a new Entity Framework Core release.
    ///     </para>
    ///     <para>
    ///         The service lifetime is <see cref="ServiceLifetime.Singleton" />. This means a single instance
    ///         is used by many <see cref="DbContext" /> instances. The implementation must be thread-safe.
    ///         This service cannot depend on services registered as <see cref="ServiceLifetime.Scoped" />.
    ///     </para>
    /// </summary>
    public class SqlServerModelValidator : RelationalModelValidator
    {
        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public SqlServerModelValidator(
            [NotNull] ModelValidatorDependencies dependencies,
            [NotNull] RelationalModelValidatorDependencies relationalDependencies)
            : base(dependencies, relationalDependencies)
        {
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public override void Validate(IModel model, IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            ValidateIndexIncludeProperties(model, logger);

            base.Validate(model, logger);

            ValidateDefaultDecimalMapping(model, logger);
            ValidateByteIdentityMapping(model, logger);
            ValidateNonKeyValueGeneration(model, logger);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected virtual void ValidateDefaultDecimalMapping(
            [NotNull] IModel model, [NotNull] IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            foreach (var property in model.GetEntityTypes()
                .SelectMany(t => t.GetDeclaredProperties())
                .Where(
                    p => p.ClrType.UnwrapNullableType() == typeof(decimal)
                        && !p.IsForeignKey()))
            {
                var valueConverterConfigurationSource = (property as IConventionProperty)?.GetValueConverterConfigurationSource();
                var valueConverterProviderType = property.GetValueConverter()?.ProviderClrType;
                if (!ConfigurationSource.Convention.Overrides(valueConverterConfigurationSource)
                    && typeof(decimal) != valueConverterProviderType)
                {
                    continue;
                }

                var columnTypeConfigurationSource = (property as IConventionProperty)?.GetColumnTypeConfigurationSource();
                var typeMappingConfigurationSource = (property as IConventionProperty)?.GetTypeMappingConfigurationSource();
                if ((columnTypeConfigurationSource == null
                        && ConfigurationSource.Convention.Overrides(typeMappingConfigurationSource))
                    || (columnTypeConfigurationSource != null
                        && ConfigurationSource.Convention.Overrides(columnTypeConfigurationSource)))
                {
                    logger.DecimalTypeDefaultWarning(property);
                }
            }
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected virtual void ValidateByteIdentityMapping(
            [NotNull] IModel model, [NotNull] IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            foreach (var entityType in model.GetEntityTypes())
            {
                foreach (var property in entityType.GetDeclaredProperties()
                    .Where(p => p.ClrType.UnwrapNullableType() == typeof(byte)
                            && p.GetValueGenerationStrategy() == SqlServerValueGenerationStrategy.IdentityColumn))
                {
                    logger.ByteIdentityColumnWarning(property);
                }
            }
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected virtual void ValidateNonKeyValueGeneration(
            [NotNull] IModel model, [NotNull] IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            foreach (var entityType in model.GetEntityTypes())
            {
                foreach (var property in entityType.GetDeclaredProperties()
                    .Where(p => p.GetValueGenerationStrategy() == SqlServerValueGenerationStrategy.SequenceHiLo
                            && ((IConventionProperty)p).GetValueGenerationStrategyConfigurationSource() != null
                            && !p.IsKey()
                            && p.ValueGenerated != ValueGenerated.Never
                            && (!(p.FindAnnotation(SqlServerAnnotationNames.ValueGenerationStrategy) is IConventionAnnotation strategy)
                                || !ConfigurationSource.Convention.Overrides(strategy.GetConfigurationSource()))))
                {
                    throw new InvalidOperationException(
                        SqlServerStrings.NonKeyValueGeneration(property.Name, property.DeclaringEntityType.DisplayName()));
                }
            }
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected virtual void ValidateIndexIncludeProperties(
            [NotNull] IModel model, [NotNull] IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            foreach (var index in model.GetEntityTypes().SelectMany(t => t.GetDeclaredIndexes()))
            {
                var includeProperties = index.GetIncludeProperties();
                if (includeProperties?.Count > 0)
                {
                    var notFound = includeProperties
                        .FirstOrDefault(i => index.DeclaringEntityType.FindProperty(i) == null);

                    if (notFound != null)
                    {
                        throw new InvalidOperationException(
                            SqlServerStrings.IncludePropertyNotFound(index.DeclaringEntityType.DisplayName(), notFound));
                    }

                    var duplicate = includeProperties
                        .GroupBy(i => i)
                        .Where(g => g.Count() > 1)
                        .Select(y => y.Key)
                        .FirstOrDefault();

                    if (duplicate != null)
                    {
                        throw new InvalidOperationException(
                            SqlServerStrings.IncludePropertyDuplicated(index.DeclaringEntityType.DisplayName(), duplicate));
                    }

                    var inIndex = includeProperties
                        .FirstOrDefault(i => index.Properties.Any(p => i == p.Name));

                    if (inIndex != null)
                    {
                        throw new InvalidOperationException(
                            SqlServerStrings.IncludePropertyInIndex(index.DeclaringEntityType.DisplayName(), inIndex));
                    }
                }
            }
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override void ValidateSharedTableCompatibility(
            IReadOnlyList<IEntityType> mappedTypes,
            string tableName,
            string schema,
            IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            var firstMappedType = mappedTypes[0];
            var isMemoryOptimized = firstMappedType.IsMemoryOptimized();

            foreach (var otherMappedType in mappedTypes.Skip(1))
            {
                if (isMemoryOptimized != otherMappedType.IsMemoryOptimized())
                {
                    throw new InvalidOperationException(
                        SqlServerStrings.IncompatibleTableMemoryOptimizedMismatch(
                            tableName, firstMappedType.DisplayName(), otherMappedType.DisplayName(),
                            isMemoryOptimized ? firstMappedType.DisplayName() : otherMappedType.DisplayName(),
                            !isMemoryOptimized ? firstMappedType.DisplayName() : otherMappedType.DisplayName()));
                }
            }

            base.ValidateSharedTableCompatibility(mappedTypes, tableName, schema, logger);
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        protected override void ValidateSharedColumnsCompatibility(
            IReadOnlyList<IEntityType> mappedTypes,
            string tableName,
            string schema,
            IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            base.ValidateSharedColumnsCompatibility(mappedTypes, tableName, schema, logger);

            var identityColumns = new Dictionary<string, IProperty>();

            foreach (var property in mappedTypes.SelectMany(et => et.GetDeclaredProperties()))
            {
                if (property.GetValueGenerationStrategy(tableName, schema) == SqlServerValueGenerationStrategy.IdentityColumn)
                {
                    var columnName = property.GetColumnName(tableName, schema);
                    if (columnName == null)
                    {
                        continue;
                    }

                    identityColumns[columnName] = property;
                }
            }

            if (identityColumns.Count > 1)
            {
                var sb = new StringBuilder()
                    .AppendJoin(identityColumns.Values.Select(p => "'" + p.DeclaringEntityType.DisplayName() + "." + p.Name + "'"));
                throw new InvalidOperationException(SqlServerStrings.MultipleIdentityColumns(sb, tableName));
            }
        }

        /// <inheritdoc />
        protected override void ValidateCompatible(
            IProperty property,
            IProperty duplicateProperty,
            string columnName,
            string tableName,
            string schema,
            IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            base.ValidateCompatible(property, duplicateProperty, columnName, tableName, schema, logger);

            var propertyStrategy = property.GetValueGenerationStrategy(tableName, schema);
            var duplicatePropertyStrategy = duplicateProperty.GetValueGenerationStrategy(tableName, schema);
            if (propertyStrategy != duplicatePropertyStrategy)
            {
                throw new InvalidOperationException(
                    SqlServerStrings.DuplicateColumnNameValueGenerationStrategyMismatch(
                        duplicateProperty.DeclaringEntityType.DisplayName(),
                        duplicateProperty.Name,
                        property.DeclaringEntityType.DisplayName(),
                        property.Name,
                        columnName,
                        tableName));
            }

            switch (propertyStrategy)
            {
                case SqlServerValueGenerationStrategy.IdentityColumn:
                    var increment = property.GetIdentityIncrement();
                    var duplicateIncrement = duplicateProperty.GetIdentityIncrement();
                    if (increment != duplicateIncrement)
                    {
                        throw new InvalidOperationException(
                            SqlServerStrings.DuplicateColumnIdentityIncrementMismatch(
                                duplicateProperty.DeclaringEntityType.DisplayName(),
                                duplicateProperty.Name,
                                property.DeclaringEntityType.DisplayName(),
                                property.Name,
                                columnName,
                                tableName));
                    }

                    var seed = property.GetIdentitySeed();
                    var duplicateSeed = duplicateProperty.GetIdentitySeed();
                    if (seed != duplicateSeed)
                    {
                        throw new InvalidOperationException(
                            SqlServerStrings.DuplicateColumnIdentitySeedMismatch(
                                duplicateProperty.DeclaringEntityType.DisplayName(),
                                duplicateProperty.Name,
                                property.DeclaringEntityType.DisplayName(),
                                property.Name,
                                columnName,
                                tableName));
                    }

                    break;
                case SqlServerValueGenerationStrategy.SequenceHiLo:
                    if (property.GetHiLoSequenceName() != duplicateProperty.GetHiLoSequenceName()
                        || property.GetHiLoSequenceSchema() != duplicateProperty.GetHiLoSequenceSchema())
                    {
                        throw new InvalidOperationException(
                            SqlServerStrings.DuplicateColumnSequenceMismatch(
                                duplicateProperty.DeclaringEntityType.DisplayName(),
                                duplicateProperty.Name,
                                property.DeclaringEntityType.DisplayName(),
                                property.Name,
                                columnName,
                                tableName));
                    }

                    break;
            }
        }

        /// <inheritdoc />
        protected override void ValidateCompatible(
            IKey key,
            IKey duplicateKey,
            string keyName,
            string tableName,
            string schema,
            IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            base.ValidateCompatible(key, duplicateKey, keyName, tableName, schema, logger);

            key.AreCompatibleForSqlServer(duplicateKey, tableName, schema, shouldThrow: true);
        }

        /// <inheritdoc />
        protected override void ValidateCompatible(
            IIndex index,
            IIndex duplicateIndex,
            string indexName,
            string tableName,
            string schema,
            IDiagnosticsLogger<DbLoggerCategory.Model.Validation> logger)
        {
            base.ValidateCompatible(index, duplicateIndex, indexName, tableName, schema, logger);

            index.AreCompatibleForSqlServer(duplicateIndex, tableName, schema, shouldThrow: true);
        }
    }
}
