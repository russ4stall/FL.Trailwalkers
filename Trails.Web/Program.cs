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
hikeLogs.MapGet("/", async (
    TrailsDb db,
    CancellationToken cancellationToken) =>
{
    var hikeLogs = await db.HikeLogs.ToListAsync(cancellationToken);
    var html = string.Empty;

    foreach (var hikeLog in hikeLogs)
    {
        html += $@"
            <tr>
                <td>{hikeLog.Id}</td>
                <td><a href=""https://trails.apps.fourscorepicks.com/hike-logs/{hikeLog.Id}"">{hikeLog.Name}</a></td>
                <td>{hikeLog.Trail}</td>
                <td>{hikeLog.Length}</td>
            </tr>
        ";
    }

    return Results.Extensions.Html(html);
});

hikeLogs.MapGet("/{id:long}", async (long id, TrailsDb db, CancellationToken cancellationToken) =>
{
    var hikeLog = await db.HikeLogs.FirstOrDefaultAsync(x => x.Id == id);
    var html = $@"
        <dl>
          <dt>Name: {hikeLog.Name}</dt>
          <dd>Trail: {hikeLog.Trail}</dd>
          <dt>Length: {hikeLog.Trail}</dt>
        </dl>
        ";
    return Results.Extensions.Html(html);
});

hikeLogs.MapPost("/", async (
    HttpContext ctx,
    TrailsDb db,
    CancellationToken cancellationToken) =>
{
    decimal.TryParse(ctx.Request.Form["length"], out var length);
    var hikeLog = new HikeLog()
    {
        Name = ctx.Request.Form["name"],
        Trail = ctx.Request.Form["trail"],
        Length = length
    };
    
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
