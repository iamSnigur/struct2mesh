using System.Collections.Generic;
using Newtonsoft.Json;

namespace Integrations.Mojang.Internal
{
    internal record PlayerDbResponse
    {
        [JsonProperty("code")]
        public string Code;
        
        [JsonProperty("message")]
        public string Message;
        
        [JsonProperty("data")]
        public Data Data;
        
        [JsonProperty("success")]
        public bool Success;
    }

    internal record Meta
    {
        [JsonProperty("cached_at")]
        public int CachedAt;
    }

    internal record Data
    {
        [JsonProperty("player")]
        public Player Player;
    }

    internal record Player
    {
        [JsonProperty("meta")]
        public Meta Meta;
        
        [JsonProperty("username")]
        public string Username;
        
        [JsonProperty("id")]
        public string Id;
        
        [JsonProperty("raw_id")]
        public string RawId;
        
        [JsonProperty("avatar")]
        public string Avatar;
        
        [JsonProperty("skin_texture")]
        public string SkinTexture;
        
        [JsonProperty("cape_texture")]
        public string CapeTexture;
        
        [JsonProperty("properties")]
        public IReadOnlyList<Properties> Properties;
        
        [JsonProperty("name_history")]
        public IReadOnlyList<string> NameHistory;
    }

    internal record Properties
    {
        [JsonProperty("name")]
        public string Name;
        
        [JsonProperty("value")]
        public string Value;
        
        [JsonProperty("signature")]
        public string Signature;
    }
}