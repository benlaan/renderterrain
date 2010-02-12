using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Laan.DLOD.Levelled
{
    public struct VertexMultiTextured
    {
        public VertexMultiTextured(System.Drawing.Point p, Terrain terrain)
        {
            Position = new Vector3(p.X, p.Y, terrain.HeightAt(p));
            int textureWidth = terrain.Height / 32;
            TextureCoordinate = new Vector4(((float)p.X) / textureWidth, ((float)p.Y) / textureWidth, 0, 0);
            Normal = new Vector3(0, 0, 1);
            TexWeights = new Vector4();
            SetTexture(terrain.UnscaledHeightAt(p));
        }

        private float GetBlendDistribution(float height, int start, int end)
        {
            return height >= start && height <= end ? (end - height) / (float)(end - start) : 0;
        }

        private void SetTexture(float height)
        {
            TexWeights.X = height <= 90 ? 1 : 0;
            TexWeights.Y = GetBlendDistribution(height, 90, 130);
            TexWeights.Z = GetBlendDistribution(height, 110, 175);
            TexWeights.W = GetBlendDistribution(height, 165, 200);

            float total = TexWeights.X + TexWeights.Y + TexWeights.Z + TexWeights.W;

            TexWeights.X /= total;
            TexWeights.Y /= total;
            TexWeights.Z /= total;
            TexWeights.W /= total;
        }

        public Vector3 Position;
        public Vector3 Normal;
        public Vector4 TextureCoordinate;
        public Vector4 TexWeights;

        public static int SizeInBytes = (3 + 3 + 4 + 4) * sizeof(float);
        public static VertexElement[] VertexElements = new VertexElement[]
        {
	         new VertexElement( 0, 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0 ),
	         new VertexElement( 0, sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 0 ),
	         new VertexElement( 0, sizeof(float) * 6, VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0 ),
	         new VertexElement( 0, sizeof(float) * 10, VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 1 ),
	    };
    }
}
