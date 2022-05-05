namespace LibBot.Models.SharePointResponses;

public class BookDataResponse
{
    public string Title { get; set; }
    public int Id { get; set; }
    public int? BookReaderId { get; set; }
    public Technology Technology { get; set; }
}