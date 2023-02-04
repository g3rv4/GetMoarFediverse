namespace GetMoarFediverse.Responses;

public class TagResponse
{
    public string[] OrderedItems { get; }
    
    public TagResponse(string[] orderedItems)
    {
        OrderedItems = orderedItems;
    }
}