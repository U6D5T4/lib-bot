using System;

namespace LibBot.Models;

public class BorrowedBook
{
    public BorrowedBook(int bookId, DateTime takenToRead)
    {
        BookId = bookId;
        TakenToRead = takenToRead;
    }

    public int BookId { get; set; }
    public DateTime TakenToRead { get; set; }
    public DateTime Returned { get; set; }
}
