#pragma warning disable CA1416 // Validate platform compatibility

using HarmonyLib;
using STM.Data;
using STM.Data.Entities;
using STM.GameWorld;
using STM.GameWorld.Users;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Utilities;
using DTTweaks.Config;

namespace DTTweaks;


public static class ModEntry
{
    public static string ModName { get; private set; } = nameof(DTTweaks);
    public static string HarmonyId { get; private set; } = String.Empty;

    //public static ConfigurationXml? Config { get; private set; }


    [UnmanagedCallersOnly]
    public static int InitializeMod()
    {
        DebugConsole.Show();

        // Gather environment info
        Assembly currentAssembly = Assembly.GetExecutingAssembly();
        ModName = currentAssembly.GetName().Name ?? nameof(DTTweaks);
        string? assemblyDirectory = Path.GetDirectoryName(currentAssembly.Location);
        HarmonyId = "Infixo." + ModName;
        Log.Write($"Mod {ModName} successfully started. HarmonyId is {HarmonyId}.");

        // Read config data
        _ = ConfigToolXml.LoadConfig(ModName, assemblyDirectory!);
        //if (Config == null)
        //{
            //Log.Write("Failed to load the config file. Aborting.");
            //return 1;
        //}

        try
        {
            // Harmony
            var harmony = new Harmony(HarmonyId);
            //harmony.PatchAll(typeof(Mod).Assembly);
            harmony.PatchAll();
            var patchedMethods = harmony.GetPatchedMethods().ToArray();
            Log.Write($"Plugin {HarmonyId} made patches! Patched methods: " + patchedMethods.Length);
            foreach (var patchedMethod in patchedMethods)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                Log.Write($"Patched method: {patchedMethod.Module.Name}:{patchedMethod.DeclaringType.Name}.{patchedMethod.Name}");
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
        }
        catch (Exception ex)
        {
            Log.Write("EXCEPTION. ABORTING.");
            Log.Write(ex.ToString());
            return 2;
        }

        // do other stuff here to initialize
        if (ConfigToolXml.Config?.Defaults != null) ChangeMainData();
        Log.DumpMainDataDefaults();
        if (ConfigToolXml.Config?.Vehicles != null) ChangeVehicles();

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

        //MainData.Defaults.SetPublicProperty("Passenger_max_search_connections", 4); // 6

        // Roads & Rails cost
        //MainData.Defaults.SetPublicProperty("Road_price", 200000); // 150k => 200k
        //MainData.Defaults.SetPublicProperty("Rails_price", 600000);  // 250k => 600k

        // Station times
        //MainData.Defaults.SetPublicProperty("Bus_station_time", 300); // 900
        //MainData.Defaults.SetPublicProperty("Train_station_time", 600);  // 300
        //Plane_airport_time = 3600(Int32)
        //Ship_port_time = 3600(Int32)

        // passengers per month: 86400 / City_destination_progress
        // 1 would mean 1 passenger every second
        // normal 5760 is 15, high 2880 is 30
        // Max_level_destination=4  
        //City_destination_progress_cap = 15(Int32) // destination from 15 to 60 passengers
        //Utilities.Log.DumpMainDataDefaults: City_destination_progress= 5760(Int32)
        // passengers high
        //City_destination_progress_cap= 30(Int32) // destination from 30 to 120 passengers
        //Utilities.Log.DumpMainDataDefaults: City_destination_progress= 2880(Int32)

        // City shrinks if fulfillment is below that threshold
        //MainData.Defaults.SetPublicProperty("City_shrink_threshold", 0.5m); // 0.3

        // Last destination is changed if its fulfillment is below that threshold
        //MainData.Defaults.SetPublicProperty("City_destination_change = 0,3(Decimal) // last destination is changed if below that
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

        // Road vehicles
        //RoadVehicleEntity vehicle = MainData.Road_vehicles.Where(x => x.Name == "vehicle_antero").First();
        //vehicle.SetPublicProperty("Capacity", 20); // 3 Antero 12 => 20 // less cap than prev
        //vehicle.SetPublicProperty("Price", 175000);

        // Ships
        //MainData.Ships.Where(x => x.Name == "vehicle_silver_tide").First().SetPublicProperty("Capacity", 700); // 5 Silver Tide 860 => 700
        //MainData.Ships.Where(x => x.Name == "vehicle_royal_wanderer").First().SetPublicProperty("Capacity", 1460); // 6 Royal Wanderer  1360 => 1460
        //MainData.Ships.Where(x => x.Name == "vehicle_sea_conqueror").First().SetPublicProperty("Capacity", 2800); // 6 Sea Conqueror 7600 => 2800 // absurd number
        //MainData.Ships.Where(x => x.Name == "vehicle_wind_rider").First().SetPublicProperty("Capacity", 190); // 3 Wind Rider 250 => 190 // too many, makes next one worse

        // Trains
        //MainData.Trains.Where(x => x.Name == "vehicle_class_87").First().SetPublicProperty("Capacity", 420/6); // 5 Class 87 360 => 430 // lower capacity than previous!
        //MainData.Trains.Where(x => x.Name == "vehicle_apex_3").First().SetPublicProperty("Capacity", 450/6); // 6 Apex 3 390 => 450 // only Tier6 with less cap than Tier5
        //MainData.Trains.Where(x => x.Name == "vehicle_gsw_5").First().SetPublicProperty("Capacity", 320/5); // 4 GSW - 5 275 => 320 // less cap than prev 

        // Planes
        //MainData.Planes.Where(x => x.Name == "vehicle_a950l").First().SetPublicProperty("Capacity", 150); // 5 120 => 150 // lower capacity than previous!
        //MainData.Planes.Where(x => x.Name == "vehicle_822_135").First().SetPublicProperty("Capacity", 135); // 5 134 => 135 stick with the name
        //MainData.Planes.Where(x => x.Name == "vehicle_822_270").First().SetPublicProperty("Capacity", 270); // 5 134 => 270 stick with the name
        //MainData.Planes.Where(x => x.Name == "vehicle_900_350").First().SetPublicProperty("Capacity", 350); // 5 351 => 350 stick with the name

        // Dump companies
        Log.Write("--- VEHICLE COMPANIES ---");
        foreach (VehicleCompanyEntity vce in MainData.Vehicle_companies)
            Log.Write($"{vce.ID} {vce.Translated_name} {vce.Country.ISO} {vce.GetRegionName()} {vce.Name}");

        // Dump vehicles
        Log.Write("--- ROAD VEHICLES---");
        foreach (VehicleBaseEntity vbe in MainData.Road_vehicles)
            Log.Write($"{vbe.ID} {vbe.Translated_name} t{vbe.Tier} s{vbe.Speed} c{vbe.Capacity} m{vbe.GetPrivateProperty<int>("Min_passengers")} p{vbe.Price} e{vbe.Passenger_pay_per_km} {vbe.Name}");
        Log.Write("--- TRAINS ---");
        foreach (TrainEntity vbe in MainData.Trains)
            Log.Write($"{vbe.ID} {vbe.Translated_name} t{vbe.Tier} s{vbe.Speed} c{vbe.Max_capacity} m{vbe.GetPrivateProperty<int>("Min_passengers")} p{vbe.Price} e{vbe.Passenger_pay_per_km} {vbe.Name}");
        Log.Write("--- SHIPS ---");
        foreach (VehicleBaseEntity vbe in MainData.Ships)
            Log.Write($"{vbe.ID} {vbe.Translated_name} t{vbe.Tier} s{vbe.Speed} c{vbe.Capacity} m{vbe.GetPrivateProperty<int>("Min_passengers")} p{vbe.Price} e{vbe.Passenger_pay_per_km} {vbe.Name}");
        Log.Write("--- PLANES ---");
        foreach (PlaneEntity vbe in MainData.Planes)
            Log.Write($"{vbe.ID} {vbe.Translated_name} t{vbe.Tier} s{vbe.Speed} c{vbe.Capacity} m{vbe.GetPrivateProperty<int>("Min_passengers")} r{vbe.Range} p{vbe.Price} e{vbe.Passenger_pay_per_km} {vbe.Name}");
    }
}


[HarmonyPatch]
public static class Patches
{
    private static int _MaxLevel = 0; // 0 is undefined, -1 is disabled

    [HarmonyPatch(typeof(CityUser), "LevelUp"), HarmonyPrefix]
    public static bool CityUser_LevelUp_Prefix(CityUser __instance, GameScene scene, bool threaded)
    {
        if (_MaxLevel == 0) // late init
        {
            ParamXml? paramMax = ConfigToolXml.Config?.Options?.TryGet("RawCityLevelLimit");
            _MaxLevel = paramMax == null ? -1 : (paramMax.IsOK ? (int)paramMax.GetValue() : -1);
        }
        if (_MaxLevel < 0) return true; // continue with the original

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

#pragma warning restore CA1416 // Validate platform compatibility
