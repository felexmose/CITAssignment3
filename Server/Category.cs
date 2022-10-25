using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace server
{
    public class Category
    {
        [JsonPropertyName("cid")]
        public int? Cid { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
