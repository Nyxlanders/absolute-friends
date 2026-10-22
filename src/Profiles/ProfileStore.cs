using System.Runtime.Serialization.Json;
using AbsoluteFriends.Diagnostics;

namespace AbsoluteFriends.Profiles;

internal static class ProfileStore
{
    private static readonly DataContractJsonSerializer _serializer = new(typeof(Profile), new DataContractJsonSerializerSettings
    {
        UseSimpleDictionaryFormat = true,
    });

    internal static string FolderPath => Path.Combine(Storage.RootPath, "profiles");

    private static string TrashPath => Path.Combine(FolderPath, "Trash");

    internal static List<Profile> Load()
    {
        List<Profile> profiles = [];
        HashSet<string> ids = [];

        try
        {
            Directory.CreateDirectory(FolderPath);

            foreach (var path in Directory.GetFiles(FolderPath, "*.json"))
            {
                try
                {
                    using FileStream stream = File.OpenRead(path);

                    if (_serializer.ReadObject(stream) is Profile profile && IsValid(profile) && ids.Add(profile.Id))
                    {
                        profile.CreatedAt = Timestamp(profile.CreatedAt, File.GetCreationTimeUtc(path));
                        profile.ModifiedAt = Timestamp(profile.ModifiedAt, File.GetLastWriteTimeUtc(path));
                        profile.Addons ??= [];

                        foreach (var addon in profile.Addons.Values)
                        {
                            addon.Options ??= [];
                        }

                        profiles.Add(profile);
                    }
                    else
                    {
                        Reporter.LogWarning($"Could not load profile {Path.GetFileName(path)}");
                    }
                }
                catch (Exception exception)
                {
                    Reporter.LogError($"Could not load profile {Path.GetFileName(path)}", exception);
                }
            }
        }
        catch (Exception exception)
        {
            Reporter.LogError("Could not open the profile folder", exception);
        }

        return [.. profiles.OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)];
    }

    internal static void Save(Profile profile)
    {
        if (!IsValid(profile))
        {
            throw new InvalidDataException("The profile has invalid identifying information");
        }

        string path = Path.Combine(FolderPath, profile.Id + ".json");

        Storage.Write(path, stream => _serializer.WriteObject(stream, profile));
    }

    internal static void Remove(Profile profile)
    {
        if (!IsValid(profile))
        {
            throw new InvalidDataException("The profile has invalid identifying information");
        }

        string path = Path.Combine(FolderPath, profile.Id + ".json");

        if (!File.Exists(path))
        {
            return;
        }

        Directory.CreateDirectory(TrashPath);

        string destination = Path.Combine(TrashPath, $"{profile.Id}-{DateTime.UtcNow:yyyyMMddHHmmssfff}.json");

        File.Move(path, destination);
    }

    private static bool IsValid(Profile profile)
    {
        return profile.FormatVersion == 1
            && Guid.TryParseExact(profile.Id, "N", out _)
            && !string.IsNullOrWhiteSpace(profile.Name)
            && profile.Name.Length <= 80
            && profile.Addons != null
            && profile.Addons.Values.All(addon => addon != null);
    }

    private static string Timestamp(string value, DateTime fallback)
    {
        return DateTimeOffset.TryParse(value, out _) ? value : fallback.ToString("O");
    }
}
