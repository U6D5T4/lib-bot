using System;

namespace LibBot.Models;

public interface ICodeDbModel
{
    public int ChatId { get; set; }
    public int Code { get; set; }
    public DateTime ExpiryDate { get; set; }
}
