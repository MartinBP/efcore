// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Relational;
using Microsoft.Data.Entity.Services;
using Xunit;

namespace Microsoft.Data.Entity.SqlServer.Tests
{
    public class SqlServerSequenceValueGeneratorFactoryTest
    {
        [Fact]
        public void Block_size_is_obtained_from_property_annotation()
        {
            var property = CreateProperty();
            property[Entity.Metadata.SqlServerMetadataExtensions.Annotations.SequenceBlockSize] = "11";
            property.EntityType[Entity.Metadata.SqlServerMetadataExtensions.Annotations.SequenceBlockSize] = "-1";
            property.EntityType.Model[Entity.Metadata.SqlServerMetadataExtensions.Annotations.SequenceBlockSize] = "-1";

            var factory = new SqlServerSequenceValueGeneratorFactory(new SqlStatementExecutor(new NullLoggerFactory()));

            Assert.Equal(11, factory.GetBlockSize(property));
        }

        [Fact]
        public void Block_size_is_obtained_from_entity_type_annotation_if_not_set_on_property()
        {
            var property = CreateProperty();
            property.EntityType[Entity.Metadata.SqlServerMetadataExtensions.Annotations.SequenceBlockSize] = "11";
            property.EntityType.Model[Entity.Metadata.SqlServerMetadataExtensions.Annotations.SequenceBlockSize] = "-1";

            var factory = new SqlServerSequenceValueGeneratorFactory(new SqlStatementExecutor(new NullLoggerFactory()));

            Assert.Equal(11, factory.GetBlockSize(property));
        }

        [Fact]
        public void Block_size_is_obtained_from_model_annotation_if_not_set_on_property_or_type()
        {
            var property = CreateProperty();
            property.EntityType.Model[Entity.Metadata.SqlServerMetadataExtensions.Annotations.SequenceBlockSize] = "11";

            var factory = new SqlServerSequenceValueGeneratorFactory(new SqlStatementExecutor(new NullLoggerFactory()));

            Assert.Equal(11, factory.GetBlockSize(property));
        }

        [Fact]
        public void Default_block_size_is_used_if_not_set_anywhere()
        {
            var property = CreateProperty();

            var factory = new SqlServerSequenceValueGeneratorFactory(new SqlStatementExecutor(new NullLoggerFactory()));

            Assert.Equal(Entity.Metadata.SqlServerMetadataExtensions.DefaultSequenceBlockSize, factory.GetBlockSize(property));
        }

        [Fact]
        public void Sequence_name_is_obtained_from_property_annotation()
        {
            var property = CreateProperty();
            property[Entity.Metadata.SqlServerMetadataExtensions.Annotations.SequenceName] = "Robert";
            property.EntityType[Entity.Metadata.SqlServerMetadataExtensions.Annotations.SequenceName] = "Jimmy";
            property.EntityType.Model[Entity.Metadata.SqlServerMetadataExtensions.Annotations.SequenceName] = "Jimmy";

            var factory = new SqlServerSequenceValueGeneratorFactory(new SqlStatementExecutor(new NullLoggerFactory()));

            Assert.Equal("Robert", factory.GetSequenceName(property));
        }

        [Fact]
        public void Sequence_name_is_obtained_from_entity_type_annotation_if_not_set_on_property()
        {
            var property = CreateProperty();
            property.EntityType[Entity.Metadata.SqlServerMetadataExtensions.Annotations.SequenceName] = "Robert";
            property.EntityType.Model[Entity.Metadata.SqlServerMetadataExtensions.Annotations.SequenceName] = "Jimmy";

            var factory = new SqlServerSequenceValueGeneratorFactory(new SqlStatementExecutor(new NullLoggerFactory()));

            Assert.Equal("Robert", factory.GetSequenceName(property));
        }

        [Fact]
        public void Sequence_name_is_obtained_from_model_annotation_if_not_set_on_property_or_type()
        {
            var property = CreateProperty();
            property.EntityType.Model[Entity.Metadata.SqlServerMetadataExtensions.Annotations.SequenceName] = "Robert";

            var factory = new SqlServerSequenceValueGeneratorFactory(new SqlStatementExecutor(new NullLoggerFactory()));

            Assert.Equal("Robert", factory.GetSequenceName(property));
        }

        [Fact]
        public void Default_Sequence_name_is_used_if_not_set_anywhere()
        {
            var property = CreateProperty();

            var factory = new SqlServerSequenceValueGeneratorFactory(new SqlStatementExecutor(new NullLoggerFactory()));

            Assert.Equal("MyTable_Sequence", factory.GetSequenceName(property));
        }

        [Fact]
        public void Creates_the_appropriate_value_generator()
        {
            var property = CreateProperty();
            property[Entity.Metadata.SqlServerMetadataExtensions.Annotations.SequenceBlockSize] = "11";
            property[Entity.Metadata.SqlServerMetadataExtensions.Annotations.SequenceName] = "Zeppelin";

            var factory = new SqlServerSequenceValueGeneratorFactory(new SqlStatementExecutor(new NullLoggerFactory()));

            var generator = (SqlServerSequenceValueGenerator)factory.Create(property);

            Assert.Equal("Zeppelin", generator.SequenceName);
            Assert.Equal(11, generator.BlockSize);
        }

        [Fact]
        public void Returns_the_default_pool_size()
        {
            var property = CreateProperty();

            var factory = new SqlServerSequenceValueGeneratorFactory(new SqlStatementExecutor(new NullLoggerFactory()));

            Assert.Equal(5, factory.GetPoolSize(property));
        }

        [Fact]
        public void Sequence_name_is_the_cache_key()
        {
            var property = CreateProperty();
            property[Entity.Metadata.SqlServerMetadataExtensions.Annotations.SequenceName] = "Led";

            var factory = new SqlServerSequenceValueGeneratorFactory(new SqlStatementExecutor(new NullLoggerFactory()));

            Assert.Equal("Led", factory.GetCacheKey(property));
        }

        protected static IEnumerable<Type> GetAllTypes(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                yield return type;

                foreach (var nestedType in GetAllTypes(type.GetNestedTypes()))
                {
                    yield return nestedType;
                }
            }
        }

        private static Property CreateProperty()
        {
            var entityType = new Model().AddEntityType("MyType");
            var property = entityType.GetOrAddProperty("MyProperty", typeof(string), shadowProperty: true);
            entityType.SetTableName("MyTable");

            return property;
        }
    }
}
