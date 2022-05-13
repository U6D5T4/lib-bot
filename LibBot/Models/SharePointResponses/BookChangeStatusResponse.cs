using System;

namespace LibBot.Models.SharePointResponses;

public class BookChangeStatusResponse
{
    public bool IsBorrowedBook { get; set; }
    public DateTime? TakenToRead { get; set; }
}
