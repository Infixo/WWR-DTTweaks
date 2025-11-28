# Worldwide Rush Infixo's Data Tweaks
[Worldwide Rush](https://store.steampowered.com/app/3325500/Worldwide_Rush/) mod that tweaks various game parameters and internal settings.

## Features
- The configuration is read from DTTweaks.xml file. Please see the contents of this file for details of changed values.

### City level limiter
- Parameter Options.RawCityLevelLimit allows to set limit to city growth. Set to 0 (or remove) to disable the feature.

### Game parameters
- Tweaking some key game parameters like number of hops, station times, city growth thresholds, road and rail prices, etc.
- After the changes, the mod will log all game parameters.

### Vehicles
- Changing vehicles' parameters like capacity, price, speed, etc.
- There are several capacities changed mainly to make sure that higher level vehicles are considered actually better by AI. It depends on capacity.
- Also, few planes have changed capacites to match their names.
- After the changes, the mod will log main parameters for all vehicles.

### Troubleshooting
- Output messages are logged into DTTweaksLog.txt in the %TEMP% dir.
- The copy of .xml config is saved to %TEMP% dir. This allow to check if the file is properly formatted.

## Technical

### Requirements and Compatibility
- [WWR ModLoader](https://github.com/Infixo/WWR-ModLoader).
- [Harmony v2.4.1 for .net 8.0](https://github.com/pardeike/Harmony/releases/tag/v2.4.1.0). The correct dll is provided in the release files.

### Known Issues
- None atm.

### Changelog
- v0.1.2 (2025-11-28)
  - Tweaks servicing costs to make bus maintenance cheaper than a plane.
  - Technical update.
- v0.1.1 (2025-10-11)
  - Fixes few params of int type not accepting negative values.
  - Changed value to disable the city growth limiter to <= 0.
- v0.1.0 (2025-10-11)
  - Initial release.

### Support
- Please report bugs and issues on [GitHub](https://github.com/Infixo/WWR-DTTweaks).
- You may also leave comments on [Discord](https://discord.com/channels/1342565384066170964/1421898965556920342).
