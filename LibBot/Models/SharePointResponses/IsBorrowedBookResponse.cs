using System;

namespace LibBot.Models.SharePointResponses;

public class IsBorrowedBookResponse
{
    public bool IsBorrowedBook { get; set; }
    public DateTime? TakenToRead { get; set; }
}
