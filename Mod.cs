using Kitchen;
using Kitchen.Layouts;
using KitchenData;
using KitchenLib.References;
using KitchenMods;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenAcceleratedStart
{
    public class Mod : NightSystem, IModSystem
    {
        public const string MOD_GUID = "com.lk.acceleratedstart";
        public const string MOD_NAME = "Accelerated Start";
        public const string MOD_VERSION = "0.1.0";
        public const string MOD_AUTHOR = "LK";

        private EntityQuery _appliancesQuery;
        List<int> _applianceIds = new List<int>();

        private struct SAcceleratedStartProvided : IModComponent
        {
        }

        private struct SAcceleratedStartBlueprintsProvided : IModComponent
        {
        }

        protected override void Initialise()
        {
            LogWarning($"{MOD_GUID} v{MOD_VERSION} in use!");
            _appliancesQuery = GetEntityQuery(new QueryHelper()
                .All(typeof(CAppliance)));

            _applianceIds.Add(ApplianceReferences.GrabberRotatable);
            _applianceIds.Add(ApplianceReferences.SinkSoak);
            _applianceIds.Add(ApplianceReferences.PlateStack);
            _applianceIds.Add(ApplianceReferences.Mixer);
            _applianceIds.Add(ApplianceReferences.DishWasher);
            _applianceIds.Add(ApplianceReferences.AutoPlater);

            base.Initialise();
        }

        protected override void OnUpdate()
        {
            if (!Has<SLayout>())
                return;

            if (GetOrCreate<SDay>().Day != 0)
                return;

            if (!HasSingleton<SAcceleratedStartProvided>())
            {
                if (FreeStuff())
                {
                    BonusIncome();
                    World.Add<SAcceleratedStartProvided>();
                }
            }

            if (!HasSingleton<SAcceleratedStartBlueprintsProvided>())
            {
                if (AddBlueprintsToCabinet())
                {
                    World.Add<SAcceleratedStartBlueprintsProvided>();
                }
            }
        }

        private bool AddBlueprintsToCabinet()
        {
            NativeArray<Entity> entities = _appliancesQuery.ToEntityArray(Allocator.Temp);
            List<Entity> cabinets = new List<Entity>();
            foreach (Entity entity in entities)
            {
                if (Require(entity, out CAppliance appliance))
                {
                    if (appliance.ID == ApplianceReferences.BlueprintCabinet)
                    {
                        cabinets.Add(entity);
                    }
                }
            }

            if (cabinets.Count < 6)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < cabinets.Count; i++)
                {
                    Entity cabinet = cabinets[i];

                    // Adding some goodies
                    var appliance_id = _applianceIds[i];
                    GameData.Main.TryGet(appliance_id, out Appliance baseAppliance);
                    EntityManager.AddComponentData(cabinet, new CBlueprintStore
                    {
                        ApplianceID = appliance_id,
                        BlueprintID = AssetReference.Blueprint,
                        HasBeenCopied = true,
                        HasBeenMadeFree = false,
                        HasBeenUpgraded = false,
                        InUse = true,
                        Price = baseAppliance.PurchaseCost,
                    });
                }
                return true;
            }
        }

        private List<Vector3> GetNearbyTilesFromOrigin(Vector3 origin, int radius)
        {
            List<Vector3> nearbyTiles = new List<Vector3>();
            for (int i = 0; i < radius; i++)
            {
                float r = i + 1;
                AddIfInside(nearbyTiles, origin + new Vector3(r, 0, 0));
                AddIfInside(nearbyTiles, origin + new Vector3(-r, 0, 0));
                AddIfInside(nearbyTiles, origin + new Vector3(0, 0, r));
                AddIfInside(nearbyTiles, origin + new Vector3(0, 0, -r));
                AddIfInside(nearbyTiles, origin + new Vector3(r, 0,  r));
                AddIfInside(nearbyTiles, origin + new Vector3(-r, 0, r));
                AddIfInside(nearbyTiles, origin + new Vector3(r, 0, -r));
                AddIfInside(nearbyTiles, origin + new Vector3(-r, 0, -r));
            }
            return nearbyTiles;
        }

        private void AddIfInside(List<Vector3> tilesList, Vector3 position)
        {
            var tiletype = TileManager.GetTile(position).Type;
            if (LayoutHelpers.IsInside(tiletype))
            {
                tilesList.Add(position);
            }
        }

        private void BonusIncome()
        {
            var money = GetOrDefault<SMoney>();
            money.Amount += 500;
            Set(money);
        }

        private bool FreeStuff()
        {
            Vector3 cabinetLocOri = GetBlueprintCabinetLocation();
            if (cabinetLocOri == Vector3.zero)
                return false;

            List<Vector3> postTiles = GetPostTiles();

            // Absolutely free
            int cabinet_tile = 0;
            List<Vector3> cabinetLocList = GetNearbyTilesFromOrigin(cabinetLocOri, 2);
            for (int i = 0; i < 5; i++)
            {
                Vector3 cabinetLoc = GetValidTile(cabinetLocList, ref cabinet_tile);
                SpawnAppliance(EntityManager, cabinetLoc, ApplianceReferences.BlueprintCabinet);
                postTiles.Remove(cabinetLoc);
            }

            int placed_tile = 0;
            postTiles.ShuffleInPlace();
            Vector3 candidate = GetValidTile(postTiles, ref placed_tile);
            SpawnAppliance(EntityManager, candidate, ApplianceReferences.EnchantingDesk);

            candidate = GetValidTile(postTiles, ref placed_tile);
            SpawnAppliance(EntityManager, candidate, ApplianceReferences.BlueprintUpgradeDesk);

            candidate = GetValidTile(postTiles, ref placed_tile);
            SpawnAppliance(EntityManager, candidate, ApplianceReferences.BlueprintCopyDesk);
            
            candidate = GetValidTile(postTiles, ref placed_tile);
            SpawnAppliance(EntityManager, candidate, ApplianceReferences.BlueprintDiscountDesk);

            candidate = GetValidTile(postTiles, ref placed_tile);
            SpawnAppliance(EntityManager, candidate, ApplianceReferences.BlueprintOrderingDesk);


            // Test
            //bool useRed = true;
            //float priceFactor = 1f;

            //       C9    C10
            //    C8 D1/C3 -
            //    C5 C2    C6
            //    C4 C1    C7

            //Vector3 candidate1 = GetFallbackTile();
            //Vector3 candidate2 = GetFrontDoor(get_external_tile: true);
            //Vector3 candidate3 = GetFrontDoor(get_external_tile: false);
            //Vector3 candidate4 = GetFrontDoor(get_external_tile: true) + new Vector3(-1f, 0f, -1f); // -x is left, -z is down
            //Vector3 candidate5 = GetFrontDoor(get_external_tile: true) + new Vector3(-1f, 0f, 0f); // -x is left
            //Vector3 candidate6 = GetFrontDoor(get_external_tile: true) + new Vector3(1f, 0f, 0f); // x is right
            //Vector3 candidate7 = GetFrontDoor(get_external_tile: true) + new Vector3(1f, 0f, -1f); // x is right, -z is down
            //Vector3 candidate8 = candidate3 + new Vector3(-1f, 0f, 0f);

            // Purchase with starting money
            //PostHelpers.CreateBlueprintLetter(EntityManager, candidate2, ApplianceReferences.GrabberRotatable, priceFactor, use_red: useRed);
            //PostHelpers.CreateBlueprintLetter(EntityManager, candidate3, ApplianceReferences.SinkSoak, priceFactor, use_red: useRed);
            //PostHelpers.CreateBlueprintLetter(EntityManager, candidate4, ApplianceReferences.Teleporter, priceFactor, use_red: useRed);
            //PostHelpers.CreateBlueprintLetter(EntityManager, candidate5, ApplianceReferences.Mixer, priceFactor, use_red: useRed);
            //PostHelpers.CreateBlueprintLetter(EntityManager, candidate6, ApplianceReferences.Microwave, priceFactor, use_red: useRed);
            //PostHelpers.CreateBlueprintLetter(EntityManager, candidate7, ApplianceReferences.AutoPlater, priceFactor, use_red: useRed);
            //PostHelpers.CreateApplianceParcel(EntityManager, candidate2, ApplianceReferences.BlueprintCabinet);
            //PostHelpers.CreateOpenedLetter(EntityManager, candidate2, ApplianceReferences.BlueprintCabinet, 0f);

            return true;
        }

        public static Entity SpawnAppliance(EntityManager em, Vector3 position, int appliance_id)
        {
            Entity entity = em.CreateEntity();
            em.AddComponentData(entity, new CCreateAppliance
            {
                ID = appliance_id,
            });
            em.AddComponentData(entity, new CPosition(position));
            return entity;
        }

        private Vector3 GetBlueprintCabinetLocation()
        {
            NativeArray<Entity> entities = _appliancesQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in entities)
            {
                if (Require(entity, out CAppliance appliance))
                {
                    LogWarning("Found CAppliance of Blueprint Cabinet");
                    if (appliance.ID == ApplianceReferences.BlueprintCabinet)
                    {
                        LogWarning("Found Blueprint Cabinet ID");
                        if (Require(entity, out CPosition position))
                        {
                            LogWarning("Found CPosition of Blueprint Cabinet");
                            return position.Position;
                        }
                    }
                }
            }
            return Vector3.zero;
        }

        private Vector3 GetValidTile(List<Vector3> postTiles, ref int placed_tile)
        {
            if (!FindTile(ref placed_tile, postTiles, out var candidate))
            {
                candidate = GetFallbackTile();
            }

            return candidate;
        }

        private bool FindTile(ref int placed_tile, List<Vector3> floor_tiles, out Vector3 candidate)
        {
            candidate = Vector3.zero;
            bool flag = false;
            while (!flag && placed_tile < floor_tiles.Count)
            {
                candidate = floor_tiles[placed_tile++];
                if (TileManager.GetOccupant(candidate) == default(Entity))
                {
                    flag = true;
                }
            }

            if (!flag)
            {
                return false;
            }

            return true;
        }

        #region Logging
        internal static void LogInfo(string _log) { Debug.Log($"[{MOD_NAME}] " + _log); }
        internal static void LogWarning(string _log) { Debug.LogWarning($"[{MOD_NAME}] " + _log); }
        internal static void LogError(string _log) { Debug.LogError($"[{MOD_NAME}] " + _log); }
        #endregion
    }
}