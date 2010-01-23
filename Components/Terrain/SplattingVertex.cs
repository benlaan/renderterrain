using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Laan.DLOD
{
    public struct SplattingVertex
    {
        public Vector3 Position;
        public Vector2 Tex0;
        public Vector2 Tex1;
        public Vector3 Normal;

        public static readonly VertexElement[] VertexElements =
            new VertexElement[] { 
                // Position float3
                new VertexElement(0, 0, VertexElementFormat.Vector3,
                                  VertexElementMethod.Default,VertexElementUsage.Position, 0),

                // TexCoord0 float2
                new VertexElement(0, sizeof(float) * 3, VertexElementFormat.Vector2,
                                  VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0),

                // TexCoord1 float2
                new VertexElement(0, sizeof(float) * 5, VertexElementFormat.Vector2,
                                  VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 1),

                // Normal float3
                new VertexElement(0, sizeof(float) * 7, VertexElementFormat.Vector3,
                                  VertexElementMethod.Default, VertexElementUsage.Normal, 0)
            };

        public SplattingVertex(Vector3 position, Vector2 uv0, Vector2 uv1, Vector3 normal)
        {
            Position = position;
            Tex0 = uv0;
            Tex1 = uv1;
            Normal = normal;
        }

        public static bool operator != (SplattingVertex left, SplattingVertex right)
        {
            return left.GetHashCode() != right.GetHashCode();
        }

        public static bool operator == (SplattingVertex left, SplattingVertex right)
        {
            return left.GetHashCode() == right.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this == (SplattingVertex)obj;
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode() | Tex0.GetHashCode() | Tex1.GetHashCode() | Normal.GetHashCode();
        }

        public static int SizeInBytes
        {
            get
            {
                return sizeof(float) * 10;
            }
        }
    }
}
