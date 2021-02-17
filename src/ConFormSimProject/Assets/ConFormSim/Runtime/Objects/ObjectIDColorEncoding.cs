using UnityEngine;
using System;

namespace ConFormSim.ObjectProperties
{
    public static class ObjectIDColorEncoding
    {
        public static Color IDToColor(int id)
        {
            byte[] intBytes = BitConverter.GetBytes(id);
            return new Color(BitConverter.ToSingle(intBytes, 0), 0,0,1);
        }

        public static int ColorToID(Color color)
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(color.r),0);
        }

        public static byte[] ToByteArray(this Color32 color) 
        {
            return new[] {color.r, color.g, color.b, color.a};
        }
    }
}