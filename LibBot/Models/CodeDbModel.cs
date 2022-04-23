using System;

namespace LibBot.Models;

public class CodeDbModel
{
    public long ChatId { get; set; }
    public int Code { get; set; }
    public DateTime ExpiryDate { get; set; }
}
