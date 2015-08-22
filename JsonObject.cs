using System;
using System.Linq;
using System.Collections.Generic;

namespace IConvertJson
{
	public class JsonObject : Dictionary<string,IConvertible>,IConvertible
	{
		public TypeCode GetTypeCode() { return TypeCode.Object; }
		public override string ToString()
		{
			return this.ToString(null);
		}

		public string ToString(IFormatProvider provider)
		{
			return "{" + string.Join(",",this.Select((pair) => string.Format("{0}:{1}",pair.Key.ToJson(),pair.Value.ToJson())).ToArray()) + "}";
		}

		#region NotImplemented

		public object ToType(Type conversionType,IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public bool ToBoolean(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public byte ToByte(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public char ToChar(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public DateTime ToDateTime(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public decimal ToDecimal(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public double ToDouble(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public short ToInt16(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public int ToInt32(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public long ToInt64(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public sbyte ToSByte(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public float ToSingle(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public ushort ToUInt16(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public uint ToUInt32(IFormatProvider provider)
		{
			throw new NotImplementedException();
		}

		public ulong ToUInt64(IFormatProvider provider)
		{
			throw new NotImplementedException();
		} 
		#endregion
	}
}