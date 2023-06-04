using HtmlAgilityPack;

namespace Trails.Domain;

public static class HtmlNodeExtensions
{
    public static HtmlNode NextNonTextSibling(this HtmlNode node)
    {
        do node = node.NextSibling;
        while (node.NodeType == HtmlNodeType.Text);
        return node;
    }
    
    public static HtmlNode FirstNonTextChild(this HtmlNode node)
    {
        node = node.FirstChild;
        do node = node.NextNonTextSibling();
        while (node.NodeType == HtmlNodeType.Text);
        return node;
    }
}