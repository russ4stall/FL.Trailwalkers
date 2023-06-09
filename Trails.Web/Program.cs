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

var hikeLogsApi = app.MapGroup("api/hike-logs");
hikeLogsApi.MapGet("/", async (
    TrailsDb db,
    CancellationToken cancellationToken) => await db.HikeLogs.ToListAsync(cancellationToken));

hikeLogsApi.MapGet("/{id:long}", async (long id, TrailsDb db, CancellationToken cancellationToken) =>
{
    var hikeLog = await db.HikeLogs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    return hikeLog is null ? Results.NotFound(id) : Results.Ok(hikeLog);
});

hikeLogsApi.MapPut("/{id:long}", async (long id, HikeLog hikeLog, TrailsDb db, CancellationToken cancellationToken) =>
{
    var updateHikeLog = await db.HikeLogs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    updateHikeLog.Length = hikeLog.Length;
    updateHikeLog.Name = hikeLog.Name;
    updateHikeLog.Trail = hikeLog.Trail;
    
    await db.SaveChangesAsync(cancellationToken);
    
    return Results.Ok(updateHikeLog);
});

hikeLogsApi.MapDelete("/{id:long}", async (long id, TrailsDb db, CancellationToken cancellationToken) =>
{
    if (await db.HikeLogs.FindAsync(id) is HikeLog hikeLog)
    {
        db.HikeLogs.Remove(hikeLog);
        await db.SaveChangesAsync();
        return Results.Ok();
    }

    return Results.NotFound();
});

hikeLogsApi.MapPost("/", async (
    HikeLog hikeLog,
    TrailsDb db,
    CancellationToken cancellationToken) =>
{
    await db.HikeLogs.AddAsync(hikeLog, cancellationToken);
    await db.SaveChangesAsync(cancellationToken);
    return hikeLog; // tried Results.Created() however it returned an empty body with status of 200 (instead of 201)...
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
            <tr hx-get=""https://trails.apps.fourscorepicks.com/hike-logs/{hikeLog.Id}""
                hx-target=""#HikeLogsTable""
                hx-swap=""outerHTML""
                hx-push-url=""true"">
                <td>{hikeLog.Id}</td>
                <td>{hikeLog.Name}</td>
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
        <button class=""btn btn-secondary"" type=""button"" onclick=""javascript:history.back()"">Show All</button>
        <dl>
          <dt>Name</dt>
          <dd>{hikeLog.Name}</dd>
          <dt>Trail</dt>
          <dd>{hikeLog.Trail}</dd>
          <dt>Length</dt>
          <dd>{hikeLog.Trail}</dd>
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
        <tr hx-get=""https://trails.apps.fourscorepicks.com/hike-logs/{hikeLog.Id}""
            hx-target=""#HikeLogsTable""
            hx-swap=""outerHTML""
            hx-push-url=""true"">
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
