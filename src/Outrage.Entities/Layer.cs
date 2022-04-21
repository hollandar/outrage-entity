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
            }
            if (!layer[ix.y].IsSet) layer[ix.y].IsSet = true;
            if (updateAction != null)
            {
                updateAction(entityId, ref layer[ix.y].Value);
            }
        }

        public void UpdateSet(UpdateRef<TProperty> updateAction)
        {
            for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
            {
                if (layers[layerIndex] != null)
                {
                    int layerLength = layers[layerIndex].Length;
                    for (int propertyIndex = 0; propertyIndex < layerLength; propertyIndex++)
                    {
                        if (layers[layerIndex][propertyIndex].IsSet)
                        {
                            updateAction((layerIndex * capacityStep) + propertyIndex, ref layers[layerIndex][propertyIndex].Value);
                        }
                    }
                }
            }
        }

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

        public bool IsSet(long entityId)
        {
            var ix = GetIndex(entityId);
            if (layers[ix.x] != null)
                return layers[ix.x][ix.y].IsSet;

            return false;
        }

        public void MarkUnset(IEnumerable<long> entityIds)
        {
            foreach (var entityId in entityIds)
            {
                MarkUnset(entityIds);
            }
        }

        public void MarkUnset(long entityId)
        {
            var ix = GetIndex(entityId);
            if (layers[ix.x] != null)
                layers[ix.x][ix.y].IsSet = false;
        }
    }
}
