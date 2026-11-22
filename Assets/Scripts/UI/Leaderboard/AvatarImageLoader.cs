using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ChasingLight.UI.Leaderboard
{
    /// <summary>
    /// Tiện ích tải và cache ảnh Avatar bất đồng bộ từ URL bằng UnityWebRequestTexture.
    /// Có cache RAM để tái sử dụng ảnh khi cuộn danh sách và tự sinh placeholder texture nếu không có ảnh.
    /// </summary>
    public static class AvatarImageLoader
    {
        private static readonly Dictionary<string, Sprite> _cache = new();
        private static Sprite _defaultPlaceholder;

        /// <summary>
        /// Sprite placeholder mặc định (hình tròn xám mềm mại) tạo bằng code nếu không có sprite từ asset.
        /// </summary>
        public static Sprite DefaultPlaceholder
        {
            get
            {
                if (_defaultPlaceholder == null)
                {
                    _defaultPlaceholder = CreateCircularPlaceholder(64, new Color(0.28f, 0.32f, 0.40f, 1f));
                }
                return _defaultPlaceholder;
            }
        }

        /// <summary>
        /// Tải avatar từ <paramref name="url"/>, nếu đã có trong cache sẽ trả về ngay.
        /// </summary>
        public static IEnumerator LoadAvatar(string url, Action<Sprite> onLoaded, Sprite fallback = null)
        {
            Sprite placeholder = fallback != null ? fallback : DefaultPlaceholder;

            if (string.IsNullOrWhiteSpace(url))
            {
                onLoaded?.Invoke(placeholder);
                yield break;
            }

            if (_cache.TryGetValue(url, out var cachedSprite) && cachedSprite != null)
            {
                onLoaded?.Invoke(cachedSprite);
                yield break;
            }

            using var req = UnityWebRequestTexture.GetTexture(url);
            req.timeout = 10;

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                // Tải thất bại -> dùng placeholder
                onLoaded?.Invoke(placeholder);
                yield break;
            }

            var texture = DownloadHandlerTexture.GetContent(req);
            if (texture == null)
            {
                onLoaded?.Invoke(placeholder);
                yield break;
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));

            _cache[url] = sprite;
            onLoaded?.Invoke(sprite);
        }

        /// <summary>
        /// Tạo một Sprite placeholder hình tròn mềm mại bằng code.
        /// </summary>
        private static Sprite CreateCircularPlaceholder(int size, Color color)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float radius = size * 0.48f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= radius)
                    {
                        // Viền nhẹ
                        float borderDist = radius - dist;
                        float alpha = Mathf.Clamp01(borderDist * 2f);
                        pixels[y * size + x] = new Color(color.r, color.g, color.b, color.a * alpha);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// Xóa bộ nhớ cache ảnh khi cần giải phóng RAM.
        /// </summary>
        public static void ClearCache()
        {
            _cache.Clear();
        }
    }
}
