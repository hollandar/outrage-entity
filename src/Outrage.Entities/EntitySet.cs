using System.Collections.Concurrent;

namespace Outrage.Entities
{
    public class EntitySet
    {
        private int capacityStep;
        private int layerCapacity;
        ConcurrentDictionary<Type, ILayer> layers;
        HashSet<long> clearedEntities;
        long lastEntityId = 0;

        /// <summary>
        /// Construct an entity set
        /// </summary>
        /// <param name="layerCapacity">Maximum number of layers in the entity set</param>
        /// <param name="capacityStep">Number of entities in each layer, and the increment of allocation as entities grow</param>
        public EntitySet(int layerCapacity = 2048, int capacityStep = 512)
        {
            this.layerCapacity = layerCapacity;
            this.capacityStep = capacityStep;
            this.layers = new ConcurrentDictionary<Type, ILayer>();
            this.clearedEntities = new HashSet<long>(capacityStep);
        }

        /// <summary>
        /// The total number of active entities in the entity set
        /// </summary>
        public long Count => lastEntityId - this.clearedEntities.Count;

        /// <summary>
        /// The total capacity of the entity set, capacity can not be adjusted at runtime
        /// </summary>
        public long Capacity => layerCapacity * capacityStep;

        /// <summary>
        /// Reserve an entity id, or reuse an existing entity that has been released (Clear)
        /// </summary>
        /// <returns>next entity id</returns>
        public long ReserveEntityId()
        {
            if (clearedEntities.Count == 0)
                return lastEntityId++;
            else
            {
                var result = clearedEntities.Last();
                this.clearedEntities.Remove(result);

                return result;
            }
        }

        /// <summary>
        /// Reserve a number of entity ids
        /// </summary>
        /// <param name="count">count of ids to reserve</param>
        /// <returns>a list of new ids</returns>
        public IEnumerable<long> ReserveEntityIds(int count)
        {
            HashSet<long> reservedIds = new HashSet<long>(count);
            while (this.clearedEntities.Count > 0 && reservedIds.Count < count)
            {
                var id = this.clearedEntities.First();
                reservedIds.Add(id);
                this.clearedEntities.Remove(id);
            }

            while (reservedIds.Count < count)
            {
                reservedIds.Add(lastEntityId++);
            }

            return reservedIds;
        }

        /// <summary>
        /// Get the property of an entity
        /// </summary>
        /// <typeparam name="TProperty">The struct that stores related properties of the entity</typeparam>
        /// <param name="entityId">entity id</param>
        /// <returns>the current value of the property</returns>
        /// <exception cref="EntityUndefinedException">The entity is unset</exception>
        /// <exception cref="Exception">Unexpected.</exception>
        public TProperty Get<TProperty>(long entityId) where TProperty : struct
        {
            if (entityId >= lastEntityId || this.clearedEntities.Contains(entityId))
                throw new EntityUndefinedException();

            Layer<TProperty>? typedLayer;
            if (layers.TryGetValue(typeof(TProperty), out ILayer? layer))
            {
                typedLayer = layer as Layer<TProperty>;
                if (typedLayer != null)
                {
                    return typedLayer.Get(entityId);
                }
                else throw new Exception("Get entities exception.");
            }
            else
            {
                layers[typeof(TProperty)] = typedLayer = new Layer<TProperty>(layerCapacity, capacityStep);
                return typedLayer.Get(entityId);
            }
        }

        /// <summary>
        /// Mutate the value of a property of the entity, and mark it as set if it isnt already
        /// </summary>
        /// <typeparam name="TProperty">The type of property being mutated.</typeparam>
        /// <param name="entityId">entity id</param>
        /// <param name="updateAction">action to perform the changes</param>
        /// <exception cref="Exception">Unexpected.</exception>
        public void Mutate<TProperty>(long entityId, UpdateRef<TProperty>? updateAction = null) where TProperty : struct
        {
            Layer<TProperty>? typedLayer;
            if (layers.TryGetValue(typeof(TProperty), out ILayer? layer))
            {
                typedLayer = layer as Layer<TProperty>;
                if (typedLayer != null)
                {
                    typedLayer.Update(entityId, updateAction);
                }
                else throw new Exception("Mutate entities exception.");
            }
            else
            {
                layers[typeof(TProperty)] = typedLayer = new Layer<TProperty>(layerCapacity, capacityStep);
                if (typedLayer != null)
                {
                    typedLayer.Update(entityId, updateAction);
                }
                else throw new Exception("Mutate entities exception.");
            }
        }

