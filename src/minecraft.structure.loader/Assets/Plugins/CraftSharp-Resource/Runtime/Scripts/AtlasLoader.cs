using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;

namespace CraftSharp.Resource
{
    public class AtlasLoader
    {
        private SpriteAtlas _spriteAtlas;
        private readonly Dictionary<string, Sprite> _spritesCache = new();
        private Texture2D _atlasTexture;

        public async UniTask Init()
        {
            ResourceRequest request = Resources.LoadAsync<SpriteAtlas>("vanilla/minecraft/textures/block_atlas");
            await request.ToUniTask();
            
            _spriteAtlas = request.asset as SpriteAtlas;
        }

        public void Reset()
        {
            _spritesCache.Clear();
        }
    
        public Sprite GetTextureInfo(string name)
        {
            name = name.Replace("block/", string.Empty);
            
            if (_spritesCache.TryGetValue(name, out Sprite sprite))
                return sprite;
        
            sprite = _spriteAtlas.GetSprite(name);
            _spritesCache[name] = sprite;

            return sprite;
        }
        
        public Texture2D GetAtlasTexture()
        {
            if (_atlasTexture is not null)
                return _atlasTexture;

            var sprites = new Sprite[_spriteAtlas.spriteCount];
            _spriteAtlas.GetSprites(sprites);

            _atlasTexture = sprites[0].texture;
            return _atlasTexture;
        }
    }
}