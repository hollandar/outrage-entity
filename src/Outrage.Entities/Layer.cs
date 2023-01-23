using System.Net.WebSockets;

namespace Outrage.Entities
{

    internal struct Property<TProperty> where TProperty : struct
    {
        public bool IsSet;
        public TProperty Value;

        public Property()
        {
            this.IsSet = false;
            this.Value = default(TProperty);
        }
    }

    internal class Layer<TProperty> : ILayer where TProperty : struct
    {
        int layerCount;
        int capacityStep;
        long upperLayer = 0;
        Property<TProperty>[][] layers;

        public IEnumerable<long> SetEntities => throw new NotImplementedException();

        /// <summary>
        /// Construct a layer
        /// </summary>
        /// <param name="layerCount">The number of allocation blocks</param>
        /// <param name="capacityStep">The size of each allocation block</param>
        public Layer(int layerCount = 1000, int capacityStep = 1000)
        {
            this.layerCount = layerCount;
            this.capacityStep = capacityStep;
            layers = new Property<TProperty>[layerCount][];
        }

        /// <summary>
        /// Given an entity id, work out what the index in the collection is
        /// </summary>
        /// <param name="id">entity id</param>
        /// <returns>(x,y) index into array set</returns>
        /// <exception cref="CapacityException">The entity ID was larger than the layers support</exception>
        private (long x, long y) GetIndex(long id)
        {
            var o = Math.DivRem(id, capacityStep);
            if (o.Quotient > layerCount)
                throw new CapacityException();

            return (o.Quotient, o.Remainder);
        }

        /// <summary>
        /// Get the value of a property from the layer
        /// </summary>
        /// <param name="entityId">the entity</param>
        /// <returns>entity property</returns>
        public TProperty Get(long entityId)
        {
            var ix = GetIndex(entityId);
            var layer = layers[ix.x];
            if (layer == null)
            {
                layers[ix.x] = layer = new Property<TProperty>[this.capacityStep];
            }

            return layer[ix.y].Value;
        }

        /// <summary>
        /// Perform an update action against an entity in this layer
        /// </summary>
        /// <param name="entityId">entityId</param>
        /// <param name="updateAction">update action</param>
        public void Update(long entityId, UpdateRef<TProperty>? updateAction)
        {
            var ix = GetIndex(entityId);
            var layer = layers[ix.x];
            if (layer == null)
            {
                layers[ix.x] = layer = new Property<TProperty>[this.capacityStep];
                upperLayer = upperLayer < ix.x ? ix.x : upperLayer;
            }
            if (!layer[ix.y].IsSet) layer[ix.y].IsSet = true;
            if (updateAction != null)
            {
                updateAction(entityId, ref layer[ix.y].Value);
            }
        }

        /// <summary>
        /// Perform an update action against an entity in this layer
        /// </summary>
        /// <param name="entityId">entityId</param>
        /// <param name="updateAction">update action</param>
        public void UpdateIfSet(long entityId, UpdateRef<TProperty>? updateAction)
        {
            var ix = GetIndex(entityId);
            var layer = layers[ix.x];
            if (layer == null)
                return;

            if (layer[ix.y].IsSet)
            {
                if (updateAction != null)
                {
                    updateAction(entityId, ref layer[ix.y].Value);
                }
            }
        }

        /// <summary>
        /// Update properties that are marked with the set flag
        /// </summary>
        /// <param name="updateAction">Action to apply</param>
        public void UpdateSet(UpdateRef<TProperty> updateAction)
        {
            for (var layerIndex = 0; layerIndex < layers.Length; layerIndex++)
            {
                if (layers[layerIndex] != null)
                {
                    int layerLength = layers[layerIndex].Length;
                    for (var propertyIndex = 0; propertyIndex < layerLength; propertyIndex++)
                    {
                        if (layers[layerIndex][propertyIndex].IsSet)
                        {
                            updateAction((layerIndex * capacityStep) + propertyIndex, ref layers[layerIndex][propertyIndex].Value);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update properties that are marked with the set flag
        /// </summary>
        /// <param name="updateAction">Action to apply</param>
        public void UpdateSetParallel(UpdateRef<TProperty> updateAction)
        {
            GetLayerIds().AsParallel().ForAll(layerIndex =>
            {
                var layer = layers[layerIndex];
                if (layer != null)
                {
                    for (int i = 0; i < layer.Length; i++)
                    {
                        if (layer[i].IsSet)
                        {
                            updateAction(layerIndex * i, ref layer[i].Value);
                        }
                    }
                }
            });
        }

        private IEnumerable<long> GetLayerIds()
        {
            for (long i = 0; i < upperLayer; i++)
                yield return i;
        }

        /// <summary>
        /// Query for properties marked with the set flag
        /// </summary>
        /// <returns>list of entity ids</returns>
        public IEnumerable<long> QuerySet()
        {
            for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
            {
                if (layers[layerIndex] != null)
                {
                    int layerLength = layers[layerIndex].Length;
                    for (int propertyIndex = 0; propertyIndex < layerLength; propertyIndex++)
                    {
                        if (layers[layerIndex][propertyIndex].IsSet)
                            yield return (layerIndex * capacityStep) + propertyIndex;
                    }
                }
            }
        }

        /// <summary>
        /// Test if an entity id is set
        /// </summary>
        /// <param name="entityId">entity id to test</param>
        /// <returns>list of entity ids</returns>
        public bool IsSet(long entityId)
        {
            var ix = GetIndex(entityId);
            if (layers[ix.x] != null)
                return layers[ix.x][ix.y].IsSet;

            return false;
        }

        /// <summary>
        /// Mark a number of entity ids as unset
        /// </summary>
        /// <param name="entityIds">list of entity ids</param>
        public void MarkUnset(IEnumerable<long> entityIds)
        {
            entityIds.AsParallel().ForAll((entityId) =>
            {
                MarkUnset(entityId);
            });
        }

        /// <summary>
        /// Mark an individual entity id as unset
        /// </summary>
        /// <param name="entityId">entity id</param>
        public void MarkUnset(long entityId)
        {
            var ix = GetIndex(entityId);
            if (layers[ix.x] != null)
                layers[ix.x][ix.y].IsSet = false;
        }
    }
}
