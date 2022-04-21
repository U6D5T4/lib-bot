using System;

namespace LibBot.Models;

public class CodeDbModel
{
    public int ChatId { get; set; }
    public int Code { get; set; }
    public DateTime ExpiryDate { get; set; }
}
