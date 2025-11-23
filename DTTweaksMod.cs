using HarmonyLib;
using STM.Data;
using STM.Data.Entities;
using STM.GameWorld;
using STM.GameWorld.Users;
using System.Reflection;
using System.Runtime.InteropServices;
using Utilities;

namespace DTTweaks;


public static class ModEntry
{
    public static readonly string ModName = nameof(DTTweaks);

    [UnmanagedCallersOnly]
    public static int InitializeMod()
    {
        if (ModInit.InitializeMod(ModName))
        {
            // Gather environment info
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            string? assemblyDirectory = Path.GetDirectoryName(currentAssembly.Location);
            // Read config data
            _ = ConfigToolXml.LoadConfig(ModName, assemblyDirectory!);
            // Do other stuff here to initialize
            if (ConfigToolXml.Config?.Defaults != null) ChangeMainData();
            Log.DumpMainDataDefaults();
            if (ConfigToolXml.Config?.Vehicles != null) ChangeVehicles();
        }
        return 0;
    }


    internal static void ChangeMainData()
    {
        DefaultsXml paramDefaults = ConfigToolXml.Config?.Defaults!;

        foreach (ParamXml param in paramDefaults.Params)
        {
            if (param.IsOK)
            {
                MainData.Defaults.SetPublicProperty(param.Name!, param.GetValue());
            }
            else
                Log.Write($"Wrong param: {param}");
        }
    }

    internal static void ChangeVehicles()
    {
        foreach (VehicleXml vehicle in ConfigToolXml.Config!.Vehicles)
        {
            if (vehicle.IsOK)
            {
                // find the vehicle
                VehicleBaseEntity? entity = vehicle.Type switch
                {
                    "road_vehicle" => MainData.Road_vehicles.Where(x => x.Name == vehicle.Name).FirstOrDefault(),
                    "train" => MainData.Trains.Where(x => x.Name == vehicle.Name).FirstOrDefault(),
                    "plane" => MainData.Planes.Where(x => x.Name == vehicle.Name).FirstOrDefault(),
                    "ship" => MainData.Ships.Where(x => x.Name == vehicle.Name).FirstOrDefault(),
                    _ => null,
                };
                if (entity == null)
                {
                    Log.Write($"Vehicle {vehicle.Name} not found.");
                    continue;
                }
                // modify
                foreach (ParamXml param in vehicle.Params)
                {
                    if (param.IsOK)
                    {
                        entity.SetPublicProperty(param.Name!, param.GetValue());
                    }
                    else
                        Log.Write($"Wrong param: {param}");
                }
            }
            else
                Log.Write($"Wrong vehicle {vehicle}");
        }

        // Dump companies
        Log.Write("--- VEHICLE COMPANIES ---");
        foreach (VehicleCompanyEntity vce in MainData.Vehicle_companies)
            Log.Write($"{vce.ID} {vce.Translated_name} {vce.Country.ISO} {vce.GetRegionName()} {vce.Name}");

        // Dump vehicles
        Log.Write("--- ROAD VEHICLES---");
        foreach (VehicleBaseEntity vbe in MainData.Road_vehicles)
            Log.Write($"{vbe.ID} {vbe.Translated_name} t{vbe.Tier} s{vbe.Speed} c{vbe.Capacity} m{vbe.GetPrivateProperty<int>("Min_passengers")} p{vbe.Price} i{vbe.Passenger_pay_per_km} e{-vbe.Cost_per_km} {vbe.Name}");
        Log.Write("--- TRAINS ---");
        foreach (TrainEntity vbe in MainData.Trains)
            Log.Write($"{vbe.ID} {vbe.Translated_name} t{vbe.Tier} s{vbe.Speed} c{vbe.Max_capacity} m{vbe.GetPrivateProperty<int>("Min_passengers")} p{vbe.Price} i{vbe.Passenger_pay_per_km} e{-vbe.Cost_per_km} {vbe.Name}");
        Log.Write("--- SHIPS ---");
        foreach (VehicleBaseEntity vbe in MainData.Ships)
            Log.Write($"{vbe.ID} {vbe.Translated_name} t{vbe.Tier} s{vbe.Speed} c{vbe.Capacity} m{vbe.GetPrivateProperty<int>("Min_passengers")} p{vbe.Price} i{vbe.Passenger_pay_per_km} e{-vbe.Cost_per_km} {vbe.Name}");
        Log.Write("--- PLANES ---");
        foreach (PlaneEntity vbe in MainData.Planes)
            Log.Write($"{vbe.ID} {vbe.Translated_name} t{vbe.Tier} s{vbe.Speed} c{vbe.Capacity} m{vbe.GetPrivateProperty<int>("Min_passengers")} r{vbe.Range} p{vbe.Price} i{vbe.Passenger_pay_per_km} e{-vbe.Cost_per_km} {vbe.Name}");
    }
}


[HarmonyPatch]
public static class Patches
{
    private static int _MaxLevel = int.MaxValue; // 0 is disabled

    [HarmonyPatch(typeof(CityUser), "LevelUp"), HarmonyPrefix]
    public static bool CityUser_LevelUp_Prefix(CityUser __instance, GameScene scene, bool threaded)
    {
        if (_MaxLevel <= 0) return true; // continue with the original

        if (_MaxLevel == int.MaxValue) // late init
        {
            ParamXml? paramMax = ConfigToolXml.Config?.Options?.TryGet("RawCityLevelLimit");
            _MaxLevel = paramMax == null ? 0 : (paramMax.IsOK ? (int)paramMax.GetValue() : 0);
            if (_MaxLevel <= 0) return true; // continue with the original
        }

        // limit growth
        int max = _MaxLevel;
        if (__instance.Important) max += 3;
        if (__instance.City.Capital) max += 3; // capital is always Important
        if (__instance.City.Resort) max -= 5; // should be small
        if (__instance.Sea != null) max += 2;
        // each building level adds 1 city level
        // TODO: there can be more hubs from other players - this also should influence size
        ushort player = scene.Session.Player;
        Hub hub = __instance.GetHub(player);
        if (hub != null)
        {
            int buildings = 0;
            foreach (CityBuilding bldg in hub.Buildings.Where(b => b != null))
                buildings += bldg.Level;
            max += 1 * buildings;
        }
        //Log.Write($"{__instance.City.GetCountry(scene).ISO3166_1} {__instance.Name} {__instance.Level} / {max}   h={__instance.City.Height:F2}  p={__instance.City.Real_population} r={__instance.City.Resort}");
        return __instance.Level < max;
    }
}
