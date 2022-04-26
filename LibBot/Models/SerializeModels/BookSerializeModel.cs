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

    public partial class D
    {
        [JsonProperty("results")]
        public List<Result> Results { get; set; }

        [JsonProperty("__next")]
        public Uri Next { get; set; }
    }

    public partial class Result
    {
        [JsonProperty("__metadata")]
        public Metadata Metadata { get; set; }

        [JsonProperty("Title")]
        public string Title { get; set; }
}

    public partial class Metadata
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }
    }

    public partial class Book
    {
        public static Book FromJson(string json) => JsonConvert.DeserializeObject<Book>(json, Converter.Settings);

        public static List<BookDataResponse> GetBookDataResponse(Book data)
        {
            List<BookDataResponse> books = new List<BookDataResponse>();
            var items = data.D.Results;

            foreach (var item in items)
            {
                BookDataResponse book = new BookDataResponse();
                book.Title = item.Title;
                book.Id = item.Metadata.Id;
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

