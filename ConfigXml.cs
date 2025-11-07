using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Xml.Serialization;
using Utilities;

namespace DTTweaks;


// From Gemini - for future
public class ValidationHelper
{
    public static void Validate(object obj)
    {
        var context = new ValidationContext(obj, serviceProvider: null, items: null);
        Validator.ValidateObject(obj, context, validateAllProperties: true);
    }
}


[XmlRoot("Configuration")]
public class ConfigurationXml
{
    [XmlElement("Options")]
    public OptionsXml? Options { get; set; }

    [XmlElement("Defaults")]
    public DefaultsXml? Defaults { get; set; }

    [XmlArray("Vehicles")]
    [XmlArrayItem("Vehicle")]
    public List<VehicleXml> Vehicles { get; set; } = [];
}


public class OptionsXml // Child element: <Options>
{
    [XmlElement("Param")] // The <Param> elements are children of <Options>
    public List<ParamXml> Params { get; set; } = [];

    public ParamXml? TryGet(string name)
    {
        foreach (ParamXml param in Params)
            if (param.Name == name)
                return param;
        return null;
    }
}


public class DefaultsXml // Child element: <Defaults>
{
    [XmlElement("Param")] // The <Param> elements are children of <Defaults>
    public List<ParamXml> Params { get; set; } = [];
}


// Complex type for <Vehicle name="..." type="...">
public class VehicleXml
{
    [XmlAttribute("name")]
    public string? Name { get; set; }

    [XmlAttribute("type")]
    public string? Type { get; set; }

    [XmlElement("Param")] // <Vehicle> contains child <Param> elements
    public List<ParamXml> Params { get; set; } = [];

    public bool IsOK => !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Type);
}


public class ParamXml
{
    [XmlAttribute("name")]
    public string? Name { get; set; }

    [XmlAttribute("type")]
    public string? Type { get; set; } // e.g. int, long, etc.

    [XmlText] // maps the element's inner content (e.g., "10") to the property
    public string? Value { get; set; } // Value stored as the inner text of the <Data> element

    public bool IsOK => !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Type) && !string.IsNullOrEmpty(Value);


    // Converts the string Value to the C# type specified by Type
    public object GetValue()
    {
        // NumberStyles.None enforces only digits (no sign!!!).
        // Leading sign must be explicitly allowed, however only few params use negative values (all ints).
        // InvariantCulture for consistent parsing (no thousands separators, dot for decimal).
        return Type switch
        {
            "byte" => byte.Parse(Value ?? "0", NumberStyles.None, CultureInfo.InvariantCulture),
            "int" => int.Parse(Value ?? "0", NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture),
            "long" => long.Parse(Value ?? "0", NumberStyles.None, CultureInfo.InvariantCulture),
            "decimal" => decimal.Parse(Value ?? "0", NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture),// Use NumberStyles.Any for decimal to handle various formats if necessary
            _ => Value ?? String.Empty, // Return the raw string or throw an exception for unknown types
        };
    }

    public override string ToString()
    {
        return $"{Name} ({Type}): {Value}";
    }
}


public static class ConfigToolXml
{
    private static string _configFileName = ".xml";

    private static ConfigurationXml? _config;
    public static ConfigurationXml? Config { get { return _config; } }

    /// <summary>
    /// Loads prefab config data from a file in the mod directory.
    /// Settings are set to null id there is any problem during loading.
    /// </summary>
    public static ConfigurationXml? LoadConfig(string modName, string modDir)
    {
        //Mod.Log($"{assetPath}");
        //Mod.Log($"{Path.GetDirectoryName(assetPath)}");
        try
        {
            //string configDir = Path.Combine(Path.GetDirectoryName(modDir));
            //if (Mod.setting.UseLocalConfig)
            //{
            //string appDataDir = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            //configDir = Path.Combine(appDataDir, @"LocalLow\Colossal Order\Cities Skylines II\Mods", Mod.modAsset == null ? "RealCity" : Mod.modAsset.name);
            //}
            _configFileName = modName + ".xml";
            string configFilePath = Path.Combine(modDir, _configFileName);
            Log.Write($"Loading configuration from {configFilePath}.");

            XmlSerializer serializer = new XmlSerializer(typeof(ConfigurationXml));
            using (FileStream fs = new FileStream(configFilePath, FileMode.Open))
            {
                _config = serializer.Deserialize(fs) as ConfigurationXml;
            }
            SaveConfig(); // debug
            return _config;
        }
        catch (Exception e)
        {
            Log.Write($"ERROR: Cannot load configuration, exception {e.Message}");
            return null;
        }
    }
    
    public static void SaveConfig()
    {
        try
        {
            string dumpFile = Path.Combine(Path.GetTempPath(), _configFileName);

            //string appDataDir = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            //string dumpDir = Path.Combine(appDataDir, @"LocalLow\Colossal Order\Cities Skylines II\Mods", Mod.modAsset == null ? "RealCity" : Mod.modAsset.name);
            //string dumpFile = Path.Combine(dumpDir, _dumpFileName);
            XmlSerializer serializer = new XmlSerializer(typeof(ConfigurationXml));
            using (FileStream fs = new FileStream(dumpFile, FileMode.Create))
            {
                serializer.Serialize(fs, Config);
            }
            Log.Write($"Configuration saved to file {dumpFile}.");
        }
        catch (Exception e)
        {
            Log.Write($"ERROR: Cannot save configuration, exception {e.Message}.");
        }
    }
}