        /// <summary>
        /// Mutate all entities with a propery marked as set
        /// </summary>
        /// <typeparam name="TProperty">The property to mutate</typeparam>
        /// <param name="updateAction">Action to use for the mutation</param>
        /// <exception cref="Exception">Unexpected.</exception>
        public void MutateAllSet<TProperty>(UpdateRef<TProperty> updateAction, bool parallel = false) where TProperty : struct
        {
            Layer<TProperty>? typedLayer;
            if (layers.TryGetValue(typeof(TProperty), out ILayer? layer))
            {
                typedLayer = layer as Layer<TProperty>;
                if (typedLayer != null)
                {
                    if (parallel)
                        typedLayer.QuerySet().AsParallel().ForAll(entityId => typedLayer.Update(entityId, updateAction));
                    else
                        typedLayer.UpdateSet(updateAction);
                }
                else throw new Exception("MutateAll entities exception.");
            }
        }

        /// <summary>
        /// Mutate all entities that have a related property
        /// </summary>
        /// <typeparam name="TProperty">The property to mutate</typeparam>
        /// <typeparam name="TWithProperty">Mutate all entities that also have this property set</typeparam>
        /// <param name="updateAction">The action to perform the update</param>
        /// <exception cref="Exception">Unexpected.</exception>
        public void MutateAllSetWith<TProperty, TWithProperty>(UpdateRef<TProperty>? updateAction = null, bool parallel = false) where TProperty : struct where TWithProperty : struct
        {
            IEnumerable<long> setEntityIds = Enumerable.Empty<long>();
            if (layers.TryGetValue(typeof(TWithProperty), out ILayer? withLayer))
            {
                setEntityIds = withLayer.QuerySet();
            }

            Layer<TProperty>? typedLayer;
            if (layers.TryGetValue(typeof(TProperty), out ILayer? layer))
            {
                typedLayer = layer as Layer<TProperty>;
                if (typedLayer != null)
                {
                    if (parallel)
                    {
                        setEntityIds.AsParallel().ForAll(entityId =>
                        {
                            typedLayer.UpdateIfSet(entityId, updateAction);
                        });
                    }
                    else
                    {
                        foreach (var entityId in setEntityIds)
                        {
                            typedLayer.UpdateIfSet(entityId, updateAction);
                        }
                    }
                }
                else throw new Exception("MutateAll entities exception.");
            }
            else
            {
                layers[typeof(TProperty)] = typedLayer = new Layer<TProperty>(layerCapacity, capacityStep);
                if (typedLayer != null)
                {
                    if (parallel)
                    {
                        setEntityIds.AsParallel().ForAll(entityId =>
                        {
                            typedLayer.UpdateIfSet(entityId, updateAction);
                        });
                    }
                    else
                    {
                        foreach (var entityId in setEntityIds)
                        {
                            typedLayer.UpdateIfSet(entityId, updateAction);
                        }
                    }
                }
                else throw new Exception("MutateAll entities exception.");
            }
        }

        /// <summary>
        /// Mutate properties of a list of entities, but only where the property is already set.
        /// </summary>
        /// <typeparam name="TProperty">The property to mutate.</typeparam>
        /// <param name="entityIds">A list of entities to potentially mutate</param>
        /// <param name="updateAction">Action to perform on the entity property</param>
        /// <exception cref="Exception">Unexpected.</exception>
        public void MutateSet<TProperty>(IEnumerable<long> entityIds, UpdateRef<TProperty>? updateAction = null, bool parallel = false) where TProperty : struct
        {
            Layer<TProperty>? typedLayer;
            if (layers.TryGetValue(typeof(TProperty), out ILayer? layer))
            {
                typedLayer = layer as Layer<TProperty>;
                if (typedLayer != null)
                {
                    if (parallel)
                    {
                        entityIds.AsParallel().ForAll(entityId =>
                        {
                            typedLayer.UpdateIfSet(entityId, updateAction);
                        });
                    }
                    else
                    {
                        foreach (var entityId in entityIds)
                        {
                            typedLayer.UpdateIfSet(entityId, updateAction);
                        }
                    }
                }
                else throw new Exception("MutateSet entities exception.");
            }
            else
            {
                layers[typeof(TProperty)] = typedLayer = new Layer<TProperty>(layerCapacity, capacityStep);
                if (typedLayer != null)
                {
                    if (parallel)
                    {
                        entityIds.AsParallel().ForAll(entityId =>
                        {
                            typedLayer.UpdateIfSet(entityId, updateAction);
                        });
                    }
                    else
                    {
                        foreach (var entityId in entityIds)
                        {
                            typedLayer.UpdateIfSet(entityId, updateAction);
                        }
                    }
                }
                else throw new Exception("MutateSet entities exception.");
            }
        }

