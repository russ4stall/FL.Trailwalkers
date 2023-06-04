using Microsoft.EntityFrameworkCore;
using Trails.Domain;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<TrailsDb>(opt => opt.UseInMemoryDatabase("Trails"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var flTrailsUri = new Uri("https://www.fdacs.gov/Forest-Wildfire/Our-Forests/State-Forests/State-Forest-Recreation/Recreational-Activities/Hiking/Trailwalker-Program/Trailwalker-Trail-List-by-State-Forest");
builder.Services.AddSingleton<ITrailsWebScraper>(x => 
    ActivatorUtilities.CreateInstance<TrailsWebScraper>(x, flTrailsUri));

var app = builder.Build();

app.MapGet("/trails", async(TrailsDb db) => await db.Trails.ToListAsync());

app.MapPost("/trails/sync", async (TrailsDb db, ITrailsWebScraper scraper) =>
{
    var trails = scraper.ScrapeTrails();

    db.Trails.AddRange(trails);
    await db.SaveChangesAsync();
    
    return Results.Ok($"Added {trails.Count} trails to the database.");
});

app.Run();
