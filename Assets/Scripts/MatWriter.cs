using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public static class MatWriter
    {
        public static void Add(this Material mat, string name, float i)
        {
            mat.SetFloat(name, i);
        }

        public static void Add(this Material mat, string name, int i)
        {
            mat.SetInt(name, i);
        }

        public static void Add(this Material mat, string name, Color i)
        {
            mat.SetColor(name, i);
        }

        public static void Add(this Material mat, string name, Color[] i)
        {
            mat.SetColorArray(name, i);
        }

        public static void Add(this Material mat, string name, Vector2 i)
        {
            mat.SetVector(name, i);
        }

        public static void Add(this Material mat, string name, Vector3 i)
        {
            mat.SetVector(name, i);
        }

        public static void Add(this Material mat, string name, Vector4 i)
        {
            mat.SetVector(name, i);
        }

        public static void Add(this Material mat, string name, Matrix4x4 i)
        {
            mat.SetMatrix(name, i);
        }

        public static void Add(this Material mat, string name, Texture i)
        {
            mat.SetTexture(name, i);
        }
    }
}
