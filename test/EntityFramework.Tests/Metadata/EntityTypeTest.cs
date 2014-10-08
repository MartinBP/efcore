// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Data.Entity.Metadata;
using Xunit;

namespace Microsoft.Data.Entity.Tests.Metadata
{
    public class EntityTypeTest
    {
        [Fact]
        public void Can_create_entity_type()
        {
            var entityType = new EntityType(typeof(Customer), new Model());

            Assert.Equal(typeof(Customer).FullName, entityType.Name);
            Assert.Same(typeof(Customer), entityType.Type);
        }

        [Fact]
        public void Simple_name_is_simple_CLR_name()
        {
            Assert.Equal("EntityTypeTest", new EntityType(typeof(EntityTypeTest), new Model()).SimpleName);
            Assert.Equal("Customer", new EntityType(typeof(Customer), new Model()).SimpleName);
            Assert.Equal("List`1", new EntityType(typeof(List<Customer>), new Model()).SimpleName);
        }

        [Fact]
        public void Simple_name_is_part_of_name_following_final_separator_when_no_CLR_type()
        {
            Assert.Equal("Everything", new EntityType("Everything", new Model()).SimpleName);
            Assert.Equal("Is", new EntityType("Everything.Is", new Model()).SimpleName);
            Assert.Equal("Awesome", new EntityType("Everything.Is.Awesome", new Model()).SimpleName);
            Assert.Equal("WhenWe`reLivingOurDream", new EntityType("Everything.Is.Awesome+WhenWe`reLivingOurDream", new Model()).SimpleName);
        }

        [Fact]
        public void Can_set_reset_and_clear_primary_key()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var idProperty = entityType.GetOrAddProperty(Customer.IdProperty);
            var nameProperty = entityType.GetOrAddProperty(Customer.NameProperty);

            var key1 = entityType.SetPrimaryKey(new[] { idProperty, nameProperty });

            Assert.NotNull(key1);
            Assert.Same(key1, entityType.GetPrimaryKey());
            Assert.Same(key1, entityType.TryGetPrimaryKey());
            Assert.Same(key1, entityType.Keys.Single());

            var key2 = entityType.SetPrimaryKey(idProperty);

            Assert.NotNull(key2);
            Assert.Same(key2, entityType.GetPrimaryKey());
            Assert.Same(key2, entityType.TryGetPrimaryKey());
            Assert.Same(key2, entityType.Keys.Single());

            Assert.Null(entityType.SetPrimaryKey((Property)null));

            Assert.Null(entityType.TryGetPrimaryKey());
            Assert.Empty(entityType.Keys);

            Assert.Null(entityType.SetPrimaryKey(new Property[0]));

            Assert.Null(entityType.TryGetPrimaryKey());
            Assert.Empty(entityType.Keys);

            Assert.Equal(
                Strings.FormatEntityRequiresKey(typeof(Customer).FullName),
                Assert.Throws<ModelItemNotFoundException>(() => entityType.GetPrimaryKey()).Message);
        }

        [Fact]
        public void Setting_primary_key_throws_if_properties_from_different_type()
        {
            var entityType1 = new EntityType(typeof(Customer), new Model());
            var entityType2 = new EntityType(typeof(Order), new Model());
            var idProperty = entityType2.GetOrAddProperty(Customer.IdProperty);

            Assert.Equal(
                Strings.FormatKeyPropertiesWrongEntity("'" + Customer.IdProperty.Name + "'", typeof(Customer).FullName),
                Assert.Throws<ArgumentException>(() => entityType1.SetPrimaryKey(idProperty)).Message);
        }

        [Fact]
        public void Can_get_set_reset_and_clear_primary_key()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var idProperty = entityType.GetOrAddProperty(Customer.IdProperty);
            var nameProperty = entityType.GetOrAddProperty(Customer.NameProperty);

            var key1 = entityType.GetOrSetPrimaryKey(new[] { idProperty, nameProperty });

            Assert.NotNull(key1);
            Assert.Same(key1, entityType.GetOrSetPrimaryKey(new[] { idProperty, nameProperty }));
            Assert.Same(key1, entityType.GetPrimaryKey());
            Assert.Same(key1, entityType.TryGetPrimaryKey());
            Assert.Same(key1, entityType.Keys.Single());

            var key2 = entityType.GetOrSetPrimaryKey(idProperty);

            Assert.NotNull(key2);
            Assert.NotEqual(key1, key2);
            Assert.Same(key2, entityType.GetOrSetPrimaryKey(idProperty));
            Assert.Same(key2, entityType.GetPrimaryKey());
            Assert.Same(key2, entityType.TryGetPrimaryKey());
            Assert.Same(key2, entityType.Keys.Single());

            Assert.Null(entityType.GetOrSetPrimaryKey((Property)null));

            Assert.Null(entityType.TryGetPrimaryKey());
            Assert.Empty(entityType.Keys);

            Assert.Null(entityType.GetOrSetPrimaryKey(new Property[0]));

            Assert.Null(entityType.TryGetPrimaryKey());
            Assert.Empty(entityType.Keys);

