using UnityEngine;
using System;

namespace ConFormSim.ObjectProperties
{
    public static class ObjectIDColorEncoding
    {
        public static Color32 IDToColor(int id)
        {
            byte[] intBytes = BitConverter.GetBytes(id);
            return new Color32(intBytes[0], intBytes[1], intBytes[2], intBytes[3]);
        }

        public static int ColorToID(Color32 color)
        {
            return BitConverter.ToInt32(color.ToByteArray(), 0);
        }

        public static byte[] ToByteArray(this Color32 color) 
        {
            return new[] {color.r, color.g, color.b, color.a};
        }
    }
}