using Trails.Domain;

Console.WriteLine("Here we go!");

var uri = new Uri("https://www.fdacs.gov/Forest-Wildfire/Our-Forests/State-Forests/State-Forest-Recreation/Recreational-Activities/Hiking/Trailwalker-Program/Trailwalker-Trail-List-by-State-Forest");
var scraper = new TrailsWebScraper(uri);
Console.WriteLine($"Trying to scrape trails from: {uri}");

var trails = scraper.ScrapeTrails();

foreach (var trail in trails)
{
    Console.WriteLine($"Trail: {trail.Name}");
    Console.WriteLine($"Forest: {trail.StateForestName}");
    Console.WriteLine($"Length: {trail.Length}");
    Console.WriteLine($"Type: {trail.Type}");
    Console.WriteLine($"Trailhead Location: {trail.TrailheadLocation}");
    Console.WriteLine("-----");
}
