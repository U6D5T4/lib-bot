using System;

namespace LibBot.Models.SharePointRequests;

public class ChangeBookStatusRequest
{
    public int? BookReaderId { get; set; }
    public string BookReaderStringId { get; set; }
    public  DateTime? TakenToRead { get; set; }
    public DateTime Modified { get; set; }
    public int EditorId { get; set; }

    public ChangeBookStatusRequest(int? bookReaderId, int edtorId, DateTime? takenToRead, DateTime modified)
    {
        BookReaderId = bookReaderId;
        BookReaderStringId = bookReaderId.ToString();
        EditorId = edtorId;
        TakenToRead = takenToRead;
        Modified = modified;
    }
}