            Assert.Equal(
                Strings.FormatEntityRequiresKey(typeof(Customer).FullName),
                Assert.Throws<ModelItemNotFoundException>(() => entityType.GetPrimaryKey()).Message);
        }

        [Fact]
        public void Clearing_the_primary_throws_if_it_referenced_from_a_foreign_key_in_the_model()
        {
            var model = new Model();
            var customerType = model.AddEntityType(typeof(Customer));
            var customerPk = customerType.GetOrSetPrimaryKey(customerType.AddProperty(Customer.IdProperty));

            var orderType = model.AddEntityType(typeof(Order));
            var customerFk = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            orderType.GetOrAddForeignKey(customerFk, customerPk);

            Assert.Equal(
                Strings.FormatKeyInUse("'" + Customer.IdProperty.Name + "'", typeof(Customer).FullName, typeof(Order).FullName),
                Assert.Throws<InvalidOperationException>(() => customerType.SetPrimaryKey((Property)null)).Message);
        }

        [Fact]
        public void Changing_the_primary_key_throws_if_it_referenced_from_a_foreign_key_in_the_model()
        {
            var model = new Model();

            var customerType = model.AddEntityType(typeof(Customer));
            var customerPk = customerType.GetOrSetPrimaryKey(customerType.AddProperty(Customer.IdProperty));

            var orderType = model.AddEntityType(typeof(Order));
            var customerFk = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            orderType.GetOrAddForeignKey(customerFk, customerPk);

            Assert.Equal(
                Strings.FormatKeyInUse("'" + Customer.IdProperty.Name + "'", typeof(Customer).FullName, typeof(Order).FullName),
                Assert.Throws<InvalidOperationException>(
                    () => customerType.SetPrimaryKey(customerType.GetOrAddProperty(Customer.NameProperty))).Message);
        }

        [Fact]
        public void Can_add_and_get_a_key()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var idProperty = entityType.GetOrAddProperty(Customer.IdProperty);
            var nameProperty = entityType.GetOrAddProperty(Customer.NameProperty);

            var key1 = entityType.AddKey(new[] { idProperty, nameProperty });

            Assert.NotNull(key1);
            Assert.Same(key1, entityType.GetOrAddKey(new[] { idProperty, nameProperty }));
            Assert.Same(key1, entityType.Keys.Single());

            var key2 = entityType.GetOrAddKey(idProperty);

            Assert.NotNull(key2);
            Assert.Same(key2, entityType.GetKey(idProperty));
            Assert.Equal(new[] { key1, key2 }, entityType.Keys.ToArray());
        }

        [Fact]
        public void Adding_a_key_throws_if_properties_from_different_type()
        {
            var entityType1 = new EntityType(typeof(Customer), new Model());
            var entityType2 = new EntityType(typeof(Order), new Model());
            var idProperty = entityType2.GetOrAddProperty(Customer.IdProperty);

            Assert.Equal(
                Strings.FormatKeyPropertiesWrongEntity("'" + Customer.IdProperty.Name + "'", typeof(Customer).FullName),
                Assert.Throws<ArgumentException>(() => entityType1.AddKey(idProperty)).Message);
        }

        [Fact]
        public void Adding_a_key_throws_if_duplicated()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var idProperty = entityType.GetOrAddProperty(Customer.IdProperty);
            var nameProperty = entityType.GetOrAddProperty(Customer.NameProperty);
            entityType.GetOrAddKey(new[] { idProperty, nameProperty });

            Assert.Equal(
                Strings.FormatDuplicateKey("'" + Customer.IdProperty.Name + "', '" + Customer.NameProperty.Name + "'", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(() => entityType.AddKey(new[] { idProperty, nameProperty })).Message);
        }

        [Fact]
        public void Adding_a_key_throws_if_same_as_primary()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var idProperty = entityType.GetOrAddProperty(Customer.IdProperty);
            var nameProperty = entityType.GetOrAddProperty(Customer.NameProperty);
            entityType.GetOrSetPrimaryKey(new[] { idProperty, nameProperty });

            Assert.Equal(
                Strings.FormatDuplicateKey("'" + Customer.IdProperty.Name + "', '" + Customer.NameProperty.Name + "'", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(() => entityType.AddKey(new[] { idProperty, nameProperty })).Message);
        }

        [Fact]
        public void Can_remove_keys()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var idProperty = entityType.GetOrAddProperty(Customer.IdProperty);
            var nameProperty = entityType.GetOrAddProperty(Customer.NameProperty);

            Assert.Equal(
                Strings.FormatKeyNotFound("'" + idProperty.Name + "', '" + nameProperty.Name + "'", typeof(Customer).FullName),
                Assert.Throws<ModelItemNotFoundException>(() => entityType.GetKey(new[] { idProperty, nameProperty })).Message);
            Assert.Null(entityType.RemoveKey(new Key(new[] { idProperty })));

            var key1 = entityType.GetOrSetPrimaryKey(new[] { idProperty, nameProperty });
            var key2 = entityType.GetOrAddKey(idProperty);

            Assert.Equal(new[] { key1, key2 }, entityType.Keys.ToArray());

            Assert.Same(key1, entityType.RemoveKey(key1));
            Assert.Null(entityType.RemoveKey(key1));

            Assert.Equal(
                Strings.FormatKeyNotFound("'" + idProperty.Name + "', '" + nameProperty.Name + "'", typeof(Customer).FullName),
                Assert.Throws<ModelItemNotFoundException>(() => entityType.GetKey(new[] { idProperty, nameProperty })).Message);

            Assert.Equal(new[] { key2 }, entityType.Keys.ToArray());

            Assert.Same(key2, entityType.RemoveKey(new Key(new[] { idProperty })));

            Assert.Empty(entityType.Keys);
        }

        [Fact]
        public void Removing_a_key_throws_if_it_referenced_from_a_foreign_key_in_the_model()
        {
            var model = new Model();

            var customerType = model.AddEntityType(typeof(Customer));
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = model.AddEntityType(typeof(Order));
            var customerFk = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            orderType.GetOrAddForeignKey(customerFk, customerKey);

            Assert.Equal(
                Strings.FormatKeyInUse("'" + Customer.IdProperty.Name + "'", typeof(Customer).FullName, typeof(Order).FullName),
                Assert.Throws<InvalidOperationException>(() => customerType.RemoveKey(customerKey)).Message);
        }

        [Fact]
        public void Key_properties_are_always_read_only()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var idProperty = entityType.GetOrAddProperty(Customer.IdProperty);
            var nameProperty = entityType.GetOrAddProperty(Customer.NameProperty);

            Assert.False(idProperty.IsReadOnly);
            Assert.False(nameProperty.IsReadOnly);

            entityType.GetOrAddKey(new[] { idProperty, nameProperty });

            Assert.True(idProperty.IsReadOnly);
            Assert.True(nameProperty.IsReadOnly);

            nameProperty.IsReadOnly = true;

            Assert.Equal(
                Strings.FormatKeyPropertyMustBeReadOnly(Customer.NameProperty.Name, typeof(Customer).FullName),
                Assert.Throws<NotSupportedException>(() => nameProperty.IsReadOnly = false).Message);

            Assert.True(idProperty.IsReadOnly);
            Assert.True(nameProperty.IsReadOnly);
        }

        [Fact]
        public void Can_add_a_foreign_key()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var idProperty = customerType.GetOrAddProperty(Customer.IdProperty);
            var customerKey = customerType.GetOrAddKey(idProperty);
            var orderType = new EntityType(typeof(Order), new Model());
            var customerFk1 = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerFk2 = orderType.GetOrAddProperty("IdAgain", typeof(int), shadowProperty: true);

            var fk1 = orderType.AddForeignKey(customerFk1, customerKey);

            Assert.NotNull(fk1);
            Assert.Same(fk1, orderType.GetForeignKey(customerFk1));
            Assert.Same(fk1, orderType.TryGetForeignKey(customerFk1));
            Assert.Same(fk1, orderType.GetOrAddForeignKey(customerFk1, new Key(new[] { idProperty })));
            Assert.Same(fk1, orderType.ForeignKeys.Single());

            var fk2 = orderType.AddForeignKey(customerFk2, customerKey);
            Assert.Same(fk2, orderType.GetForeignKey(customerFk2));
            Assert.Same(fk2, orderType.TryGetForeignKey(customerFk2));
            Assert.Same(fk2, orderType.GetOrAddForeignKey(customerFk2, new Key(new[] { idProperty })));
            Assert.Equal(new[] { fk1, fk2 }, orderType.ForeignKeys.ToArray());
        }

        [Fact]
        public void Adding_a_foreign_key_throws_if_duplicate()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));
            var orderType = new EntityType(typeof(Order), new Model());
            var customerFk1 = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            orderType.AddForeignKey(customerFk1, customerKey);

            Assert.Equal(
                Strings.FormatDuplicateForeignKey("'" + Order.CustomerIdProperty.Name + "'", typeof(Order).FullName),
                Assert.Throws<InvalidOperationException>(() => orderType.AddForeignKey(customerFk1, customerKey)).Message);
        }

        [Fact]
        public void Adding_a_foreign_key_throws_if_properties_from_different_type()
        {
            var entityType1 = new EntityType(typeof(Customer), new Model());
            var entityType2 = new EntityType(typeof(Order), new Model());
            var idProperty = entityType2.GetOrAddProperty(Customer.IdProperty);

            Assert.Equal(
                Strings.FormatForeignKeyPropertiesWrongEntity("'" + Customer.IdProperty.Name + "'", typeof(Customer).FullName),
                Assert.Throws<ArgumentException>(() => entityType1.AddForeignKey(new[] { idProperty }, entityType2.GetOrAddKey(idProperty))).Message);
        }

        [Fact]
        public void Can_get_or_add_a_foreign_key()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var idProperty = customerType.GetOrAddProperty(Customer.IdProperty);
            var customerKey = customerType.GetOrAddKey(idProperty);
            var orderType = new EntityType(typeof(Order), new Model());
            var customerFk1 = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerFk2 = orderType.GetOrAddProperty("IdAgain", typeof(int), shadowProperty: true);
            var fk1 = orderType.AddForeignKey(customerFk1, customerKey);

            var fk2 = orderType.GetOrAddForeignKey(customerFk2, customerKey);

            Assert.NotNull(fk2);
            Assert.NotEqual(fk1, fk2);
            Assert.Same(fk2, orderType.GetForeignKey(customerFk2));
            Assert.Same(fk2, orderType.TryGetForeignKey(customerFk2));
            Assert.Equal(new[] { fk1, fk2 }, orderType.ForeignKeys.ToArray());

            Assert.Same(fk2, orderType.GetOrAddForeignKey(customerFk2, customerKey));
            Assert.Equal(new[] { fk1, fk2 }, orderType.ForeignKeys.ToArray());
        }

        [Fact]
        public void Can_remove_foreign_keys()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));
            var orderType = new EntityType(typeof(Order), new Model());
            var customerFk1 = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerFk2 = orderType.GetOrAddProperty("IdAgain", typeof(int), shadowProperty: true);

            Assert.Equal(
                Strings.FormatForeignKeyNotFound("'" + Order.CustomerIdProperty.Name + "'", typeof(Order).FullName),
                Assert.Throws<ModelItemNotFoundException>(() => orderType.GetForeignKey(customerFk1)).Message);
            Assert.Null(orderType.RemoveForeignKey(new ForeignKey(new[] { customerFk2 }, customerKey)));

            var fk1 = orderType.AddForeignKey(customerFk1, customerKey);
            var fk2 = orderType.AddForeignKey(customerFk2, customerKey);

            Assert.Equal(new[] { fk1, fk2 }, orderType.ForeignKeys.ToArray());

            Assert.Same(fk1, orderType.RemoveForeignKey(fk1));
            Assert.Null(orderType.RemoveForeignKey(fk1));

            Assert.Equal(
                Strings.FormatForeignKeyNotFound("'" + Order.CustomerIdProperty.Name + "'", typeof(Order).FullName),
                Assert.Throws<ModelItemNotFoundException>(() => orderType.GetForeignKey(customerFk1)).Message);
            Assert.Equal(new[] { fk2 }, orderType.ForeignKeys.ToArray());

            Assert.Same(fk2, orderType.RemoveForeignKey(new ForeignKey(new[] { customerFk2 }, customerKey)));

            Assert.Empty(orderType.ForeignKeys);
        }

        [Fact]
        public void Removing_a_foreign_key_throws_if_it_referenced_from_a_navigation_in_the_model()
        {
            var model = new Model();

            var customerType = model.AddEntityType(typeof(Customer));
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = model.AddEntityType(typeof(Order));
            var customerFk = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var fk = orderType.GetOrAddForeignKey(customerFk, customerKey);

            orderType.AddNavigation("Customer", fk, pointsToPrincipal: true);

            Assert.Equal(
                Strings.FormatForeignKeyInUse("'" + Order.CustomerIdProperty.Name + "'", typeof(Order).FullName, "Customer", typeof(Order).FullName),
                Assert.Throws<InvalidOperationException>(() => orderType.RemoveForeignKey(fk)).Message);

            customerType.AddNavigation("Orders", fk, pointsToPrincipal: false);

            Assert.Equal(
                Strings.FormatForeignKeyInUse("'" + Order.CustomerIdProperty.Name + "'", typeof(Order).FullName, "Orders", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(() => orderType.RemoveForeignKey(fk)).Message);
        }

        [Fact]
        public void Can_add_and_remove_navigations()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = new EntityType(typeof(Order), new Model());
            var foreignKeyProperty = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerForeignKey = orderType.GetOrAddForeignKey(foreignKeyProperty, customerKey);
            Assert.Null(orderType.RemoveNavigation(new Navigation("Customer", customerForeignKey, pointsToPrincipal: true)));

            var customerNavigation = orderType.AddNavigation("Customer", customerForeignKey, pointsToPrincipal: true);
            var ordersNavigation = customerType.AddNavigation("Orders", customerForeignKey, pointsToPrincipal: false);

            Assert.Equal("Customer", customerNavigation.Name);
            Assert.Same(orderType, customerNavigation.EntityType);
            Assert.Same(customerForeignKey, customerNavigation.ForeignKey);
            Assert.True(customerNavigation.PointsToPrincipal);
            Assert.False(customerNavigation.IsCollection());
            Assert.Same(customerType, customerNavigation.GetTargetType());

            Assert.Equal("Orders", ordersNavigation.Name);
            Assert.Same(customerType, ordersNavigation.EntityType);
            Assert.Same(customerForeignKey, ordersNavigation.ForeignKey);
            Assert.False(ordersNavigation.PointsToPrincipal);
            Assert.True(ordersNavigation.IsCollection());
            Assert.Same(orderType, ordersNavigation.GetTargetType());

            Assert.Same(customerNavigation, orderType.Navigations.Single());
            Assert.Same(ordersNavigation, customerType.Navigations.Single());

            Assert.Same(customerNavigation, orderType.RemoveNavigation(customerNavigation));
            Assert.Null(orderType.RemoveNavigation(customerNavigation));
            Assert.Empty(orderType.Navigations);

            Assert.Same(ordersNavigation, customerType.RemoveNavigation(new Navigation("Orders", customerForeignKey, pointsToPrincipal: false)));
            Assert.Empty(customerType.Navigations);
        }

        [Fact]
        public void Can_add_new_navigations_or_get_existing_navigations()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = new EntityType(typeof(Order), new Model());
            var foreignKeyProperty = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerForeignKey = orderType.GetOrAddForeignKey(foreignKeyProperty, customerKey);

            var customerNavigation = orderType.GetOrAddNavigation("Customer", customerForeignKey, pointsToPrincipal: true);

            Assert.Equal("Customer", customerNavigation.Name);
            Assert.Same(orderType, customerNavigation.EntityType);
            Assert.Same(customerForeignKey, customerNavigation.ForeignKey);
            Assert.True(customerNavigation.PointsToPrincipal);
            Assert.False(customerNavigation.IsCollection());
            Assert.Same(customerType, customerNavigation.GetTargetType());

            Assert.Same(customerNavigation, orderType.GetOrAddNavigation("Customer", customerForeignKey, pointsToPrincipal: false));
            Assert.True(customerNavigation.PointsToPrincipal);
        }

        [Fact]
        public void Can_get_navigation_and_can_try_get_navigation()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = new EntityType(typeof(Order), new Model());
            var foreignKeyProperty = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerForeignKey = orderType.GetOrAddForeignKey(foreignKeyProperty, customerKey);

            var customerNavigation = orderType.GetOrAddNavigation("Customer", customerForeignKey, pointsToPrincipal: true);

            Assert.Same(customerNavigation, orderType.TryGetNavigation("Customer"));
            Assert.Same(customerNavigation, orderType.GetNavigation("Customer"));

            Assert.Null(orderType.TryGetNavigation("Nose"));

            Assert.Equal(
                Strings.FormatNavigationNotFound("Nose", typeof(Order).FullName),
                Assert.Throws<ModelItemNotFoundException>(() => orderType.GetNavigation("Nose")).Message);
        }

        [Fact]
        public void Adding_a_new_navigation_with_a_name_that_already_exists_throws()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = new EntityType(typeof(Order), new Model());
            var foreignKeyProperty = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerForeignKey = orderType.GetOrAddForeignKey(foreignKeyProperty, customerKey);

            orderType.AddNavigation("Customer", customerForeignKey, pointsToPrincipal: true);

            Assert.Equal(
                Strings.FormatDuplicateNavigation("Customer", typeof(Order).FullName),
                Assert.Throws<InvalidOperationException>(
                    () => orderType.AddNavigation("Customer", customerForeignKey, pointsToPrincipal: true)).Message);
        }

        [Fact]
        public void Adding_a_navigation_that_belongs_to_a_different_type_throws()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = new EntityType(typeof(Order), new Model());
            var foreignKeyProperty = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerForeignKey = orderType.GetOrAddForeignKey(foreignKeyProperty, customerKey);

            Assert.Equal(
                Strings.FormatNavigationAlreadyOwned("Customer", typeof(Customer).FullName, typeof(Order).FullName),
                Assert.Throws<InvalidOperationException>(() => customerType.AddNavigation("Customer", customerForeignKey, pointsToPrincipal: true)).Message);
        }

        [Fact]
        public void Adding_a_navigation_to_a_shadow_entity_type_throws()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty("Id", typeof(int), shadowProperty: true));

            var orderType = new EntityType("Order", new Model());
            var foreignKeyProperty = orderType.GetOrAddProperty("CustomerId", typeof(int), shadowProperty: true);
            var customerForeignKey = orderType.GetOrAddForeignKey(foreignKeyProperty, customerKey);

            Assert.Equal(
                Strings.FormatNavigationOnShadowEntity("Customer", "Order"),
                Assert.Throws<InvalidOperationException>(
                    () => orderType.AddNavigation("Customer", customerForeignKey, pointsToPrincipal: true)).Message);
        }

        [Fact]
        public void Adding_a_navigation_pointing_to_a_shadow_entity_type_throws()
        {
            var customerType = new EntityType("Customer", new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty("Id", typeof(int), shadowProperty: true));

            var orderType = new EntityType(typeof(Order), new Model());
            var foreignKeyProperty = orderType.GetOrAddProperty("CustomerId", typeof(int), shadowProperty: true);
            var customerForeignKey = orderType.GetOrAddForeignKey(foreignKeyProperty, customerKey);

            Assert.Equal(
                Strings.FormatNavigationToShadowEntity("Customer", typeof(Order).FullName, "Customer"),
                Assert.Throws<InvalidOperationException>(
                    () => orderType.AddNavigation("Customer", customerForeignKey, pointsToPrincipal: true)).Message);
        }

        [Fact]
        public void Adding_a_navigation_that_doesnt_match_a_CLR_property_throws()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = new EntityType(typeof(Order), new Model());
            var foreignKeyProperty = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerForeignKey = orderType.GetOrAddForeignKey(foreignKeyProperty, customerKey);

            Assert.Equal(
                Strings.FormatNoClrNavigation("Snook", typeof(Order).FullName),
                Assert.Throws<InvalidOperationException>(
                    () => orderType.AddNavigation("Snook", customerForeignKey, pointsToPrincipal: true)).Message);
        }

        [Fact]
        public void Collection_navigation_properties_must_be_IEnumerables_of_the_target_type()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = new EntityType(typeof(Order), new Model());
            var foreignKeyProperty = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerForeignKey = orderType.GetOrAddForeignKey(foreignKeyProperty, customerKey);

            Assert.Equal(
                Strings.FormatWrongClrCollectionNavigationType("NotCollectionOrders", typeof(Customer).FullName, typeof(Order).FullName, typeof(Order).FullName),
                Assert.Throws<InvalidOperationException>(
                    () => customerType.AddNavigation("NotCollectionOrders", customerForeignKey, pointsToPrincipal: false)).Message);

            Assert.Equal(
                Strings.FormatWrongClrCollectionNavigationType("DerivedOrders", typeof(Customer).FullName, typeof(IEnumerable<SpecialOrder>).FullName, typeof(Order).FullName),
                Assert.Throws<InvalidOperationException>(
                    () => customerType.AddNavigation("DerivedOrders", customerForeignKey, pointsToPrincipal: false)).Message);
        }

        [Fact]
        public void Reference_navigation_properties_must_be_of_the_target_type()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = new EntityType(typeof(Order), new Model());
            var foreignKeyProperty = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerForeignKey = orderType.GetOrAddForeignKey(foreignKeyProperty, customerKey);

            Assert.Equal(
                Strings.FormatWrongClrSingleNavigationType("OrderCustomer", typeof(Order).FullName, typeof(Order).FullName, typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(
                    () => orderType.AddNavigation("OrderCustomer", customerForeignKey, pointsToPrincipal: true)).Message);

            Assert.Equal(
                Strings.FormatWrongClrSingleNavigationType("DerivedCustomer", typeof(Order).FullName, typeof(SpecialCustomer).FullName, typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(
                    () => orderType.AddNavigation("DerivedCustomer", customerForeignKey, pointsToPrincipal: true)).Message);
        }

        [Fact]
        public void Multiple_sets_of_navigations_using_the_same_foreign_key_are_not_allowed()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerKey = customerType.GetOrAddKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = new EntityType(typeof(Order), new Model());
            var foreignKeyProperty = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            var customerForeignKey = orderType.GetOrAddForeignKey(foreignKeyProperty, customerKey);

            customerType.AddNavigation("EnumerableOrders", customerForeignKey, pointsToPrincipal: false);

            Assert.Equal(
                Strings.FormatMultipleNavigations("Orders", "EnumerableOrders", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(
                    () => customerType.AddNavigation("Orders", customerForeignKey, pointsToPrincipal: false)).Message);
        }

        [Fact]
        public void Can_add_retrieve_and_remove_indexes()
        {
            var entityType = new EntityType(typeof(Order), new Model());
            var property1 = entityType.GetOrAddProperty(Order.IdProperty);
            var property2 = entityType.GetOrAddProperty(Order.CustomerIdProperty);

            Assert.Equal(0, entityType.Indexes.Count);
            Assert.Null(entityType.RemoveIndex(new Index(new[] { property1 })));

            var index1 = entityType.GetOrAddIndex(property1);

            Assert.Equal(1, index1.Properties.Count);
            Assert.Same(index1, entityType.GetIndex(property1));
            Assert.Same(index1, entityType.TryGetIndex(property1));
            Assert.Same(property1, index1.Properties[0]);

            var index2 = entityType.AddIndex(new[] { property1, property2 });

            Assert.Equal(2, index2.Properties.Count);
            Assert.Same(index2, entityType.GetOrAddIndex(new[] { property1, property2 }));
            Assert.Same(index2, entityType.TryGetIndex(new[] { property1, property2 }));
            Assert.Same(property1, index2.Properties[0]);
            Assert.Same(property2, index2.Properties[1]);

            Assert.Equal(2, entityType.Indexes.Count);
            Assert.Same(index1, entityType.Indexes[0]);
            Assert.Same(index2, entityType.Indexes[1]);

            Assert.Same(index1, entityType.RemoveIndex(index1));
            Assert.Null(entityType.RemoveIndex(index1));

            Assert.Equal(1, entityType.Indexes.Count);
            Assert.Same(index2, entityType.Indexes[0]);

            Assert.Same(index2, entityType.RemoveIndex(new Index(new[] { property1, property2 })));

            Assert.Equal(0, entityType.Indexes.Count);
        }

        [Fact]
        public void AddIndex_throws_if_not_from_same_entity()
        {
            var entityType1 = new EntityType(typeof(Customer), new Model());
            var entityType2 = new EntityType(typeof(Order), new Model());
            var property1 = entityType1.GetOrAddProperty(Customer.IdProperty);
            var property2 = entityType1.GetOrAddProperty(Customer.NameProperty);

            Assert.Equal(Strings.FormatIndexPropertiesWrongEntity("'" + Customer.IdProperty.Name + "', '" + Customer.NameProperty.Name + "'", typeof(Order).FullName),
                Assert.Throws<ArgumentException>(
                    () => entityType2.AddIndex(new[] { property1, property2 })).Message);
        }

        [Fact]
        public void AddIndex_throws_if_duplicate()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var property1 = entityType.GetOrAddProperty(Customer.IdProperty);
            var property2 = entityType.GetOrAddProperty(Customer.NameProperty);
            entityType.AddIndex(new[] { property1, property2 });

            Assert.Equal(Strings.FormatDuplicateIndex("'" + Customer.IdProperty.Name + "', '" + Customer.NameProperty.Name + "'", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(
                    () => entityType.AddIndex(new[] { property1, property2 })).Message);
        }

        [Fact]
        public void GetIndex_throws_if_index_not_found()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var property1 = entityType.GetOrAddProperty(Customer.IdProperty);
            var property2 = entityType.GetOrAddProperty(Customer.NameProperty);

            Assert.Equal(Strings.FormatIndexNotFound("'" + Customer.IdProperty.Name + "', '" + Customer.NameProperty.Name + "'", typeof(Customer).FullName),
                Assert.Throws<ModelItemNotFoundException>(
                    () => entityType.GetIndex(new[] { property1, property2 })).Message);

            entityType.AddIndex(property1);

            Assert.Equal(Strings.FormatIndexNotFound("'" + Customer.IdProperty.Name + "', '" + Customer.NameProperty.Name + "'", typeof(Customer).FullName),
                Assert.Throws<ModelItemNotFoundException>(
                    () => entityType.GetIndex(new[] { property1, property2 })).Message);
        }

        [Fact]
        public void Can_add_and_remove_properties()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            Assert.Null(entityType.RemoveProperty(new Property("Id", typeof(int), entityType)));

            var property1 = entityType.AddProperty("Id", typeof(int));

            Assert.False(property1.IsShadowProperty);
            Assert.Equal("Id", property1.Name);
            Assert.Same(typeof(int), property1.PropertyType);
            Assert.False(property1.IsConcurrencyToken);
            Assert.Same(entityType, property1.EntityType);

            var property2 = entityType.AddProperty("Name", typeof(string));

            Assert.True(new[] { property1, property2 }.SequenceEqual(entityType.Properties));

            Assert.Same(property1, entityType.RemoveProperty(property1));
            Assert.Null(entityType.RemoveProperty(property1));

            Assert.True(new[] { property2 }.SequenceEqual(entityType.Properties));

            Assert.Same(property2, entityType.RemoveProperty(new Property("Name", typeof(string), entityType)));

            Assert.Empty(entityType.Properties);
        }

        [Fact]
        public void Can_add_new_properties_or_get_existing_properties_using_PropertyInfo_or_name()
        {
            var entityType = new EntityType(typeof(Customer), new Model());

            var idProperty = entityType.GetOrAddProperty("Id", typeof(int));

            Assert.False(idProperty.IsShadowProperty);
            Assert.Equal("Id", idProperty.Name);
            Assert.Same(typeof(int), idProperty.PropertyType);
            Assert.False(idProperty.IsConcurrencyToken);
            Assert.Same(entityType, idProperty.EntityType);

            Assert.Same(idProperty, entityType.GetOrAddProperty(Customer.IdProperty));
            Assert.Same(idProperty, entityType.GetOrAddProperty("Id", typeof(int), shadowProperty: true));
            Assert.False(idProperty.IsShadowProperty);

            var nameProperty = entityType.GetOrAddProperty("Name", typeof(string), shadowProperty: true);

            Assert.True(nameProperty.IsShadowProperty);
            Assert.Equal("Name", nameProperty.Name);
            Assert.Same(typeof(string), nameProperty.PropertyType);
            Assert.False(nameProperty.IsConcurrencyToken);
            Assert.Same(entityType, nameProperty.EntityType);

            Assert.Same(nameProperty, entityType.GetOrAddProperty(Customer.NameProperty));
            Assert.Same(nameProperty, entityType.GetOrAddProperty("Name", typeof(string)));
            Assert.True(nameProperty.IsShadowProperty);

            Assert.True(new[] { idProperty, nameProperty }.SequenceEqual(entityType.Properties));
        }

        [Fact]
        public void Cannot_remove_property_when_used_by_primary_key()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var property = entityType.GetOrAddProperty(Customer.IdProperty);

            entityType.GetOrSetPrimaryKey(property);

            Assert.Equal(
                Strings.FormatPropertyInUse("Id", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(() => entityType.RemoveProperty(property)).Message);
        }

        [Fact]
        public void Cannot_remove_property_when_used_by_non_primary_key()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var property = entityType.GetOrAddProperty(Customer.IdProperty);

            entityType.GetOrAddKey(property);

            Assert.Equal(
                Strings.FormatPropertyInUse("Id", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(() => entityType.RemoveProperty(property)).Message);
        }

        [Fact]
        public void Cannot_remove_property_when_used_by_foreign_key()
        {
            var customerType = new EntityType(typeof(Customer), new Model());
            var customerPk = customerType.GetOrSetPrimaryKey(customerType.GetOrAddProperty(Customer.IdProperty));

            var orderType = new EntityType(typeof(Order), new Model());
            var customerFk = orderType.GetOrAddProperty(Order.CustomerIdProperty);
            orderType.GetOrAddForeignKey(customerFk, customerPk);

            Assert.Equal(
                Strings.FormatPropertyInUse("CustomerId", typeof(Order).FullName),
                Assert.Throws<InvalidOperationException>(() => orderType.RemoveProperty(customerFk)).Message);
        }

        [Fact]
        public void Cannot_remove_property_when_used_by_an_index()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var property = entityType.GetOrAddProperty(Customer.IdProperty);

            entityType.GetOrAddIndex(property);

            Assert.Equal(
                Strings.FormatPropertyInUse("Id", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(() => entityType.RemoveProperty(property)).Message);
        }

        [Fact]
        public void Properties_are_ordered_by_name()
        {
            var entityType = new EntityType(typeof(Customer), new Model());

            var property1 = entityType.GetOrAddProperty(Customer.IdProperty);
            var property2 = entityType.GetOrAddProperty(Customer.NameProperty);

            Assert.True(new[] { property1, property2 }.SequenceEqual(entityType.Properties));
        }

        [Fact]
        public void Properties_is_IList_to_ensure_collecting_the_count_is_fast()
        {
            Assert.IsAssignableFrom<IList<Property>>(new EntityType(typeof(Customer), new Model()).Properties);
        }

        [Fact]
        public void Can_get_property_and_can_try_get_property()
        {
            var entityType = new EntityType(typeof(Customer), new Model());
            var property = entityType.GetOrAddProperty(Customer.IdProperty);

            Assert.Same(property, entityType.TryGetProperty(Customer.IdProperty));
            Assert.Same(property, entityType.TryGetProperty("Id"));
            Assert.Same(property, entityType.GetProperty(Customer.IdProperty));
            Assert.Same(property, entityType.GetProperty("Id"));

            Assert.Null(entityType.TryGetProperty("Nose"));

            Assert.Equal(
                Strings.FormatPropertyNotFound("Nose", typeof(Customer).FullName),
                Assert.Throws<ModelItemNotFoundException>(() => entityType.GetProperty("Nose")).Message);
        }

        [Fact]
        public void Shadow_properties_have_CLR_flag_set_to_false()
        {
            var entityType = new EntityType(typeof(Customer), new Model());

            entityType.GetOrAddProperty(Customer.NameProperty);
            entityType.GetOrAddProperty("Id", typeof(int));
            entityType.GetOrAddProperty("Mane", typeof(int), shadowProperty: true);

            Assert.False(entityType.GetProperty("Name").IsShadowProperty);
            Assert.False(entityType.GetProperty("Id").IsShadowProperty);
            Assert.True(entityType.GetProperty("Mane").IsShadowProperty);
        }

        [Fact]
        public void Adding_a_new_property_with_a_name_that_already_exists_throws()
        {
            var entityType = new EntityType(typeof(Customer), new Model());

            entityType.AddProperty("Id", typeof(int));

            Assert.Equal(
                Strings.FormatDuplicateProperty("Id", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(() => entityType.AddProperty("Id", typeof(int))).Message);
        }

        [Fact]
        public void Adding_a_CLR_property_to_a_shadow_entity_type_throws()
        {
            var entityType = new EntityType("Hello", new Model());

            Assert.Equal(
                Strings.FormatClrPropertyOnShadowEntity("Kitty", "Hello"),
                Assert.Throws<InvalidOperationException>(() => entityType.AddProperty("Kitty", typeof(int))).Message);
        }

        [Fact]
        public void Adding_a_CLR_property_that_doesnt_match_a_CLR_property_throws()
        {
            var entityType = new EntityType(typeof(Customer), new Model());

            Assert.Equal(
                Strings.FormatNoClrProperty("Snook", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(() => entityType.AddProperty("Snook", typeof(int))).Message);
        }

        [Fact]
        public void Adding_a_CLR_property_where_the_type_doesnt_match_the_CLR_type_throws()
        {
            var entityType = new EntityType(typeof(Customer), new Model());

            Assert.Equal(
                Strings.FormatWrongClrPropertyType("Id", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(() => entityType.AddProperty("Id", typeof(string))).Message);
        }

        [Fact]
        public void Making_a_shadow_property_a_non_shadow_property_throws_if_CLR_property_does_not_match()
        {
            var entityType = new EntityType(typeof(Customer), new Model());

            var property1 = entityType.AddProperty("Snook", typeof(int), shadowProperty: true);
            var property2 = entityType.AddProperty("Id", typeof(string), shadowProperty: true);

            Assert.Equal(
                Strings.FormatNoClrProperty("Snook", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(() => property1.IsShadowProperty = false).Message);

            Assert.Equal(
                Strings.FormatWrongClrPropertyType("Id", typeof(Customer).FullName),
                Assert.Throws<InvalidOperationException>(() => property2.IsShadowProperty = false).Message);
        }

        [Fact]
        public void Can_get_property_indexes()
        {
            var entityType = new EntityType(typeof(Customer), new Model());

            entityType.GetOrAddProperty(Customer.NameProperty);
            entityType.GetOrAddProperty("Id", typeof(int), shadowProperty: true);
            entityType.GetOrAddProperty("Mane", typeof(int), shadowProperty: true);

            Assert.Equal(0, entityType.GetProperty("Id").Index);
            Assert.Equal(1, entityType.GetProperty("Mane").Index);
            Assert.Equal(2, entityType.GetProperty("Name").Index);

            Assert.Equal(0, entityType.GetProperty("Id").ShadowIndex);
            Assert.Equal(1, entityType.GetProperty("Mane").ShadowIndex);
            Assert.Equal(-1, entityType.GetProperty("Name").ShadowIndex);

            Assert.Equal(2, entityType.ShadowPropertyCount);
        }

        [Fact]
        public void Indexes_are_rebuilt_when_more_properties_added_or_relevant_state_changes()
        {
            var entityType = new EntityType(typeof(FullNotificationEntity), new Model());

            var nameProperty = entityType.GetOrAddProperty("Name", typeof(string));
            entityType.GetOrAddProperty("Id", typeof(int), shadowProperty: true).IsConcurrencyToken = true;

            Assert.Equal(0, entityType.GetProperty("Id").Index);
            Assert.Equal(1, entityType.GetProperty("Name").Index);

            Assert.Equal(0, entityType.GetProperty("Id").ShadowIndex);
            Assert.Equal(-1, entityType.GetProperty("Name").ShadowIndex);

            Assert.Equal(0, entityType.GetProperty("Id").OriginalValueIndex);
            Assert.Equal(-1, entityType.GetProperty("Name").OriginalValueIndex);

            Assert.Equal(1, entityType.ShadowPropertyCount);
            Assert.Equal(1, entityType.OriginalValueCount);

            var gameProperty = entityType.GetOrAddProperty("Game", typeof(int), shadowProperty: true);
            gameProperty.IsConcurrencyToken = true;

            var maneProperty = entityType.GetOrAddProperty("Mane", typeof(int), shadowProperty: true);
            maneProperty.IsConcurrencyToken = true;

            Assert.Equal(0, entityType.GetProperty("Game").Index);
            Assert.Equal(1, entityType.GetProperty("Id").Index);
            Assert.Equal(2, entityType.GetProperty("Mane").Index);
            Assert.Equal(3, entityType.GetProperty("Name").Index);

            Assert.Equal(0, entityType.GetProperty("Game").ShadowIndex);
            Assert.Equal(1, entityType.GetProperty("Id").ShadowIndex);
            Assert.Equal(2, entityType.GetProperty("Mane").ShadowIndex);
            Assert.Equal(-1, entityType.GetProperty("Name").ShadowIndex);

            Assert.Equal(0, entityType.GetProperty("Game").OriginalValueIndex);
            Assert.Equal(1, entityType.GetProperty("Id").OriginalValueIndex);
            Assert.Equal(2, entityType.GetProperty("Mane").OriginalValueIndex);
            Assert.Equal(-1, entityType.GetProperty("Name").OriginalValueIndex);

            Assert.Equal(3, entityType.ShadowPropertyCount);
            Assert.Equal(3, entityType.OriginalValueCount);

            gameProperty.IsConcurrencyToken = false;
            nameProperty.IsConcurrencyToken = true;

            Assert.Equal(0, entityType.GetProperty("Game").Index);
            Assert.Equal(1, entityType.GetProperty("Id").Index);
            Assert.Equal(2, entityType.GetProperty("Mane").Index);
            Assert.Equal(3, entityType.GetProperty("Name").Index);

            Assert.Equal(0, entityType.GetProperty("Game").ShadowIndex);
            Assert.Equal(1, entityType.GetProperty("Id").ShadowIndex);
            Assert.Equal(2, entityType.GetProperty("Mane").ShadowIndex);
            Assert.Equal(-1, entityType.GetProperty("Name").ShadowIndex);

            Assert.Equal(-1, entityType.GetProperty("Game").OriginalValueIndex);
            Assert.Equal(0, entityType.GetProperty("Id").OriginalValueIndex);
            Assert.Equal(1, entityType.GetProperty("Mane").OriginalValueIndex);
            Assert.Equal(2, entityType.GetProperty("Name").OriginalValueIndex);

            Assert.Equal(3, entityType.ShadowPropertyCount);
            Assert.Equal(3, entityType.OriginalValueCount);

            gameProperty.IsShadowProperty = false;
            nameProperty.IsShadowProperty = true;

            Assert.Equal(0, entityType.GetProperty("Game").Index);
            Assert.Equal(1, entityType.GetProperty("Id").Index);
            Assert.Equal(2, entityType.GetProperty("Mane").Index);
            Assert.Equal(3, entityType.GetProperty("Name").Index);

            Assert.Equal(-1, entityType.GetProperty("Game").ShadowIndex);
            Assert.Equal(0, entityType.GetProperty("Id").ShadowIndex);
            Assert.Equal(1, entityType.GetProperty("Mane").ShadowIndex);
            Assert.Equal(2, entityType.GetProperty("Name").ShadowIndex);

            Assert.Equal(-1, entityType.GetProperty("Game").OriginalValueIndex);
            Assert.Equal(0, entityType.GetProperty("Id").OriginalValueIndex);
            Assert.Equal(1, entityType.GetProperty("Mane").OriginalValueIndex);
            Assert.Equal(2, entityType.GetProperty("Name").OriginalValueIndex);

            Assert.Equal(3, entityType.ShadowPropertyCount);
            Assert.Equal(3, entityType.OriginalValueCount);
        }

        [Fact]
        public void Lazy_original_values_are_used_for_full_notification_and_shadow_enties()
        {
            Assert.True(new EntityType(typeof(FullNotificationEntity), new Model()).UseLazyOriginalValues);
        }

        [Fact]
        public void Lazy_original_values_are_used_for_shadow_enties()
        {
            Assert.True(new EntityType("Z'ha'dum", new Model()).UseLazyOriginalValues);
        }

        [Fact]
        public void Eager_original_values_are_used_for_enties_that_only_implement_INotifyPropertyChanged()
        {
            Assert.False(new EntityType(typeof(ChangedOnlyEntity), new Model()).UseLazyOriginalValues);
        }

        [Fact]
        public void Eager_original_values_are_used_for_enties_that_do_no_notification()
        {
            Assert.False(new EntityType(typeof(Customer), new Model()).UseLazyOriginalValues);
        }

        [Fact]
        public void Lazy_original_values_can_be_switched_off()
        {
            Assert.False(new EntityType(typeof(FullNotificationEntity), new Model()) { UseLazyOriginalValues = false }.UseLazyOriginalValues);
        }

        [Fact]
        public void Lazy_original_values_can_be_switched_on_but_only_if_entity_does_not_require_eager_values()
        {
            var entityType = new EntityType(typeof(FullNotificationEntity), new Model()) { UseLazyOriginalValues = false };
            entityType.UseLazyOriginalValues = true;
            Assert.True(entityType.UseLazyOriginalValues);

            Assert.Equal(
                Strings.FormatEagerOriginalValuesRequired(typeof(ChangedOnlyEntity).FullName),
                Assert.Throws<InvalidOperationException>(() => new EntityType(typeof(ChangedOnlyEntity), new Model()) { UseLazyOriginalValues = true }).Message);
        }

        [Fact]
        public void All_properties_have_original_value_indexes_when_using_eager_original_values()
        {
            var entityType = new EntityType(typeof(FullNotificationEntity), new Model()) { UseLazyOriginalValues = false };

            entityType.GetOrAddProperty("Name", typeof(string));
            entityType.GetOrAddProperty("Id", typeof(int));

            Assert.Equal(0, entityType.GetProperty("Id").OriginalValueIndex);
            Assert.Equal(1, entityType.GetProperty("Name").OriginalValueIndex);

            Assert.Equal(2, entityType.OriginalValueCount);
        }

        [Fact]
        public void Only_required_properties_have_original_value_indexes_when_using_lazy_original_values()
        {
            var entityType = new EntityType(typeof(FullNotificationEntity), new Model());

            entityType.GetOrAddProperty("Name", typeof(string)).IsConcurrencyToken = true;
            entityType.GetOrAddProperty("Id", typeof(int));

            Assert.Equal(-1, entityType.GetProperty("Id").OriginalValueIndex);
            Assert.Equal(0, entityType.GetProperty("Name").OriginalValueIndex);

            Assert.Equal(1, entityType.OriginalValueCount);
        }

        [Fact]
        public void FK_properties_are_marked_as_requiring_original_values()
        {
            var entityType = new EntityType(typeof(FullNotificationEntity), new Model());
            entityType.GetOrSetPrimaryKey(entityType.GetOrAddProperty("Id", typeof(int)));

            Assert.Equal(-1, entityType.GetProperty("Id").OriginalValueIndex);

            entityType.GetOrAddForeignKey(new[] { entityType.GetOrAddProperty("Id", typeof(int)) }, entityType.GetPrimaryKey());

            Assert.Equal(0, entityType.GetProperty("Id").OriginalValueIndex);
        }

        private class Customer
        {
            public static readonly PropertyInfo IdProperty = typeof(Customer).GetProperty("Id");
            public static readonly PropertyInfo NameProperty = typeof(Customer).GetProperty("Name");

            public int Id { get; set; }
            public Guid Unique { get; set; }
            public string Name { get; set; }
            public string Mane { get; set; }
            public ICollection<Order> Orders { get; set; }

            public IEnumerable<Order> EnumerableOrders { get; set; }
            public Order NotCollectionOrders { get; set; }
            public IEnumerable<SpecialOrder> DerivedOrders { get; set; }
        }

        private class SpecialCustomer : Customer
        {
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
            public SpecialCustomer DerivedCustomer { get; set; }
        }

        private class SpecialOrder : Order
        {
        }

        private class FullNotificationEntity : INotifyPropertyChanging, INotifyPropertyChanged
        {
            private int _id;
            private string _name;
            private int _game;

            public int Id
            {
                get { return _id; }
                set
                {
                    if (_id != value)
                    {
                        NotifyChanging();
                        _id = value;
                        NotifyChanged();
                    }
                }
            }

            public string Name
            {
                get { return _name; }
                set
                {
                    if (_name != value)
                    {
                        NotifyChanging();
                        _name = value;
                        NotifyChanged();
                    }
                }
            }

            public int Game
            {
                get { return _game; }
                set
                {
                    if (_game != value)
                    {
                        NotifyChanging();
                        _game = value;
                        NotifyChanged();
                    }
                }
            }

            public event PropertyChangingEventHandler PropertyChanging;
            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyChanged([CallerMemberName] String propertyName = "")
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            private void NotifyChanging([CallerMemberName] String propertyName = "")
            {
                if (PropertyChanging != null)
                {
                    PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
                }
            }
        }

        private class ChangedOnlyEntity : INotifyPropertyChanged
        {
            private int _id;
            private string _name;

            public int Id
            {
                get { return _id; }
                set
                {
                    if (_id != value)
                    {
                        _id = value;
                        NotifyChanged();
                    }
                }
            }

            public string Name
            {
                get { return _name; }
                set
                {
                    if (_name != value)
                    {
                        _name = value;
                        NotifyChanged();
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyChanged([CallerMemberName] String propertyName = "")
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }
    }
}
