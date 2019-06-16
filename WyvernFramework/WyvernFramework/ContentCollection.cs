using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using VulkanCore;

namespace WyvernFramework
{
    /// <summary>
    /// Provides content loading systems
    /// </summary>
    public class ContentCollection
    {
        public struct ContentItem
        {
            public string Key;
            public Type Type;
            public string Path;
        }

        /// <summary>
        /// The content root directory
        /// </summary>
        public static string ContentRoot { get; } = Path.Combine("..", "..", "..", "Content");

        /// <summary>
        /// The shaders root directory
        /// </summary>
        public static string ShaderRoot { get; } = Path.Combine(ContentRoot, "Shaders");

        /// <summary>
        /// The textures root directory
        /// </summary>
        public static string TextureRoot { get; } = Path.Combine(ContentRoot, "Textures");

        /// <summary>
        /// The Graphics object associated with the object
        /// </summary>
        public Graphics Graphics { get; }

        /// <summary>
        /// Whether the content is loaded or not
        /// </summary>
        public bool Loaded { get; private set; }

        private List<ContentItem> ContentItems { get; } = new List<ContentItem>();

        /// <summary>
        /// Get all content that should be loaded
        /// </summary>
        public IEnumerable<ContentItem> AllContent => ContentItems;

        private Dictionary<string, object> LoadedContent { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Get all currently loaded content
        /// </summary>
        public IEnumerable<KeyValuePair<string, object>> AllLoadedContent => LoadedContent;

        /// <summary>
        /// Get a content item that was loaded with Load()
        /// Same as calling GetLoadedContent
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object this[string key] => GetLoadedContent<object>(key);

        public ContentCollection(Graphics graphics)
        {
            Graphics = graphics;
        }

        ~ContentCollection()
        {
            if (Loaded)
                Unload();
            Clear();
        }

        /// <summary>
        /// Add a content item to the collection
        /// </summary>
        /// <param name="key"></param>
        /// <param name="type"></param>
        /// <param name="path"></param>
        public void Add<T>(string key, string path)
        {
            if (Loaded)
                throw new InvalidOperationException("Cannot add content while the ContentCollection is loaded");
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (key.Length == 0)
                throw new ArgumentException("key.Length must be > 0", nameof(key));
            if (path is null)
                throw new ArgumentNullException(nameof(path));
            if (path.Length == 0)
                throw new ArgumentException("path.Length must be > 0", nameof(path));
            if (!(ContentItems.FirstOrDefault(e => e.Key == key).Key is null))
                throw new ArgumentException("Content with given key already exists", nameof(key));
            ContentItems.Add(new ContentItem { Key = key, Type = typeof(T), Path = path });
        }

        /// <summary>
        /// Remove a content item from the collection
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key)
        {
            if (Loaded)
                throw new InvalidOperationException("Cannot remove content while the ContentCollection is loaded");
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (key.Length == 0)
                throw new ArgumentException("key.Length must be > 0", nameof(key));
            var ind = ContentItems.FindIndex(e => e.Key == key);
            if (ind < 0)
                throw new ArgumentException($"key \"{key}\" does not refer to any content", nameof(key));
            ContentItems.RemoveAt(ind);
        }

        /// <summary>
        /// Remove all content from the collection
        /// </summary>
        public void Clear()
        {
            if (Loaded)
                throw new InvalidOperationException("Cannot clear content while the ContentCollection is loaded");
            ContentItems.Clear();
        }

        /// <summary>
        /// Load all content
        /// </summary>
        internal void Load()
        {
            if (Loaded)
                throw new InvalidOperationException("The ContentCollection is already loaded");
            Loaded = true;
            foreach (var content in AllContent)
            {
                LoadContentItem(content);
            }
        }

        /// <summary>
        /// Unload all content
        /// </summary>
        internal void Unload()
        {

            if (Loaded)
                throw new InvalidOperationException("The ContentCollection is already loaded");
            foreach (var content in AllLoadedContent)
            {
                UnloadContentItem(content);
            }
            Loaded = false;
        }

        private void LoadContentItem(ContentItem item)
        {
            object loaded = null;
            switch (item.Type.Name)
            {
                default:
                    throw new NotImplementedException($"No case for loading content of type {item.Type.Name}");
                case nameof(ShaderModule):
                    loaded = LoadShaderModule(item.Path);
                    break;
                case nameof(Texture2D):
                    loaded = LoadTexture(item.Key, item.Path);
                    break;
            }
            if (loaded is null)
                throw new NotImplementedException($"{nameof(loaded)} was never set for content {item.Key} of type {item.Type.Name}");
            LoadedContent.Add(item.Key, loaded);
        }

        private void UnloadContentItem(KeyValuePair<string, object> item)
        {
            LoadedContent.Remove(item.Key);
            if (item.Value is IDisposable disposable)
                disposable.Dispose();
        }

        /// <summary>
        /// Load a shader module immediately
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public ShaderModule LoadShaderModule(string path)
        {
            var actualPath = Path.Combine(ShaderRoot, path);
            return Graphics.Device.CreateShaderModule(new ShaderModuleCreateInfo(
                    File.ReadAllBytes(actualPath)
                ));
        }

        /// <summary>
        /// Load a texture immediately
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Texture2D LoadTexture(string name, string path)
        {
            var actualPath = Path.Combine(TextureRoot, path);
            return Texture2D.FromFile(name, Graphics, actualPath);
        }

        /// <summary>
        /// Get a content item that was loaded with Load()
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public T GetLoadedContent<T>(string key)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (key.Length == 0)
                throw new ArgumentException("key.Length must be > 0", nameof(key));
            if (LoadedContent.TryGetValue(key, out var value))
            {
                if (value is T ret)
                    return ret;
                throw new InvalidOperationException($"Content with key \"{key}\" is not type {typeof(T)}");
            }
            throw new ArgumentException($"Content with key \"{key}\" is not loaded", nameof(key));
        }
    }
}
