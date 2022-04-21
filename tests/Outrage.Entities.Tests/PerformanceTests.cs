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

    private void BuildEntities(EntitySet entitySet)
    {
        for (var i = 0; i < 100000; i++)
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
        Random random = new Random();
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
        Random random = new Random();
        var entitySet = new EntitySet();

        BuildEntities(entitySet);

        var cycles = 1000;
        Console.WriteLine($"Mutating {entitySet.Count} entitys for {cycles} cycles...");
        var startMutating = DateTimeOffset.UtcNow;
        while (cycles-- > 0)
        {
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

        }
        var millisecondsTaken = (DateTimeOffset.UtcNow - startMutating).TotalMilliseconds;
        Console.WriteLine($"Done in {millisecondsTaken} milliseconds.\n");

        var perFrameSpeed = millisecondsTaken / 1000;
        var frameRate = 1000 / perFrameSpeed;

        Console.WriteLine($"Effective frame rate: {frameRate} per second.\n");
    }

    [TestMethod]
    public void BlockMutationSequential()
    {
        Random random = new Random();
        var entitySet = new EntitySet();

        BuildEntities(entitySet);

        var cycles = 1000;
        Console.WriteLine($"Mutating {entitySet.Count} entitys for {cycles} cycles...");
        var startMutating = DateTimeOffset.UtcNow;
        while (cycles-- > 0)
        {
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

        }
        var millisecondsTaken = (DateTimeOffset.UtcNow - startMutating).TotalMilliseconds;
        Console.WriteLine($"Done in {millisecondsTaken} milliseconds.\n");

        var perFrameSpeed = millisecondsTaken / 1000;
        var frameRate = 1000 / perFrameSpeed;

        Console.WriteLine($"Effective frame rate: {frameRate} per second.\n");
    }

    [TestMethod]
    public void BlockMutationWithCreationParallel()
    {
        Random random = new Random();
        var entitySet = new EntitySet();

        BuildEntities(entitySet);

        var cycles = 1000;
        Console.WriteLine($"Mutating {entitySet.Count} entitys for {cycles} cycles, with recreation...");
        var startMutating = DateTimeOffset.UtcNow;
        while (cycles-- > 0)
        {
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

            // Drop one thousand of them;
            var thousandEntities = entitySet.QueryEntitiesWith<Position>().Take(1000);
            entitySet.Clear(thousandEntities, true);

            // Reinstate and reinitialize one thousand of them
            var newEntityIds = entitySet.ReserveEntityIds(1000);
            entitySet.Mutate<Position>(newEntityIds, (long id, ref Position position) =>
            {
                position.X = 0;
                position.Y = 0;
            }, true);
        }

        var millisecondsTaken = (DateTimeOffset.UtcNow - startMutating).TotalMilliseconds;
        Console.WriteLine($"Done in {millisecondsTaken} milliseconds.");

        var perFrameSpeed = millisecondsTaken / 1000;
        var frameRate = 1000 / perFrameSpeed;

        Console.WriteLine($"Effective frame rate: {frameRate} per second.\n");
    }

    [TestMethod]
    public void BlockMutationWithCreationSequential()
    {
        Random random = new Random();
        var entitySet = new EntitySet();

        BuildEntities(entitySet);

        var cycles = 1000;
        Console.WriteLine($"Mutating {entitySet.Count} entitys for {cycles} cycles, with recreation...");
        var startMutating = DateTimeOffset.UtcNow;
        while (cycles-- > 0)
        {
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

            // Drop one thousand of them;
            var thousandEntities = entitySet.QueryEntitiesWith<Position>().Take(1000);
            entitySet.Clear(thousandEntities, false);

            // Reinstate and reinitialize one thousand of them
            var newEntityIds = entitySet.ReserveEntityIds(1000);
            entitySet.Mutate<Position>(newEntityIds, (long id, ref Position position) =>
            {
                position.X = 0;
                position.Y = 0;
            }, false);
        }

        var millisecondsTaken = (DateTimeOffset.UtcNow - startMutating).TotalMilliseconds;
        Console.WriteLine($"Done in {millisecondsTaken} milliseconds.");

        var perFrameSpeed = millisecondsTaken / 1000;
        var frameRate = 1000 / perFrameSpeed;

        Console.WriteLine($"Effective frame rate: {frameRate} per second.\n");
    }

    [TestMethod]
    public void CreateDropCreateSequential()
    {
        Random random = new Random();
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
        
        var thousandEntities = entitySet.QueryEntitiesWith<Position>().Take(1000);
        startMutating = DateTimeOffset.UtcNow;

        // Drop one thousand of them;
        entitySet.Clear(thousandEntities, false);

        millisecondsTaken = (DateTimeOffset.UtcNow - startMutating).TotalMilliseconds;
        Console.WriteLine($"Done clearing in {millisecondsTaken} milliseconds.");
        startMutating = DateTimeOffset.UtcNow;

        // Reinstate and reinitialize one thousand of them
        var newEntityIds = entitySet.ReserveEntityIds(1000);
        entitySet.Mutate<Position>(newEntityIds, (long id, ref Position position) =>
        {
            position.X = 0;
            position.Y = 0;
        }, false);

        millisecondsTaken = (DateTimeOffset.UtcNow - startMutating).TotalMilliseconds;
        Console.WriteLine($"Done recreating in {millisecondsTaken} milliseconds.");

        var perFrameSpeed = millisecondsTaken / 1000;
        var frameRate = 1000 / perFrameSpeed;

        Console.WriteLine($"Effective frame rate: {frameRate} per second.\n");
    }
    
    [TestMethod]
    public void CreateDropCreateParallel()
    {
        Random random = new Random();
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
        
        var thousandEntities = entitySet.QueryEntitiesWith<Position>().Take(1000);
        startMutating = DateTimeOffset.UtcNow;

        // Drop one thousand of them;
        entitySet.Clear(thousandEntities, true);

        millisecondsTaken = (DateTimeOffset.UtcNow - startMutating).TotalMilliseconds;
        Console.WriteLine($"Done clearing in {millisecondsTaken} milliseconds.");
        startMutating = DateTimeOffset.UtcNow;

        // Reinstate and reinitialize one thousand of them
        var newEntityIds = entitySet.ReserveEntityIds(1000);
        entitySet.Mutate<Position>(newEntityIds, (long id, ref Position position) =>
        {
            position.X = 0;
            position.Y = 0;
        }, true);

        millisecondsTaken = (DateTimeOffset.UtcNow - startMutating).TotalMilliseconds;
        Console.WriteLine($"Done recreating in {millisecondsTaken} milliseconds.");

        var perFrameSpeed = millisecondsTaken / 1000;
        var frameRate = 1000 / perFrameSpeed;

        Console.WriteLine($"Effective frame rate: {frameRate} per second.\n");
    }
}
