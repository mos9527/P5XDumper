using AresChroniclesDumper.FileSystem;

namespace AresChroniclesDumper;

static class Program
{
    private static string OutputFolder;
    private static string ClientFolder;

    public static void Main(string[] argv)
    {
        if (argv.Length == 2)
        {
            OutputFolder = argv[0];
            ClientFolder = argv[1];
        } else
        {
            Console.WriteLine("Usage: P5XDumper [OutputFolder] [ClientFolder]");
            return;
        }

        Console.WriteLine($"Output Folder: {OutputFolder}");
        Console.WriteLine($"Client Folder: {ClientFolder}");

        if (CheckForRequirements())
        {
            DumpInnerPackageAssets();
            // DumpOuterPackageAssets();
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadLine();
    }

    private static bool CheckForRequirements()
    {

        if (!File.Exists(Path.Combine(ClientFolder, "bin", ZeusFileSystem.VFileIndex)))
        {
            Console.WriteLine($"[ERROR] Unable to locate the \"{ZeusFileSystem.VFileIndex}\" file.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Inner Package Assets refer to those contained
    /// within the base game's _vfileContent archives
    /// </summary>
    private static void DumpInnerPackageAssets()
    {
        using var fileSystem = new ZeusFileSystem(Path.Combine(ClientFolder, "bin"));

        fileSystem.LoadVFileEntrysFromFB();

        foreach (var fileEntry in fileSystem.GetFileEntries())
        {
            using var stream = fileSystem.OpenReadStream(fileEntry);

            if (stream == null)
                continue;

            Console.WriteLine($"Dumping InnerPackage {fileEntry}...");
            DumpAsset(fileEntry, "InnerPackage", stream);
        }
    }

    /// <summary>    
    /// Outer Package Assets refer to those downloaded
    /// via updates and hotfixes
    /// </summary>
    private static void DumpOuterPackageAssets()
    {
        var bundlesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low",
            "PerfectWorld",
            "아레스 크로니클",
            "OuterPackage",
            "Bundles");

        if (!Directory.Exists(bundlesPath))
            return;

        foreach (var bundle in Directory.GetFiles(bundlesPath, "*.unity3d", SearchOption.AllDirectories))
        {
            using var stream = File.OpenRead(bundle);

            var fileEntry = bundle[bundle.IndexOf("Bundles")..];
            Console.WriteLine($"Dumping OuterPackage {fileEntry}...");
            DumpAsset(fileEntry, "OuterPackage", stream);
        }
    }

    /// <summary>
    /// Copies an asset to the <see cref="OutputFolder"/> and
    /// converts it to a normal Asset Bundle if required
    /// </summary>
    /// <param name="fileEntry"></param>
    /// <param name="stream"></param>
    private static void DumpAsset(string fileEntry, string subfolder, Stream stream)
    {
        var filename = Path.Combine(OutputFolder, subfolder, fileEntry);
        var directory = Path.GetDirectoryName(filename);

        Directory.CreateDirectory(directory);

        using var fs = File.Create(filename);
        stream.CopyTo(fs);
        fs.Flush();

        if (BundleConverter.IsAresBundle(fs))
        {
            Console.WriteLine($"Converting Ares Bundle {fileEntry} to Unity Bundle...");
            BundleConverter.ConvertAresToUnity(fs);
        }
    }
}