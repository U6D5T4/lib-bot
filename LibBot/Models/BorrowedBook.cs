using System;

namespace LibBot.Models;

public class BorrowedBook
{
    public BorrowedBook(int bookId, DateTime takenToRead, string title)
    {
        BookId = bookId;
        TakenToRead = takenToRead;
        Title = title;
    }

    public int BookId { get; set; }
    public DateTime TakenToRead { get; set; }
    public DateTime Returned { get; set; }
    public string Title { get; set; }
}
