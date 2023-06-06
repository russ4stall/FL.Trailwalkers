using HtmlAgilityPack;
using static System.Web.HttpUtility;

namespace Trails.Domain;

public interface ITrailsWebScraper
{
    public List<Trail> ScrapeTrails();
}

public class TrailsWebScraper : ITrailsWebScraper
{
    private readonly Uri _uri;
    public TrailsWebScraper(Uri uri)
    {
        _uri = uri;
    }

    public List<Trail> ScrapeTrails()
    {
        var trailList = new List<Trail>();
        
        var web = new HtmlWeb();
        var doc = web.Load(_uri);

        const string xpath = "//div[contains(@id, 'body-content')]//div[contains(@class, 'ezrichtext-field')]//h2";
        var stateForestNodes = doc.DocumentNode.SelectNodes(xpath);

        foreach (var node in stateForestNodes)
        {
            var forestName = HtmlDecode(node.InnerText).Trim();
            var currentNode = node.NextNonTextSibling();
                
            while (currentNode.Name == "div") // until the next forest (h2)
            {
                var trailNameNode = currentNode.FirstNonTextChild();
                var nextNode = trailNameNode.NextNonTextSibling();
                var trailPropertyList = nextNode.SelectSingleNode("ul");
                var trail = new Trail
                {
                    StateForestName = forestName,
                    Name = HtmlDecode(trailNameNode.InnerText).Trim(),
                    Length = GetTrailProperty("Length:", trailPropertyList.ChildNodes),
                    Type = GetTrailProperty("Trail Type:", trailPropertyList.ChildNodes),
                    TrailheadLocation = GetTrailProperty("Trailhead Location:", trailPropertyList.ChildNodes)
                };
                
                trailList.Add(trail);
                currentNode = currentNode.NextSibling;
            }
        }
        
        return trailList;
    }

    private static string? GetTrailProperty(string property, HtmlNodeCollection propertyNodes)
    {
        foreach (var node in propertyNodes)
        {
            var text = node.InnerText;
            if (text.Contains(property))
                return HtmlDecode(text[property.Length..]).Trim();
        }

        return null;
    }
}