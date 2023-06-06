using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Trails.Domain;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<TrailsDb>(opt => opt.UseInMemoryDatabase("Trails"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure middleware to add X-Forwarded-For and X-Forwarded-Proto headers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    //accept all networks and proxies
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var flTrailsUri = new Uri("https://www.fdacs.gov/Forest-Wildfire/Our-Forests/State-Forests/State-Forest-Recreation/Recreational-Activities/Hiking/Trailwalker-Program/Trailwalker-Trail-List-by-State-Forest");
builder.Services.AddSingleton<ITrailsWebScraper>(x => 
    ActivatorUtilities.CreateInstance<TrailsWebScraper>(x, flTrailsUri));

var app = builder.Build();

app.UseForwardedHeaders();

app.MapGet("/trails", async (TrailsDb db) => await db.Trails.ToListAsync());

app.MapGet("/test", async () => {

    using (var client = new HttpClient())
    {
        HttpResponseMessage response = new HttpResponseMessage();

        response = await client.GetAsync("https://www.google.com/").ConfigureAwait(false);

        return response.Content.ReadAsStringAsync().Result;
    }
});

app.MapPost("/trails/sync", async (TrailsDb db, ITrailsWebScraper scraper) =>
{
    List<Trail> trails;
    try
    {
        trails = scraper.ScrapeTrails();

        db.Trails.AddRange(trails);
        await db.SaveChangesAsync();
    }
    catch (Exception e)
    {
        return Results.Problem(e.Message, String.Empty, 500);
    }
    
    
    return Results.Ok($"Added {trails.Count} trails to the database.");
});

app.Run();