        /// <summary>
        /// Mutate properties of a list of entities, but only where the property is already set.
        /// </summary>
        /// <typeparam name="TProperty">The property to mutate.</typeparam>
        /// <param name="entityIds">A list of entities to potentially mutate</param>
        /// <param name="updateAction">Action to perform on the entity property</param>
        /// <exception cref="Exception">Unexpected.</exception>
        public void Mutate<TProperty>(IEnumerable<long> entityIds, UpdateRef<TProperty>? updateAction = null, bool parallel = false) where TProperty : struct
        {
            Layer<TProperty>? typedLayer;
            if (layers.TryGetValue(typeof(TProperty), out ILayer? layer))
            {
                typedLayer = layer as Layer<TProperty>;
                if (typedLayer != null)
                {
                    if (parallel)
                    {
                        entityIds.AsParallel().ForAll(entityId =>
                        {
                            typedLayer.Update(entityId, updateAction);
                        });
                    }
                    else
                    {
                        foreach (var entityId in entityIds)
                        {
                            typedLayer.Update(entityId, updateAction);
                        }
                    }
                }
                else throw new Exception("MutateSet entities exception.");
            }
            else
            {
                layers[typeof(TProperty)] = typedLayer = new Layer<TProperty>(layerCapacity, capacityStep);
                if (typedLayer != null)
                {
                    if (parallel)
                    {
                        entityIds.AsParallel().ForAll(entityId =>
                        {
                            typedLayer.Update(entityId, updateAction);
                        });
                    }
                    else
                    {
                        foreach (var entityId in entityIds)
                        {
                            typedLayer.Update(entityId, updateAction);
                        }
                    }
                }
                else throw new Exception("MutateSet entities exception.");
            }
        }
        /// <summary>
        /// Clear a number of entities
        /// This does not modify properties or unallocate anything
        /// </summary>
        /// <param name="entityIds">entity ids to release</param>
        public void Clear(IEnumerable<long> entityIds, bool parallel = false)
        {
            foreach (var entityId in entityIds)
                this.clearedEntities.Add(entityId);

            if (parallel)
            {
                layers.AsParallel().ForAll(layer =>
                {
                    layer.Value.MarkUnset(entityIds);
                });
            } else
            {
                foreach (var layer in layers)
                {
                    layer.Value.MarkUnset(entityIds);
                }
            }
        }

        /// <summary>
        /// Clear an entity
        /// This does not modify properties or unallocate anything
        /// </summary>
        /// <param name="entityId">entity id to release</param>
        public void Clear(long entityId)
        {
            this.clearedEntities.Add(entityId);

            foreach (var layerref in layers)
            {
                layerref.Value.MarkUnset(entityId);
            }
        }

        /// <summary>
        /// Clear a property from an entity
        /// </summary>
        /// <typeparam name="TProperty">The property to unset</typeparam>
        /// <param name="entityId">entity id</param>
        public void Clear<TProperty>(long entityId) where TProperty : struct
        {
            ILayer? layerRef;
            if (layers.TryGetValue(typeof(TProperty), out layerRef))
            {
                layerRef.MarkUnset(entityId);
            }
        }

        /// <summary>
        /// Get a list of entities that have a specific property
        /// </summary>
        /// <typeparam name="TProperty">The property to query for</typeparam>
        /// <returns>A list of entity ids</returns>
        public IEnumerable<long> QueryEntitiesWith<TProperty>() where TProperty : struct
        {
            ILayer? layerRef;
            if (layers.TryGetValue(typeof(TProperty), out layerRef))
            {
                return layerRef.QuerySet();
            }

            return Enumerable.Empty<long>();
        }

        /// <summary>
        /// Test if an entity has a property set
        /// </summary>
        /// <typeparam name="TProperty">The property of interest</typeparam>
        /// <param name="entityId">entity id</param>
        /// <returns>true if the property is set on the entity</returns>
        public bool Has<TProperty>(long entityId) where TProperty : struct
        {
            ILayer? layerRef;
            if (layers.TryGetValue(typeof(TProperty), out layerRef))
            {
                return layerRef.IsSet(entityId);
            }

            return false;
        }
    }
}