using Microsoft.VisualStudio.TestTools.UnitTesting;
using Outrage.Entities.Tests.Entities;
using System;
using System.Linq;

namespace Outrage.Entities.Tests
{
    [TestClass]
    public class EntityTests
    {
        [TestMethod]
        public void Capacity()
        {
            var entities = new EntitySet(1000, 1000);
            Assert.AreEqual(1000 * 1000, entities.Capacity);
        }

        [TestMethod]
        public void EmptyGet()
        {
            var entities = new EntitySet(1000, 1000);

            Assert.ThrowsException<EntityUndefinedException>(() =>
            {
                entities.Get<Strength>(0);
            });
        }

        [TestMethod]
        public void GetFirstEntity()
        {
            var entities = new EntitySet(1000, 1000);
            Assert.AreEqual(0, entities.ReserveEntityId());
        }

        [TestMethod]
        public void DefaultOnGet()
        {
            var entities = new EntitySet(1000, 1000);
            var entityId = entities.ReserveEntityId();
            var strength = entities.Get<Strength>(entityId);

            Assert.AreEqual(default(int), strength.Value);
        }

        [TestMethod]
        public void UndefineEntity()
        {
            var entities = new EntitySet(1000, 1000);
            var entityId = entities.ReserveEntityId();
            var strength = entities.Get<Strength>(entityId);
            entities.Clear(entityId);

            Assert.ThrowsException<EntityUndefinedException>(() =>
            {
                strength = entities.Get<Strength>(entityId);
            });

            Assert.AreEqual(entityId, entities.ReserveEntityId());
        }

        [TestMethod]
        public void Has()
        {
            var entities = new EntitySet(1000, 1000);
            var entityId = entities.ReserveEntityId();
            entities.Mutate(entityId, (long _, ref Strength s) => s.Value = 10);
            Assert.IsTrue(entities.Has<Strength>(entityId));
            Assert.IsFalse(entities.Has<Health>(entityId));
        }

        [TestMethod]
        public void Value()
        {
            var entities = new EntitySet(1000, 1000);
            var entityId = entities.ReserveEntityId();
            entities.Mutate(entityId, (long _, ref Strength s) => s.Value = 10);
            entities.Mutate(entityId, (long _, ref Health s) => s.Value = 100);
            Assert.AreEqual(100, entities.Get<Health>(entityId).Value);
            Assert.AreEqual(10, entities.Get<Strength>(entityId).Value);
            Assert.IsTrue(entities.Has<Health>(entityId));
            Assert.IsTrue(entities.Has<Strength>(entityId));
        }

        [TestMethod]
        public void Query()
        {
            var entities = new EntitySet(1000, 1000);

            var entityId1 = entities.ReserveEntityId();
            entities.Mutate(entityId1, (long _, ref Strength s) => s.Value = 10);

            var entityId2 = entities.ReserveEntityId();
            entities.Mutate(entityId2, (long _, ref Health s) => s.Value = 100);

            Assert.IsFalse(entities.Has<Health>(entityId1));
            Assert.IsTrue(entities.Has<Strength>(entityId1));
            Assert.IsTrue(entities.Has<Health>(entityId2));
            Assert.IsFalse(entities.Has<Strength>(entityId2));

            Assert.IsTrue(Enumerable.SequenceEqual(entities.QueryEntitiesWith<Strength>(), new long[] { entityId1 }));
            Assert.IsTrue(Enumerable.SequenceEqual(entities.QueryEntitiesWith<Health>(), new long[] { entityId2 }));
        }

        [TestMethod]
        public void QueryEmpty()
        {
            var entities = new EntitySet(1000, 1000);
            Assert.AreEqual(0, entities.QueryEntitiesWith<Health>().Count());
        }

        [TestMethod]
        public void Pressure()
        {
            var entities = new EntitySet(1000, 1000);
            var entityId1 = entities.ReserveEntityId();
            entities.Mutate(entityId1, (long _, ref Strength s) => s.Value = 10);

            int i;
            for (i = 0; i < 1000; i++)
            {
                var entityId = entities.ReserveEntityId();
                entities.Mutate(entityId, (long _, ref Strength s) => s.Value = i);
            }

            entities.Mutate(entityId1, (long _, ref Health s) => s.Value = 10);

            for (i = 0; i < 1000; i++)
            {
                var entityId = entities.ReserveEntityId();
                entities.Mutate(entityId, (long _, ref Health s) => s.Value = i);
            }
        }

        [TestMethod]
        public void MutateAll()
        {
            var entities = new EntitySet(1000, 1000);

            int i;
            for (i = 0; i < 1000; i++)
            {
                var entityId = entities.ReserveEntityId();
                entities.Mutate(entityId, (long _, ref Strength s) => s.Value = i);
            }

            entities.MutateAllSet((long _, ref Strength s) => s.Value = s.Value + 1);
            entities.MutateAllSet((long _, ref Strength s) => s.Value = s.Value + 1);
            entities.MutateAllSet((long _, ref Strength s) => s.Value = s.Value + 1);
            entities.MutateAllSet((long _, ref Strength s) => s.Value = s.Value + 1);
            entities.MutateAllSet((long _, ref Strength s) => s.Value = s.Value + 1);

            var check = entities.Get<Strength>(0);
            Assert.AreEqual(5, check.Value);
        }

        [TestMethod]
        public void PressureCheck()
        {
            var entities = new EntitySet(1000, 1000);
            var entityId1 = entities.ReserveEntityId();
            entities.Mutate(entityId1, (long _, ref Strength s) => s.Value = 10);

            int i;
            for (i = 0; i < 1000; i++)
            {
                var entityId = entities.ReserveEntityId();
                entities.Mutate(entityId, (long _, ref Strength s) => s.Value = i);
            }

            var tam1 = GC.GetTotalAllocatedBytes();
            GC.Collect();
            Assert.AreEqual(tam1, GC.GetTotalAllocatedBytes());

            entities.Mutate(entityId1, (long _, ref Health s) => s.Value = 10);

            for (i = 0; i < 1000; i++)
            {
                var entityId = entities.ReserveEntityId();
                entities.Mutate(entityId, (long _, ref Health s) => s.Value = i);
            }

            var tam2 = GC.GetTotalAllocatedBytes();
            GC.Collect();
            Assert.AreEqual(tam2, GC.GetTotalAllocatedBytes());
        }
    }
}