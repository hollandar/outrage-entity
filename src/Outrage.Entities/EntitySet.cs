using System.Collections.Concurrent;

namespace Outrage.Entities
{
    public class EntitySet
    {
        private int capacityStep;
        private int layerCapacity;
        ConcurrentDictionary<Type, ILayerRef> layers;
        HashSet<long> clearedEntities;
        long lastEntityId = 0;

        public EntitySet(int layerCapacity = 1000, int capacityStep = 1000)
        {
            this.layerCapacity = layerCapacity;
            this.capacityStep = capacityStep;
            this.layers = new ConcurrentDictionary<Type, ILayerRef>();
            this.clearedEntities = new HashSet<long>(capacityStep);
        }

        public long Count => lastEntityId - this.clearedEntities.Count;
        public long Capacity => layerCapacity * capacityStep;

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

        public TEntity Get<TEntity>(long entityId) where TEntity : struct
        {
            if (entityId >= lastEntityId || this.clearedEntities.Contains(entityId))
                throw new EntityUndefinedException();

            LayerRef<TEntity>? typedLayer;
            if (layers.TryGetValue(typeof(TEntity), out ILayerRef? layer))
            {
                typedLayer = layer as LayerRef<TEntity>;
                if (typedLayer != null)
                {
                    return typedLayer.Layer.Get(entityId);
                }
                else throw new Exception("Get entities exception.");
            }
            else
            {
                layers[typeof(TEntity)] = typedLayer = new LayerRef<TEntity>(layerCapacity, capacityStep);
                return typedLayer.Layer.Get(entityId);
            }
        }

        public void Mutate<TEntity>(long entityId, UpdateRef<TEntity>? updateAction = null) where TEntity : struct
        {
            LayerRef<TEntity>? typedLayer;
            if (layers.TryGetValue(typeof(TEntity), out ILayerRef? layer))
            {
                typedLayer = layer as LayerRef<TEntity>;
                if (typedLayer != null)
                {
                    typedLayer.Layer.Update(entityId, updateAction);
                    typedLayer.MarkSet(entityId);
                }
                else throw new Exception("Mutate entities exception.");
            }
            else
            {
                layers[typeof(TEntity)] = typedLayer = new LayerRef<TEntity>(layerCapacity, capacityStep);
                if (typedLayer != null)
                {
                    typedLayer.Layer.Update(entityId, updateAction);
                    typedLayer.MarkSet(entityId);
                }
                else throw new Exception("Mutate entities exception.");
            }
        }

        public void MutateAllSet<TEntity>(UpdateRef<TEntity>? updateAction = null) where TEntity : struct
        {
            LayerRef<TEntity>? typedLayer;
            if (layers.TryGetValue(typeof(TEntity), out ILayerRef? layer))
            {
                typedLayer = layer as LayerRef<TEntity>;
                if (typedLayer != null)
                {
                    foreach (var entityId in typedLayer.SetEntities)
                    {
                        typedLayer.Layer.Update(entityId, updateAction);
                        typedLayer.MarkSet(entityId);
                    }
                }
                else throw new Exception("MutateAll entities exception.");
            }
            else
            {
                layers[typeof(TEntity)] = typedLayer = new LayerRef<TEntity>(layerCapacity, capacityStep);
                if (typedLayer != null)
                {
                    foreach (var entityId in typedLayer.SetEntities)
                    {
                        typedLayer.Layer.Update(entityId, updateAction);
                        typedLayer.MarkSet(entityId);
                    }
                }
                else throw new Exception("MutateAll entities exception.");
            }
        }
        
        public void MutateAllSetWith<TEntity, TEntityWith>(UpdateRef<TEntity>? updateAction = null) where TEntity : struct where TEntityWith : struct
        {
            
            LayerRef<TEntity>? typedLayer;
            if (layers.TryGetValue(typeof(TEntity), out ILayerRef? layer))
            {
                typedLayer = layer as LayerRef<TEntity>;
                if (typedLayer != null)
                {
                    foreach (var entityId in QueryEntitiesWith<TEntityWith>())
                    {
                        if (Has<TEntity>(entityId))
                        {
                            typedLayer.Layer.Update(entityId, updateAction);
                            typedLayer.MarkSet(entityId);
                        }
                    }
                }
                else throw new Exception("MutateAll entities exception.");
            }
            else
            {
                layers[typeof(TEntity)] = typedLayer = new LayerRef<TEntity>(layerCapacity, capacityStep);
                if (typedLayer != null)
                {
                    foreach (var entityId in QueryEntitiesWith<TEntityWith>())
                    {
                        if (Has<TEntity>(entityId))
                        {
                            typedLayer.Layer.Update(entityId, updateAction);
                            typedLayer.MarkSet(entityId);
                        }
                    }
                }
                else throw new Exception("MutateAll entities exception.");
            }
        }

        public void MutateSet<TEntity>(IEnumerable<long> entityIds, UpdateRef<TEntity>? updateAction = null) where TEntity : struct
        {
            LayerRef<TEntity>? typedLayer;
            if (layers.TryGetValue(typeof(TEntity), out ILayerRef? layer))
            {
                typedLayer = layer as LayerRef<TEntity>;
                if (typedLayer != null)
                {
                    foreach (var entityId in entityIds)
                    {
                        if (Has<TEntity>(entityId)) { 
                            if (updateAction != null) typedLayer.Layer.Update(entityId, updateAction);
                            typedLayer.MarkSet(entityId);
                        }
                    }
                }
                else throw new Exception("MutateSet entities exception.");
            }
            else
            {
                layers[typeof(TEntity)] = typedLayer = new LayerRef<TEntity>(layerCapacity, capacityStep);
                if (typedLayer != null)
                {
                    foreach (var entityId in entityIds)
                    {
                        if (Has<TEntity>(entityId))
                        {
                            typedLayer.Layer.Update(entityId, updateAction);
                            typedLayer.MarkSet(entityId);
                        }
                    }
                }
                else throw new Exception("MutateSet entities exception.");
            }
        }

        public void Clear(long entityId)
        {
            this.clearedEntities.Add(entityId);

            foreach (var layerref in layers)
            {
                layerref.Value.MarkSet(entityId, false);
            }
        }

        public void Clear<TEntity>(long entityId) where TEntity: struct
        {
            ILayerRef? layerRef;
            if (layers.TryGetValue(typeof(TEntity), out layerRef))
            {
                layerRef.MarkSet(entityId, false);
            }
        }

        public IEnumerable<long> QueryEntitiesWith<TEntity>() where TEntity : struct
        {
            ILayerRef? layerRef;
            if (layers.TryGetValue(typeof(TEntity), out layerRef))
            {
                return layerRef.SetEntities;
            }

            return Enumerable.Empty<long>();
        }

        public bool Has<TEntity>(long entityId) where TEntity : struct
        {
            ILayerRef? layerRef;
            if (layers.TryGetValue(typeof(TEntity), out layerRef))
            {
                return layerRef.IsSet(entityId);
            }

            return false;
        }
    }
}