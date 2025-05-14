using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Unity.HLODSystem.Utils
{
    public static class TextureExtensions
    {
        public static WorkingTexture ToWorkingTexture(this Texture2D texture, Allocator allocator)
        {
            var wt = new WorkingTexture(allocator, texture);

            return wt;
        }
    }

    public class WorkingTexture : IDisposable
    {
        private NativeArray<int> m_detector = new NativeArray<int>(1, Allocator.Persistent);

        private WorkingTextureBuffer m_buffer;

        public string Name
        {
            set { m_buffer.Name = value; }
            get { return m_buffer.Name; }
        }

        public TextureFormat Format => m_buffer.Format;
        public int Width => m_buffer.Widht;

        public int Height => m_buffer.Height;

        public bool Linear
        {
            set => m_buffer.Linear = value;
            get => m_buffer.Linear;
        }

        public TextureWrapMode WrapMode
        {
            set => m_buffer.WrapMode = value;
            get => m_buffer.WrapMode;
        }

        private WorkingTexture()
        {
        }

        public WorkingTexture(Allocator allocator, TextureFormat format, int width, int height, bool linear)
        {
            m_buffer = WorkingTextureBufferManager.Instance.Create(allocator, format, width, height, linear);
        }

        public WorkingTexture(Allocator allocator, Texture2D source)
        {
            m_buffer = WorkingTextureBufferManager.Instance.Get(allocator, source);
        }

        public void Dispose()
        {
            m_buffer.Release();
            m_buffer = null;

            m_detector.Dispose();
        }

        public WorkingTexture Clone()
        {
            WorkingTexture nwt = new WorkingTexture();
            nwt.m_buffer = m_buffer;
            nwt.m_buffer.AddRef();

            return nwt;
        }

        public Texture2D ToTexture()
        {
            return m_buffer.ToTexture();
        }

        public Guid GetGUID()
        {
            return m_buffer.GetGUID();
        }

        public void SetPixel(int x, int y, Color color)
        {
            MakeWriteable();

            m_buffer.SetPixel(x, y, color);
        }


        public Color GetPixel(int x, int y)
        {
            return m_buffer.GetPixel(x, y);
        }

        public Color GetPixel(float u, float v)
        {
            float x = u * (Width - 1);
            float y = v * (Height - 1);

            int x1 = Mathf.FloorToInt(x);
            int x2 = Mathf.CeilToInt(x);

            int y1 = Mathf.FloorToInt(y);
            int y2 = Mathf.CeilToInt(y);

            float xWeight = x - x1;
            float yWeight = y - y1;

            Color c1 = Color.Lerp(GetPixel(x1, y1), GetPixel(x2, y1), xWeight);
            Color c2 = Color.Lerp(GetPixel(x1, y2), GetPixel(x2, y2), xWeight);

            return Color.Lerp(c1, c2, yWeight);
        }

        public void Blit(WorkingTexture source, int x, int y)
        {
            MakeWriteable();

            m_buffer.Blit(source.m_buffer, x, y);
        }


        public WorkingTexture Resize(Allocator allocator, int newWidth, int newHeight)
        {
            WorkingTexture wt = new WorkingTexture(allocator, m_buffer.Format, newWidth, newHeight, m_buffer.Linear);

            float xWeight = (float)(m_buffer.Widht - 1) / (float)(newWidth - 1);
            float yWeight = (float)(m_buffer.Height - 1) / (float)(newHeight - 1);

            for (int y = 0; y < newHeight; ++y)
            {
                for (int x = 0; x < newWidth; ++x)
                {
                    float xpos = x * xWeight;
                    float ypos = y * yWeight;

                    float u = xpos / Width;
                    float v = ypos / Height;

                    wt.SetPixel(x, y, GetPixel(u, v));
                }
            }

            return wt;
        }


        private void MakeWriteable()
        {
            if (m_buffer.GetRefCount() > 1)
            {
                WorkingTextureBuffer newBuffer = WorkingTextureBufferManager.Instance.Clone(m_buffer);
                m_buffer.Release();
                m_buffer = newBuffer;
            }
        }
    }

    public class WorkingTextureBufferManager
    {
        private static WorkingTextureBufferManager s_instance;

        public static WorkingTextureBufferManager Instance
        {
            get
            {
                if (s_instance == null)
                    s_instance = new WorkingTextureBufferManager();

                return s_instance;
            }
        }


        private Dictionary<Texture2D, WorkingTextureBuffer> m_cache = new Dictionary<Texture2D, WorkingTextureBuffer>();

        public WorkingTextureBuffer Get(Allocator allocator, Texture2D texture)
        {
            WorkingTextureBuffer buffer = null;
            if (m_cache.ContainsKey(texture) == true)
            {
                buffer = m_cache[texture];
            }
            else
            {
                buffer = new WorkingTextureBuffer(allocator, texture);
                m_cache.Add(texture, buffer);
            }

            buffer.AddRef();
            return buffer;
        }

        public WorkingTextureBuffer Create(Allocator allocator, TextureFormat format, int width, int height,
            bool linear)
        {
            WorkingTextureBuffer buffer = new WorkingTextureBuffer(allocator, format, width, height, linear);
            buffer.AddRef();
            return buffer;
        }

        public WorkingTextureBuffer Clone(WorkingTextureBuffer buffer)
        {
            WorkingTextureBuffer nb = buffer.Clone();
            nb.AddRef();
            return nb;
        }

        public void Destroy(WorkingTextureBuffer buffer)
        {
            if (buffer.HasSource())
            {
                m_cache.Remove(buffer.GetSource());
            }
        }
    }

    public class WorkingTextureBuffer : IDisposable
    {
        private Allocator m_allocator;
        private TextureFormat m_format;
        private int m_width;
        private int m_height;
        private bool m_linear;

        private NativeArray<Color> m_pixels;

        private int m_refCount;
        private Texture2D m_source;

        private Guid m_guid;

        private TextureWrapMode m_wrapMode = TextureWrapMode.Repeat;

        public string Name { set; get; }

        public TextureFormat Format => m_format;
        public int Widht => m_width;
        public int Height => m_height;

        public bool Linear
        {
            set => m_linear = value;
            get => m_linear;
        }

        public TextureWrapMode WrapMode
        {
            get => m_wrapMode;
            set => m_wrapMode = value;
        }


        public WorkingTextureBuffer(Allocator allocator, TextureFormat format, int width, int height, bool linear)
        {
            m_allocator = allocator;
            m_format = format;
            m_width = width;
            m_height = height;
            m_linear = linear;
            m_pixels = new NativeArray<Color>(width * height, allocator);
            m_refCount = 0;
            m_source = null;
            m_guid = Guid.NewGuid();
        }

        public WorkingTextureBuffer(Allocator allocator, Texture2D source)
            : this(allocator, source.format, source.width, source.height,
                !GraphicsFormatUtility.IsSRGBFormat(source.graphicsFormat))
        {
            Name = source.name;
            m_source = source;
            CopyFrom(source);
            m_guid = GUIDUtils.ObjectToGUID(source);
        }

        public WorkingTextureBuffer Clone()
        {
            WorkingTextureBuffer buffer = new WorkingTextureBuffer(m_allocator, m_format, m_width, m_height, m_linear);
            buffer.Blit(this, 0, 0);
            return buffer;
        }

        public Texture2D ToTexture()
        {
            Texture2D texture = new Texture2D(m_width, m_height, m_format, false, m_linear);
            texture.name = Name;
            texture.SetPixels(m_pixels.ToArray());
            texture.wrapMode = m_wrapMode;
            texture.Apply();
            return texture;
        }

        public Guid GetGUID()
        {
            return m_guid;
        }

        public bool HasSource()
        {
            return m_source != null;
        }

        public Texture2D GetSource()
        {
            return m_source;
        }

        public void AddRef()
        {
            m_refCount += 1;
        }

        public void Release()
        {
            m_refCount -= 1;

            if (m_refCount == 0)
            {
                WorkingTextureBufferManager.Instance.Destroy(this);
                Dispose();
            }
        }

        public int GetRefCount()
        {
            return m_refCount;
        }

        public void Dispose()
        {
            if (m_pixels.IsCreated)
                m_pixels.Dispose();
        }

        public void SetPixel(int x, int y, Color color)
        {
            m_pixels[y * m_width + x] = color;
        }

        public Color GetPixel(int x, int y)
        {
            return m_pixels[y * m_width + x];
        }

        public void Blit(WorkingTextureBuffer source, int x, int y)
        {
            int width = source.m_width;
            int height = source.m_height;

            for (int sy = 0; sy < height; ++sy)
            {
                int ty = y + sy;
                if (ty < 0 || ty >= m_height)
                    continue;

                for (int sx = 0; sx < width; ++sx)
                {
                    int tx = x + sx;
                    if (tx < 0 || tx >= m_width)
                        continue;

                    SetPixel(tx, ty, source.GetPixel(sx, sy));
                }
            }
        }

        private void CopyFrom(Texture2D texture)
        {
            if (texture == null)
            {
                Debug.LogWarning("텍스처가 null입니다");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(texture);

            // 텍스처가 읽을 수 없는 상태인 경우
            if (!texture.isReadable)
            {
                // 에셋 경로가 유효하지 않은 경우 (내장 텍스처 등) 처리
                if (string.IsNullOrEmpty(assetPath))
                {
                    Debug.LogError($"텍스처 '{texture.name}'는 유효한 에셋 경로가 없어 읽기 권한을 설정할 수 없습니다.");
                    CreateDefaultTexture(); // 기본 텍스처 생성하는 대체 방법
                    return;
                }

                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    Debug.Log($"텍스처 '{texture.name}'에 읽기 권한 설정 중...");

                    // 원래 설정 저장
                    bool originalReadable = importer.isReadable;

                    try
                    {
                        // 읽기 가능하도록 설정
                        importer.isReadable = true;
                        importer.SaveAndReimport();

                        // AssetDatabase 작업이 완료되도록 잠시 기다림
                        AssetDatabase.Refresh();

                        // 텍스처를 다시 로드
                        texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

                        if (texture == null || !texture.isReadable)
                        {
                            Debug.LogError($"텍스처 '{assetPath}'를 읽기 가능한 상태로 다시 로드하는데 실패했습니다.");
                            CreateDefaultTexture();
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"텍스처 '{texture.name}' 설정 변경 중 오류 발생: {ex.Message}");
                        CreateDefaultTexture();
                        return;
                    }
                }
                else
                {
                    Debug.LogError($"텍스처 '{texture.name}'의 TextureImporter를 찾을 수 없습니다.");
                    CreateDefaultTexture();
                    return;
                }
            }

            // 텍스처 데이터 복사
            try
            {
                m_linear = !GraphicsFormatUtility.IsSRGBFormat(texture.graphicsFormat);
                m_wrapMode = texture.wrapMode;

                int count = m_width * m_height;
                Color[] texturePixels = texture.GetPixels();
                if (texturePixels.Length != count)
                {
                    Debug.LogError($"텍스처 픽셀 수 불일치: 예상 {count}, 실제 {texturePixels.Length}");
                    CreateDefaultTexture();
                    return;
                }

                m_pixels.Slice(0, count).CopyFrom(texturePixels);
            }
            catch (Exception ex)
            {
                Debug.LogError($"텍스처 '{texture.name}' 데이터 복사 중 오류 발생: {ex.Message}");
                CreateDefaultTexture();
            }
        }

// 텍스처 로드 실패 시 기본 텍스처 생성
        private void CreateDefaultTexture()
        {
            // 분홍색 체커보드 패턴으로 기본 텍스처 생성
            Color pink = new Color(1f, 0f, 1f, 1f);
            Color black = new Color(0f, 0f, 0f, 1f);

            for (int y = 0; y < m_height; y++)
            {
                for (int x = 0; x < m_width; x++)
                {
                    Color color = ((x / 8) % 2 == (y / 8) % 2) ? pink : black;
                    m_pixels[y * m_width + x] = color;
                }
            }

            Debug.Log("임시 체커보드 텍스처가 생성되었습니다.");
        }
    }
}