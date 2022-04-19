# Outrage.Entities

A .Net Core entity layer which stores data about entities, contained in structs.  Any entity may have one or more structs that describe aspects of an entities definition set.

Memory for the EntitySet is allocated as a block, and is not destroyed as entities are recycled, leading to little or no Garbage Collection cycles during regular operation.

An entities properties are captured as a series of struct's containing fundamental types.  For example:

```c# 
public struct Health {
    public int Value;
    public int Stamina; 
}
```

Note that can you use non-fundamental types for these properties, such as strings, but you need to be careful not to dispose or replace them regularly as their values will need to be garbage collected.  You can get around this by using a fixed length Span<char> instead.

## EntitySet

An entity set is a collection that step allocated to create a set of entities.

```c#
var entitySet = new Outrage.Entities.EntitySet( layerCapacity = 1000, capacityStep = 1000);
```

An entity set in the above configuration can manage 1,000,000 (1,000 x 1,000) entities.

Entities are allocated in steps of 1,000 at a time when entity properties are added to entities.

```c#
var entitySet = new Outrage.Entities.EntitySet();

// Get an id (a long int) for an entity.  It will be 0.
long entityId = entitySet.ReserveEntityId();

// Mutate the health values for the entity by its id
entitySet.Mutate(entityId, (long id, ref Health health) => { 
    health.Value = 100; 
    health.Stamina = 100;
});

// Get the health related properties back for the numbered entity
Health health = entitySet.Get<Health>(entityId);

Console.Writeline(health.Value); // 100
Console.Writeline(health.Stamina); // 100

// Decrease the entities stamina
entitySet.Mutate(entityId, (long id, ref Health health) => {
    health.Stamina -= 10;
});

Console.Writeline(health.Value); // 100
Console.Writeline(health.Stamina); // 90

// Clear the entity and all it's set properties
entitySet.Clear(entityId);
```

* ReserveEntityId - Get the next Id, you must get new id's this way.  If an entity is cleared, its it will be released, and reissued to you later.  Be aware that the properties of an entity after it is cleared are left in tact, on first receiving an entity you should properly initialize all the property values you will use by immediately mutating it.
* Mutate - Use an action UpdateRef(long entityId, ref TEntity entity) to mutate the values of an entity property.  See the health example above.
* Get - Return the values for an entity property.
* Clear - Mark an entity as no longer used, release its id to the pool.  Its properties are not reset, nor is the memory deallocated.  You must reinitialize properties on first use.

After you Clear an entity id, ReserveEntityId will return one of the last cleared entity ids unless none are available.  If none are available, a new id will be returned.  If you consume capacityStep (default 1000) ids, a new block of a 1000 entities will be allocated automatically.

# Queries and Property Type mutations

Consider the following in which we create two entities and set their position properties;

```c#
struct Position {
    public int X;
    public int Y;
}

var entities = new EntitySet();

// Create the first entity
var id1 = entities.ReserveEntityId();
entities.Mutate(id1, (long id, ref Position position) => {
    position.X = 0;
    position.Y = 0;
});

// Create the second entity
var id2 = entities.ReserveEntityId();
entities.Mutate(id1, (long id, ref Position position) => {
    position.X = 2;
    position.Y = 0;
});

// Move all entities with a position forward by 1 ( y+= 1 ) so long as they have not passed 10
entities.MutateAllSet((long id, ref Position position) => {
    if (position.Y < 10) position.Y += 1;
});
```

* MutateAllSet - Mutate the properties of all entities that have a certain setting.  In the above, mutate all entities that have a Position

It is faster in general to mutate entities as a group, rather than individually.  In some scenarios where you are managing an individual entity (the player, for example) you should instead manage the entity by its id.

# Marker Properties

It is possible to mark entities using empty structs and then use the properties to make dependent changes, such as the following which will move all NPC characters forward by 1.

```c#

// A marker struct to identify an entity as a non player character
struct NPCMarker { }

struct Position {
    public int X;
    public int Y;
}

var entities = new Outrage.Entities.EntitySet();
var entityId = entities.ReserveEntityId();

// Applies the marker, no properties to change so the mutator action isnt passed
entities.Mutate<NPCMarker>(entityId);
entities.Mutate<Position>(entityId, (long id, ref Position position) {
    position.X = 0;
    position.Y = 0;
});

// Move all NPC's forward by one
entities.MutateAllSetWith<Position, NPCMarker>((long id, ref Position position) => {
    position.Y += 1;
});
```

* Mutate - You can call mutate without passing an update action.
* MutateAllSetWith - Mutate all entities in one property where another property is set.


# Queries

You can also query for entities with a property set and get back a list:
```c#
var entities = new Outrage.Entities.EntitySet();
var entityId = entities.ReserveEntityId();
entities.Mutate<NPCMarker>(entityId);

// Get a list of all entities with NPCMarker
var npcEntities = entities.QueryEntitiesWith<NPCMarker>();

// long[] { 0 }

entities.MutateSet(npcEntities, (long id, ref Position position) => {
    position.Y += 1;
});
```

* QueryEntitiesWith - Get a list of all entities that have a specific property (or property marker).
* MutateSet - Apply the same mutation to a list of entities given by an enumerable of ids.

# Clearing Entity Properties

You can also unset a property on an entity, consider the following:

```c#
var entities = new Outrage.Entities.EntitySet();
var entityId = entities.ReserveEntityId();
entities.Mutate<NPCMarker>(entityId);

// Now unmark the entity
entites.Clear<NPCMarker>(entityId);

```

* Clear with property - Unset the property on an entity, to remove a property set or to remove a marker property off the entity

# Tests

For additional information, please see the test suite at /src/Outrage.Entities.Tests.