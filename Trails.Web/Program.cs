using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trails.Domain;
using Trails.Web;

var  myAllowSpecificOrigins = "_myAllowSpecificOrigins";
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TrailsDb>(opt => opt.UseInMemoryDatabase("Trails"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure middleware to add X-Forwarded-For and X-Forwarded-Proto headers
// builder.Services.Configure<ForwardedHeadersOptions>(options =>
// {
//     options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
//     //accept all networks and proxies
//     options.KnownNetworks.Clear();
//     options.KnownProxies.Clear();
// });

var flTrailsUri = new Uri("https://www.fdacs.gov/Forest-Wildfire/Our-Forests/State-Forests/State-Forest-Recreation/Recreational-Activities/Hiking/Trailwalker-Program/Trailwalker-Trail-List-by-State-Forest");
builder.Services.AddSingleton<ITrailsWebScraper>(x => 
    ActivatorUtilities.CreateInstance<TrailsWebScraper>(x, flTrailsUri));

var app = builder.Build();

//app.UseForwardedHeaders();


var trails = app.MapGroup("/trails");
trails.MapGet("/", async (TrailsDb db, CancellationToken cancellationToken) => await db.Trails.ToListAsync(cancellationToken));
trails.MapPost("/sync", async (TrailsDb db, ITrailsWebScraper scraper) =>
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

var hikeLogs = app.MapGroup("/hike-logs");
hikeLogs.MapGet("/", async (TrailsDb db) =>
{
    var hikeLogs = db.HikeLogs.ToList();
    var html = string.Empty;

    foreach (var hikeLog in hikeLogs)
    {
        html += $@"
            <tr>
                <td>{hikeLog.Id}</td>
                <td>{hikeLog.Name}</td>
                <td>{hikeLog.Trail}</td>
                <td>{hikeLog.Length}</td>
            </tr>
        ";
    }

    return Results.Extensions.Html(html);
});

hikeLogs.MapPost("/", async (
    HikeLog hikeLog,
    TrailsDb db,
    CancellationToken cancellationToken) =>
{
    await db.HikeLogs.AddAsync(hikeLog, cancellationToken);
    await db.SaveChangesAsync(cancellationToken);
    
    var html = $@"
        <tr>
            <td>{hikeLog.Id}</td>
            <td>{hikeLog.Name}</td>
            <td>{hikeLog.Trail}</td>
            <td>{hikeLog.Length}</td>
        </tr>
    ";
    return Results.Extensions.Html(html);
});




// app.UseCors(myAllowSpecificOrigins);
app.Run();
