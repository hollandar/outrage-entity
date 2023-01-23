using Microsoft.VisualStudio.TestTools.UnitTesting;
using Outrage.Entities.Tests.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Entities.Tests;

[TestClass]
public class PerformanceTests
{
    const int CYCLES = 10000;
    const int ENTITIES = 100000;
    const int RECREATE_SIZE = 1000;
    [ThreadStatic] Random random = new Random();

    private void BuildEntities(EntitySet entitySet)
    {
        for (var i = 0; i < ENTITIES; i++)
        {
            var entityId = entitySet.ReserveEntityId();
            entitySet.Mutate<Position>(entityId, (long id, ref Position position) =>
            {
                position.X = 0;
                position.Y = 0;
            });
        }
    }

    [TestMethod]
    public void Creation()
    {
        var entitySet = new EntitySet();

        {
            Console.WriteLine("Building...");
            var startBuilding = DateTimeOffset.UtcNow;

            BuildEntities(entitySet);

            Console.WriteLine($"Done in {(DateTimeOffset.UtcNow - startBuilding).TotalMilliseconds} milliseconds.");
        }
    }

    [TestMethod]
    public void BlockMutationParallel()
    {
        var entitySet = new EntitySet();


        BuildEntities(entitySet);

        var cycles = CYCLES;
        Console.WriteLine($"Mutating {entitySet.Count} entitys for {cycles} cycles...");
        var startMutating = DateTimeOffset.UtcNow;
        var moves = new List<int>(Enumerable.Range(0, ENTITIES).Select(r => random.Next(0, 4)));
        while (cycles-- > 0)
        {
            entitySet.MutateAllSet<Position>((long id, ref Position position) =>
            {
                var moveDirection = moves[(int)id];
                switch (moveDirection)
                {
                    case 0: position.Y += 1; break;
                    case 1: position.X += 1; break;
                    case 2: position.Y -= 1; break;
                    case 4: position.X -= 1; break;
                }
            }, true);

        }
        var millisecondsTaken = (DateTimeOffset.UtcNow - startMutating).TotalMilliseconds;
        Console.WriteLine($"Done in {millisecondsTaken} milliseconds.\n");

        var perFrameSpeed = millisecondsTaken / CYCLES;
        var frameRate = 1000 / perFrameSpeed;

        Console.WriteLine($"Effective frame rate: {frameRate} per second.\n");
    }

    [TestMethod]
    public void BlockMutationSequential()
    {
        var entitySet = new EntitySet();
        var moves = new List<int>(Enumerable.Range(0, ENTITIES).Select(r => random.Next(0, 4)));

        BuildEntities(entitySet);

        var cycles = CYCLES;
        Console.WriteLine($"Mutating {entitySet.Count} entitys for {cycles} cycles...");
        var startMutating = DateTimeOffset.UtcNow;
        while (cycles-- > 0)
        {
            entitySet.MutateAllSet<Position>((long id, ref Position position) =>
            {
                var moveDirection = (cycles * id) % moves.Count;
                switch (moveDirection)
                {
                    case 0: position.Y += 1; break;
                    case 1: position.X += 1; break;
                    case 2: position.Y -= 1; break;
                    case 4: position.X -= 1; break;
                }
            }, false);

        }
        var millisecondsTaken = (DateTimeOffset.UtcNow - startMutating).TotalMilliseconds;
        Console.WriteLine($"Done in {millisecondsTaken} milliseconds.\n");

        var perFrameSpeed = millisecondsTaken / CYCLES;
        var frameRate = 1000 / perFrameSpeed;

        Console.WriteLine($"Effective frame rate: {frameRate} per second.\n");
    }

    [TestMethod]
    public void BlockMutationWithCreationParallel()
    {
        var entitySet = new EntitySet();
        var moves = new List<int>(Enumerable.Range(0, ENTITIES).Select(r => random.Next(0, 4)));

        BuildEntities(entitySet);

        var cycles = CYCLES;
        Console.WriteLine($"Mutating {entitySet.Count} entitys for {cycles} cycles, with recreation...");
        var startMutating = DateTimeOffset.UtcNow;
        while (cycles-- > 0)
        {
            // Mutate all the entities
            entitySet.MutateAllSet<Position>((long id, ref Position position) =>
            {
                var moveDirection = (cycles * id) % moves.Count;
                switch (moveDirection)
                {
                    case 0: position.Y += 1; break;
                    case 1: position.X += 1; break;
                    case 2: position.Y -= 1; break;
                    case 4: position.X -= 1; break;
                }
            }, true);

            // Drop one thousand of them;
            var thousandEntities = entitySet.QueryEntitiesWith<Position>().Take(RECREATE_SIZE);
            entitySet.Clear(thousandEntities, true);

            // Reinstate and reinitialize one thousand of them
            var newEntityIds = entitySet.ReserveEntityIds(RECREATE_SIZE);
            entitySet.Mutate<Position>(newEntityIds, (long id, ref Position position) =>
            {
                position.X = 0;
                position.Y = 0;
            }, true);
        }

        var millisecondsTaken = (DateTimeOffset.UtcNow - startMutating).TotalMilliseconds;
        Console.WriteLine($"Done in {millisecondsTaken} milliseconds.");

        var perFrameSpeed = millisecondsTaken / CYCLES;
        var frameRate = 1000 / perFrameSpeed;

        Console.WriteLine($"Effective frame rate: {frameRate} per second.\n");
    }

