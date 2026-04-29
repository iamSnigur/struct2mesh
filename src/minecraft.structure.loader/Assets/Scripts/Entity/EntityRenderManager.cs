using System.Collections.Generic;
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class EntityRenderManager : MonoBehaviour
    {
        #region GameObject Prefabs for each entity type
        [SerializeField] private GameObject defaultPrefab;

        [SerializeField] private GameObject itemPrefab;
        #endregion

        private readonly Dictionary<ResourceLocation, GameObject> entityPrefabs = new();

        private GameObject GetPrefabForType(ResourceLocation type)
        {
            return entityPrefabs.GetValueOrDefault(type, defaultPrefab);
        }
        
        /// <summary>
        /// All entity renders in the current world
        /// </summary>
        private readonly Dictionary<int, EntityRender> entityRenders = new();
        
        /// <summary>
        /// A dictionary storing entities near the player
        /// Entity Id => Square distance to player
        /// </summary>
        private readonly Dictionary<int, float> nearbyEntities = new();

        /// <summary>
        /// Create a new entity render from given entity data, and prefab
        /// </summary>
        /// <param name="entitySpawn">Entity data</param>
        /// <param name="entityPrefab">Entity prefab</param>
        /// 
        public EntityRender AddEntityRenderFromGivenPrefab(EntitySpawnData entitySpawn, GameObject entityPrefab)
        {
            // If the numeral id is occupied by an entity already,
            // destroy this entity first
            if (entityRenders.ContainsKey(entitySpawn.Id))
            {
                if (entityRenders[entitySpawn.Id])
                {
                    entityRenders[entitySpawn.Id].Unload();
                }
                
                entityRenders.Remove(entitySpawn.Id);
                nearbyEntities.Remove(entitySpawn.Id);
            }

            if (entityPrefab)
            {
                var entityObj = Instantiate(entityPrefab, transform, true);
                var entityRender = entityObj!.GetComponent<EntityRender>();

                entityRenders.Add(entitySpawn.Id, entityRender);

                entityObj.name = $"{entitySpawn.Id} {entitySpawn.Type}";

                // Initialize the entity
                entityRender.Initialize(entitySpawn, _worldOriginOffset);
                
                return entityRender;
            }

            return null;
        }
        
        private Vector3Int _worldOriginOffset = Vector3Int.zero;

        private void Start()
        {
            // Clear loaded things
            entityPrefabs.Clear();

            // Register entity render prefabs ===========================================
            entityPrefabs.Add(EntityType.ITEM_ID,              itemPrefab);
            // ...

            foreach (var prefabItem in entityPrefabs)
            {
                if (!prefabItem.Value)
                {
                    Debug.LogWarning($"Prefab for entity type {prefabItem.Key} is not assigned!");
                }            
            }
        }

        private void Update()
        {
            /*foreach (var render in entityRenders.Values)
            {
                // Call managed update
                render.ManagedUpdate(tickMilSec, cameraTransform);

                if (render.IsClientEntity)
                {
                    continue;
                }

                // Update entities around the player
                var dist = (render.transform.position - client.GetPosition()).sqrMagnitude;
                var entityId = render.NumeralId;
                var inNearbyDict = nearbyEntities.ContainsKey(entityId);

                if (dist < NEARBY_THRESHOLD_INNER) // Add entity to dictionary
                {
                    if (inNearbyDict)
                        nearbyEntities[entityId] = dist;
                    else
                        nearbyEntities.Add(entityId, dist);
                }
                else if (dist > NEARBY_THRESHOLD_OUTER) // Remove entity from dictionary
                {
                    if (inNearbyDict)
                        nearbyEntities.Remove(entityId);
                }
                else // Update entity's distance to the player if it is in the dictionary, otherwise do nothing
                {
                    if (inNearbyDict)
                        nearbyEntities[entityId] = dist;
                }
            }*/
        }
    }
}