using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;

namespace Tcd.App.Core;

public interface IRecipeRepository
{
    string RecipesDirectory { get; }
    IReadOnlyList<string> ListRecipeNames();
    TcdRecipe Load(string recipeName);
    void Save(TcdRecipe recipe);
}

public sealed class JsonRecipeRepository : IRecipeRepository
{
    private readonly JsonSerializerOptions _json;

    public JsonRecipeRepository(string recipesDirectory)
    {
        RecipesDirectory = recipesDirectory ?? throw new ArgumentNullException(nameof(recipesDirectory));
        Directory.CreateDirectory(RecipesDirectory);

        _json = new JsonSerializerOptions
        {
            WriteIndented = true,
        };
        _json.Converters.Add(new JsonStringEnumConverter());
    }

    public string RecipesDirectory { get; }

    public IReadOnlyList<string> ListRecipeNames()
    {
        if (!Directory.Exists(RecipesDirectory)) return Array.Empty<string>();

        return Directory.EnumerateFiles(RecipesDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public TcdRecipe Load(string recipeName)
    {
        var path = PathFor(recipeName);
        var json = File.ReadAllText(path, Encoding.UTF8);
        var recipe = JsonSerializer.Deserialize<TcdRecipe>(json, _json);
        if (recipe == null) throw new InvalidOperationException($"Invalid recipe file: {path}");
        if (string.IsNullOrWhiteSpace(recipe.Name)) recipe.Name = recipeName;
        return recipe;
    }

    public void Save(TcdRecipe recipe)
    {
        if (recipe == null) throw new ArgumentNullException(nameof(recipe));
        if (string.IsNullOrWhiteSpace(recipe.Name)) throw new ArgumentException("Recipe.Name is required.");

        Directory.CreateDirectory(RecipesDirectory);
        var path = PathFor(recipe.Name);
        var json = JsonSerializer.Serialize(recipe, _json);
        File.WriteAllText(path, json, Encoding.UTF8);
    }

    private string PathFor(string recipeName)
    {
        if (string.IsNullOrWhiteSpace(recipeName)) throw new ArgumentNullException(nameof(recipeName));
        var safe = string.Concat(recipeName.Where(ch => !Path.GetInvalidFileNameChars().Contains(ch))).Trim();
        if (string.IsNullOrWhiteSpace(safe)) safe = "Recipe";
        return Path.Combine(RecipesDirectory, safe + ".json");
    }
}

public static class RecipePaths
{
    public static string DefaultRecipesDirectory()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(root, "TCD", "Recipes");
    }
}

