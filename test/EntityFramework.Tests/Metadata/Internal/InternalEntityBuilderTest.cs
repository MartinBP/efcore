﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Microsoft.Data.Entity.Metadata.Internal
{
    public class InternalEntityBuilderTest
    {
        [Fact]
        public void ForeignKey_returns_same_instance_for_clr_properties()
        {
            var modelBuilder = new InternalModelBuilder(new Model(), null);
            modelBuilder.Entity(typeof(Customer)).Key(new[] { Customer.IdProperty, Customer.UniqueProperty });
            var entityBuilder = modelBuilder.Entity(typeof(Order));

            var foreignKeyBuilder = entityBuilder.ForeignKey(typeof(Customer), new[] { Order.CustomerIdProperty, Order.CustomerUniqueProperty });

            Assert.NotNull(foreignKeyBuilder);
            Assert.Same(foreignKeyBuilder, entityBuilder.ForeignKey(typeof(Customer).FullName, new[] { Order.CustomerIdProperty.Name, Order.CustomerUniqueProperty.Name }));
        }

        [Fact]
        public void ForeignKey_returns_same_instance_for_property_names()
        {
            var modelBuilder = new InternalModelBuilder(new Model(), null);
            modelBuilder.Entity(typeof(Customer)).Key(new[] { Customer.IdProperty, Customer.UniqueProperty });
            var entityBuilder = modelBuilder.Entity(typeof(Order));
            entityBuilder.Property(Order.CustomerIdProperty);
            entityBuilder.Property(Order.CustomerUniqueProperty);

            var foreignKeyBuilder = entityBuilder.ForeignKey(typeof(Customer).FullName, new[] { Order.CustomerIdProperty.Name, Order.CustomerUniqueProperty.Name });

            Assert.NotNull(foreignKeyBuilder);
            Assert.Same(foreignKeyBuilder, entityBuilder.ForeignKey(typeof(Customer), new[] { Order.CustomerIdProperty, Order.CustomerUniqueProperty }));
        }

        [Fact]
        public void Index_returns_same_instance_for_clr_properties()
        {
            var entityType = new EntityType(typeof(Order));
            var entityBuilder = new InternalEntityBuilder(entityType, new InternalModelBuilder(new Model(), null));

            var indexBuilder = entityBuilder.Index(new[] { Order.IdProperty, Order.CustomerIdProperty });

            Assert.NotNull(indexBuilder);
            Assert.Same(indexBuilder, entityBuilder.Index(new[] { Order.IdProperty.Name, Order.CustomerIdProperty.Name }));
        }

        [Fact]
        public void Index_returns_same_instance_for_property_names()
        {
            var entityType = new EntityType(typeof(Order));
            var entityBuilder = new InternalEntityBuilder(entityType, new InternalModelBuilder(new Model(), null));
            entityType.GetOrAddProperty(Order.IdProperty);
            entityType.GetOrAddProperty(Order.CustomerIdProperty);

            var indexBuilder = entityBuilder.Index(new[] { Order.IdProperty.Name, Order.CustomerIdProperty.Name });

            Assert.NotNull(indexBuilder);
            Assert.Same(indexBuilder, entityBuilder.Index(new[] { Order.IdProperty, Order.CustomerIdProperty }));
        }

        [Fact]
        public void Key_returns_same_instance_for_clr_properties()
        {
            var entityType = new EntityType(typeof(Order));
            var entityBuilder = new InternalEntityBuilder(entityType, new InternalModelBuilder(new Model(), null));

            var keyBuilder = entityBuilder.Key(new[] { Order.IdProperty, Order.CustomerIdProperty });

            Assert.NotNull(keyBuilder);
            Assert.Same(keyBuilder, entityBuilder.Key(new[] { Order.IdProperty.Name, Order.CustomerIdProperty.Name }));
        }

        [Fact]
        public void Key_returns_same_instance_for_property_names()
        {
            var entityType = new EntityType(typeof(Order));
            var entityBuilder = new InternalEntityBuilder(entityType, new InternalModelBuilder(new Model(), null));
            entityType.GetOrAddProperty(Order.IdProperty);
            entityType.GetOrAddProperty(Order.CustomerIdProperty);

            var keyBuilder = entityBuilder.Key(new[] { Order.IdProperty.Name, Order.CustomerIdProperty.Name });

            Assert.NotNull(keyBuilder);
            Assert.Same(keyBuilder, entityBuilder.Key(new[] { Order.IdProperty, Order.CustomerIdProperty }));
        }

        [Fact]
        public void Property_returns_same_instance_for_clr_properties()
        {
            var entityType = new EntityType(typeof(Order));
            var entityBuilder = new InternalEntityBuilder(entityType, new InternalModelBuilder(new Model(), null));

            var propertyBuilder = entityBuilder.Property(Order.IdProperty);

            Assert.NotNull(propertyBuilder);
            Assert.Same(propertyBuilder, entityBuilder.Property(typeof(Order), Order.IdProperty.Name));
        }

        [Fact]
        public void Property_returns_same_instance_for_property_names()
        {
            var entityType = new EntityType(typeof(Order));
            var entityBuilder = new InternalEntityBuilder(entityType, new InternalModelBuilder(new Model(), null));

            var propertyBuilder = entityBuilder.Property(typeof(Order), Order.IdProperty.Name);

            Assert.NotNull(propertyBuilder);
            Assert.Same(propertyBuilder, entityBuilder.Property(Order.IdProperty));
        }

        [Fact]
        public void BuildRelationship_returns_same_instance_for_clr_types()
        {
            var modelBuilder = new InternalModelBuilder(new Model(), null);
            var customerEntityBuilder = modelBuilder.Entity(typeof(Customer));
            customerEntityBuilder.Key(new[] { Customer.IdProperty, Customer.UniqueProperty });
            var orderEntityBuilder = modelBuilder.Entity(typeof(Order));

            var relationshipBuilder = orderEntityBuilder.BuildRelationship(typeof(Customer), typeof(Order), null, null, oneToOne: true);

            Assert.NotNull(relationshipBuilder);
            Assert.Same(relationshipBuilder, orderEntityBuilder.BuildRelationship(customerEntityBuilder.Metadata, orderEntityBuilder.Metadata, null, null, oneToOne: true));
        }

        [Fact]
        public void BuildRelationship_returns_same_instance_for_entity_type()
        {
            var modelBuilder = new InternalModelBuilder(new Model(), null);
            var customerEntityBuilder = modelBuilder.Entity(typeof(Customer));
            customerEntityBuilder.Key(new[] { Customer.IdProperty, Customer.UniqueProperty });
            var orderEntityBuilder = modelBuilder.Entity(typeof(Order));

            var relationshipBuilder = orderEntityBuilder.BuildRelationship(customerEntityBuilder.Metadata, orderEntityBuilder.Metadata, null, null, oneToOne: true);

            Assert.NotNull(relationshipBuilder);
            Assert.Same(relationshipBuilder, orderEntityBuilder.BuildRelationship(typeof(Customer), typeof(Order), null, null, oneToOne: true));
        }

        [Fact]
        public void ReplaceForeignKey_returns_same_instance_same_entity()
        {
            var modelBuilder = new InternalModelBuilder(new Model(), null);
            var customerEntityBuilder = modelBuilder.Entity(typeof(Customer));
            customerEntityBuilder.Key(new[] { Customer.IdProperty, Customer.UniqueProperty });
            var orderEntityBuilder = modelBuilder.Entity(typeof(Order));

            var relationshipBuilder = orderEntityBuilder.BuildRelationship(typeof(Customer), typeof(Order), null, null, oneToOne: true);
            var newRelationshipBuilder = orderEntityBuilder.ReplaceForeignKey(relationshipBuilder, new Property[0], new Property[0]);

            Assert.NotNull(relationshipBuilder);
            Assert.Same(newRelationshipBuilder, orderEntityBuilder.BuildRelationship(typeof(Customer), typeof(Order), null, null, oneToOne: true));
        }

        [Fact]
        public void ReplaceForeignKey_returns_same_instance_different_entity()
        {
            var modelBuilder = new InternalModelBuilder(new Model(), null);
            var customerEntityBuilder = modelBuilder.Entity(typeof(Customer));
            customerEntityBuilder.Key(new[] { Customer.IdProperty, Customer.UniqueProperty });
            var orderEntityBuilder = modelBuilder.Entity(typeof(Order));

            var relationshipBuilder = orderEntityBuilder.BuildRelationship(typeof(Customer), typeof(Order), null, null, oneToOne: true);
            var newRelationshipBuilder = orderEntityBuilder.ReplaceForeignKey(relationshipBuilder.Invert(), relationshipBuilder.Metadata.ReferencedProperties, relationshipBuilder.Metadata.Properties);

            Assert.NotNull(relationshipBuilder);
            Assert.Same(newRelationshipBuilder, orderEntityBuilder.BuildRelationship(typeof(Order), typeof(Customer), null, null, oneToOne: true));
        }

        private class Order
        {
            public static readonly PropertyInfo IdProperty = typeof(Order).GetProperty("Id");
            public static readonly PropertyInfo CustomerIdProperty = typeof(Order).GetProperty("CustomerId");
            public static readonly PropertyInfo CustomerUniqueProperty = typeof(Order).GetProperty("CustomerUnique");

            public int Id { get; set; }
            public int CustomerId { get; set; }
            public Guid CustomerUnique { get; set; }
            public Customer Customer { get; set; }

            public Order OrderCustomer { get; set; }
        }

        private class Customer
        {
            public static readonly PropertyInfo IdProperty = typeof(Customer).GetProperty("Id");
            public static readonly PropertyInfo NameProperty = typeof(Customer).GetProperty("Name");
            public static readonly PropertyInfo UniqueProperty = typeof(Customer).GetProperty("Unique");

            public int Id { get; set; }
            public Guid Unique { get; set; }
            public string Name { get; set; }
            public string Mane { get; set; }
            public ICollection<Order> Orders { get; set; }

            public IEnumerable<Order> EnumerableOrders { get; set; }
            public Order NotCollectionOrders { get; set; }
        }
    }
}
