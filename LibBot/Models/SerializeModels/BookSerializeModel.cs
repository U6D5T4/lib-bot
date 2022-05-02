using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LibBot.Models.SharePointResponses;

public partial class Book
{
    [JsonProperty("d")]
    public D D { get; set; }
}

public class D
{
    [JsonProperty("results")]
    public List<Result> Results { get; set; }

    [JsonProperty("__next")]
    public Uri Next { get; set; }

    [JsonProperty("GetContextWebInformation")]
    public GetContextWebInformation GetContextWebInformation { get; set; }
}

public class GetContextWebInformation
{
    [JsonProperty("FormDigestValue")]
    public string FormDigestValue { get; set; }
}

public class Result
{
    [JsonProperty("Title")]
    public string Title { get; set; }

    [JsonProperty("id")]
    public int Id { get; set; }
}

public partial class Book
{
    public static Book FromJson(string json) => JsonConvert.DeserializeObject<Book>(json, Converter.Settings);
    public static string GetFormDigestValue(Book data) => data.D.GetContextWebInformation.FormDigestValue;

    public static List<BookDataResponse> GetBookDataResponse(Book data)
    {
        List<BookDataResponse> books = new List<BookDataResponse>();
        var items = data.D.Results;

        foreach (var item in items)
        {
            BookDataResponse book = new BookDataResponse();
            book.Title = item.Title;
            book.Id = item.Id;
            books.Add(book);
        }

        return books;
    }
}

public static class Serialize
{
    public static string ToJson(this Book self) => JsonConvert.SerializeObject(self, Converter.Settings);
}

internal static class Converter
{
    public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None,
        Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
    };
}

