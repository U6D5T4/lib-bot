using System;

namespace LibBot.Models.SharePointRequests;

public class BookBorrowRequest
{
    public int BookReaderId { get; set; }
    public string BookReaderStringId { get; set; }
    public  DateTime TakenToRead { get; set; }
    public DateTime Modified { get; set; }
    public int EditorId { get; set; }
}
