#pragma warning disable CA1416 // Validate platform compatibility

using HarmonyLib;
using STM.Data;
using STM.Data.Entities;
using System.Runtime.InteropServices;
using Utilities;

namespace DTTweaks;


public static class ModEntry
{
    public static readonly string harmonyId = "Infixo." + nameof(DTTweaks);

    // mod's instance and asset
    //public static Mod instance { get; private set; }
    //public static ExecutableAsset modAsset { get; private set; }
    // logging
    //public static ILog log = LogManager.GetLogger($"{nameof(InfoLoom)}").SetShowsErrorsInUI(false);

    [UnmanagedCallersOnly]
    //[UnmanagedCallersOnly(EntryPoint = "InitializeMod")] // not needed when called via CLR
    //[ModuleInitializer] // only works with CLR, not native loads?
    public static int InitializeMod()
    {
        DebugConsole.Show();
        Log.Write($"WWR mod {nameof(DTTweaks)} successfully started, harmonyId is {harmonyId}.");
        try
        {
            // Harmony
            var harmony = new Harmony(harmonyId);
            //harmony.PatchAll(typeof(Mod).Assembly);
            harmony.PatchAll();
            var patchedMethods = harmony.GetPatchedMethods().ToArray();
            Log.Write($"Plugin {harmonyId} made patches! Patched methods: " + patchedMethods.Length);
            foreach (var patchedMethod in patchedMethods)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                Log.Write($"Patched method: {patchedMethod.Module.Name}:{patchedMethod.DeclaringType.Name}.{patchedMethod.Name}");
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
        }
        catch (Exception ex)
        {
            Log.Write("EXCEPTION");
            Log.Write(ex.ToString());
        }

        // do other stuff here to initialize
        ChangeMainData();
        ChangeVehicles();

        return 0;
    }


    internal static void ChangeMainData()
    {
        MainData.Defaults.SetPublicProperty("Passenger_max_search_connections", 4); // 6

        // Station times
        MainData.Defaults.SetPublicProperty("Bus_station_time", 300); // 900
        MainData.Defaults.SetPublicProperty("Train_station_time", 600);  // 300
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
        MainData.Defaults.SetPublicProperty("City_shrink_threshold", 0.5m); // 0.3

        // Last destination is changed if its fulfillment is below that threshold
        //MainData.Defaults.SetPublicProperty("City_destination_change = 0,3(Decimal) // last destination is changed if below that
    }

    internal static void ChangeVehicles()
    {
        // Road vehicles
        //RoadVehicleEntity vehicle = MainData.Road_vehicles.Where(x => x.Name == "vehicle_antero").First();
        //vehicle.SetPublicProperty("Capacity", 20); // 3 Antero 12 => 20 // less cap than prev
        //vehicle.SetPublicProperty("Price", 175000);

        // Ships
        MainData.Ships.Where(x => x.Name == "vehicle_silver_tide").First().SetPublicProperty("Capacity", 700); // 5 Silver Tide 860 => 700
        MainData.Ships.Where(x => x.Name == "vehicle_royal_wanderer").First().SetPublicProperty("Capacity", 1460); // 6 Royal Wanderer  1360 => 1460
        MainData.Ships.Where(x => x.Name == "vehicle_sea_conqueror").First().SetPublicProperty("Capacity", 2800); // 6 Sea Conqueror 7600 => 2800 // absurd number
        MainData.Ships.Where(x => x.Name == "vehicle_wind_rider").First().SetPublicProperty("Capacity", 190); // 3 Wind Rider 250 => 190 // too many, makes next one worse

        // Trains
        MainData.Trains.Where(x => x.Name == "vehicle_class_87").First().SetPublicProperty("Capacity", 420/6); // 5 Class 87 360 => 430 // lower capacity than previous!
        MainData.Trains.Where(x => x.Name == "vehicle_apex_3").First().SetPublicProperty("Capacity", 450/6); // 6 Apex 3 390 => 450 // only Tier6 with less cap than Tier5
        MainData.Trains.Where(x => x.Name == "vehicle_gsw_5").First().SetPublicProperty("Capacity", 320/5); // 4 GSW - 5 275 => 320 // less cap than prev 

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

#pragma warning restore CA1416 // Validate platform compatibility
