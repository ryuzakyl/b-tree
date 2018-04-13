using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BTreeDataStructure
{
    /// <summary>
    /// Convierte a long un array de bytes.
    /// </summary>
    public static class Converter
    {

        internal static byte FromBool(bool value)
        {
            return (byte)(value ? 1 : 0);
        }
        
        internal static byte[] FromInt(int k)
        {
            return BitConverter.GetBytes(k);
        }
        internal static byte[] FromLong(long k)
        {
            return BitConverter.GetBytes(k);
        }
        internal static long ToLong(byte[] byteLong, long index)
        {
            if (Overflow(index))
                throw new IndexOutOfRangeException();
            return BitConverter.ToInt64(byteLong, (int)index);
        }
        internal static bool ToBool(byte byteBool)
        { 
            return (byteBool== 1) ? true : false;
        }
        internal static int ToInt(byte[] byteInt, long index)
        {
            if (Overflow(index))
                throw new IndexOutOfRangeException();
            return BitConverter.ToInt32(byteInt, (int)index);
        }

        internal static bool Overflow(long k)
        {
            return k > int.MaxValue;
        }
    }
}
