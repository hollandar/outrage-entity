namespace Outrage.Entities
{
    /// <summary>
    /// Update action for updating an entity
    /// </summary>
    /// <typeparam name="TProperty">The type of property we are setting</typeparam>
    /// <param name="entityId">entity id</param>
    /// <param name="entity">a ref to the struct containing the property</param>
    public delegate void UpdateRef<TProperty>(long entityId, ref TProperty entity) where TProperty: struct;
}