    [TestMethod]
    public void BlockMutationWithCreationSequential()
    {
        var entitySet = new EntitySet();
        var moves = new List<int>(Enumerable.Range(0, ENTITIES).Select(r => random.Next(0, 4)));

        BuildEntities(entitySet);

        var cycles = CYCLES;
        Console.WriteLine($"Mutating {entitySet.Count} entitys for {cycles} cycles, with recreation...");
        var startMutating = DateTimeOffset.UtcNow;
        while (cycles-- > 0)
        {
            // Mutate all the entities
            entitySet.MutateAllSet<Position>((long id, ref Position position) =>
            {
                var moveDirection = (cycles * id) % moves.Count;
                switch (moveDirection)
                {
                    case 0: position.Y += 1; break;
                    case 1: position.X += 1; break;
                    case 2: position.Y -= 1; break;
                    case 4: position.X -= 1; break;
                }
            }, false);

            // Drop one thousand of them;
            var thousandEntities = entitySet.QueryEntitiesWith<Position>().Take(RECREATE_SIZE);
            entitySet.Clear(thousandEntities, false);

            // Reinstate and reinitialize one thousand of them
            var newEntityIds = entitySet.ReserveEntityIds(RECREATE_SIZE);
            entitySet.Mutate<Position>(newEntityIds, (long id, ref Position position) =>
            {
                position.X = 0;
                position.Y = 0;
            }, false);
        }

        var millisecondsTaken = (DateTimeOffset.UtcNow - startMutating).TotalMilliseconds;
        Console.WriteLine($"Done in {millisecondsTaken} milliseconds.");

        var perFrameSpeed = millisecondsTaken / CYCLES;
        var frameRate = 1000 / perFrameSpeed;

        Console.WriteLine($"Effective frame rate: {frameRate} per second.\n");
    }

    [TestMethod]
    public void CreateDropCreateSequential()
    {
        var entitySet = new EntitySet();

        BuildEntities(entitySet);

        Console.WriteLine($"Mutating {entitySet.Count} entitys, with recreation...");
        var startMutating = DateTimeOffset.UtcNow;

        // Mutate all the entities
        entitySet.MutateAllSet<Position>((long id, ref Position position) =>
        {
            var moveDirection = random.Next(0, 4);
            switch (moveDirection)
            {
                case 0: position.Y += 1; break;
                case 1: position.X += 1; break;
                case 2: position.Y -= 1; break;
                case 4: position.X -= 1; break;
            }
        }, false);

        var millisecondsTaken = (DateTimeOffset.UtcNow - startMutating).TotalMilliseconds;
        Console.WriteLine($"Done mutating in {millisecondsTaken} milliseconds.");

        var thousandEntities = entitySet.QueryEntitiesWith<Position>().Take(ENTITIES);
        startMutating = DateTimeOffset.UtcNow;

        // Drop one thousand of them;
        entitySet.Clear(thousandEntities, false);

        millisecondsTaken = (DateTimeOffset.UtcNow - startMutating).TotalMilliseconds;
        Console.WriteLine($"Done clearing in {millisecondsTaken} milliseconds.");
        startMutating = DateTimeOffset.UtcNow;

        // Reinstate and reinitialize one thousand of them
        var newEntityIds = entitySet.ReserveEntityIds(ENTITIES);
        entitySet.Mutate<Position>(newEntityIds, (long id, ref Position position) =>
        {
            position.X = 0;
            position.Y = 0;
        }, false);

        millisecondsTaken = (DateTimeOffset.UtcNow - startMutating).TotalMilliseconds;
        Console.WriteLine($"Done recreating in {millisecondsTaken} milliseconds.");

        var perFrameSpeed = millisecondsTaken / ENTITIES;
        var frameRate = 1000 / perFrameSpeed;

        Console.WriteLine($"Effective frame rate: {frameRate} per second.\n");
    }

    [TestMethod]
    public void CreateDropCreateParallel()
    {
        var entitySet = new EntitySet();

        BuildEntities(entitySet);

        Console.WriteLine($"Mutating {entitySet.Count} entitys, with recreation...");
        var startMutating = DateTimeOffset.UtcNow;

        // Mutate all the entities
        entitySet.MutateAllSet<Position>((long id, ref Position position) =>
        {
            var moveDirection = random.Next(0, 4);
            switch (moveDirection)
            {
                case 0: position.Y += 1; break;
                case 1: position.X += 1; break;
                case 2: position.Y -= 1; break;
                case 4: position.X -= 1; break;
            }
        }, true);

        var millisecondsTaken = (DateTimeOffset.UtcNow - startMutating).TotalMilliseconds;
        Console.WriteLine($"Done mutating in {millisecondsTaken} milliseconds.");

        var thousandEntities = entitySet.QueryEntitiesWith<Position>().Take(ENTITIES);
        startMutating = DateTimeOffset.UtcNow;

        // Drop one thousand of them;
        entitySet.Clear(thousandEntities, true);

        millisecondsTaken = (DateTimeOffset.UtcNow - startMutating).TotalMilliseconds;
        Console.WriteLine($"Done clearing in {millisecondsTaken} milliseconds.");
        startMutating = DateTimeOffset.UtcNow;

        // Reinstate and reinitialize one thousand of them
        var newEntityIds = entitySet.ReserveEntityIds(ENTITIES);
        entitySet.Mutate<Position>(newEntityIds, (long id, ref Position position) =>
        {
            position.X = 0;
            position.Y = 0;
        }, true);

        millisecondsTaken = (DateTimeOffset.UtcNow - startMutating).TotalMilliseconds;
        Console.WriteLine($"Done recreating in {millisecondsTaken} milliseconds.");

        var perFrameSpeed = millisecondsTaken / ENTITIES;
        var frameRate = 1000 / perFrameSpeed;

        Console.WriteLine($"Effective frame rate: {frameRate} per second.\n");
    }
}
