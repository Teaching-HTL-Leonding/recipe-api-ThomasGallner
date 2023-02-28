using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var recipes = new ConcurrentDictionary<int, Recipe>();
var nextId = 0;

app.MapGet("/", () => "Hello World!");

app.MapGet("/recipes", () => recipes.Values);

app.MapGet("/recipes/filter-by-title/{filter}", (string filter) => {
    var filterRecipes = new List<Recipe>();

    foreach(Recipe r in recipes.Values){
        if(r.Title.Contains(filter) || r.Description.Contains(filter)){
            filterRecipes.Add(r);
        }
    }

    return Results.Ok(filterRecipes);
});

app.MapGet("/recipes/filter-by-ingredient/{filter}", (string filter) => {
    var filterRecipes = new List<Recipe>();

    foreach(Recipe r in recipes.Values){
        foreach(Ingredient i in r.Ingredients){
            if (i.Name.Contains(filter)){
                filterRecipes.Add(r);
            }
        }
    }

    return Results.Ok(filterRecipes);
});

app.MapPost("/recipes", (CreateOrUpdateRecipeDto newRecipe) =>
{
    // New id for recipe (cause of server interal id generation)
    var newId = Interlocked.Increment(ref nextId);

    // Convert DTO to persistence model
    var recipeToAdd = new Recipe
    {
        Id = newId,
        Title = newRecipe.Title,
        Description = newRecipe.Description,
        ImageLink = newRecipe.ImageLink,
        Ingredients = newRecipe.Ingredients,
    };

    // Add recipe to dict
    if (!recipes.TryAdd(newId, recipeToAdd))
    {
        // This should never happen (server internal auto increment id)
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }

    // Return 201 (Created)
    return Results.Created($"recipes/{newId}", recipeToAdd);
});

app.MapDelete("/recipes/{id}", (int id) =>
{
    if (!recipes.Remove(id, out var _))
    {
        return Results.NotFound();
    }

    return Results.NoContent();
});

app.MapPut("/recipes/{id}", (int id, CreateOrUpdateRecipeDto updatedRecipe) =>
{
   if (!recipes.TryGetValue(id, out Recipe? recipe))
    {
        return Results.NotFound();
    }

    // code down below not perfect (later on -> database -> not a problem anymore)
    
    recipe.Title = updatedRecipe.Title;
    recipe.Description = updatedRecipe.Description;
    recipe.ImageLink = updatedRecipe.ImageLink;
    recipe.Ingredients = updatedRecipe.Ingredients;

    return Results.Ok(recipe);
});

app.Run();


class Recipe
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string ImageLink { get; set; } = "";
    public List<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
}

class Ingredient
{
    public string Name { get; set; } = "";
    public string UnitOfMeasure { get; set; } = "";
    public int Quantity { get; set; }
}

record CreateOrUpdateRecipeDto(string Title, string Description, string ImageLink, List<Ingredient> Ingredients);