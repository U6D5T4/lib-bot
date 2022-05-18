using System;

namespace LibBot.Models.SharePointResponses;

public class BookDataResponse
{
    public string Title { get; set; }
    public int Id { get; set; }
    public int? BookReaderId { get; set; }
    public DateTime? TakenToRead { get; set; }
    public Technology Technology { get; set; }
    public DateTime Created { get; set; }
}