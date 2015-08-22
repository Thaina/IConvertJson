//
// JavaScriptReader.cs
//
// Authors:
//	Marek Safar (marek.safar@gmail.com)
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace IConvertJson
{
	struct JavaScriptReader
	{
		int? peek;
		int line,column;
		readonly TextReader reader;
		JavaScriptReader(TextReader textReader)
		{
			reader	= textReader;
			peek	= null;

			line	= 0;
			column	= 0;
		}

		public static IConvertible Read(TextReader reader)
		{
			var state	= new JavaScriptReader(reader);
			var v	= state.ReadCore();
			state.SkipSpaces();
			if(reader.Read() >= 0)
				throw state.JsonError(String.Format("extra characters in JSON input"));
			return v;
		}

		public Exception JsonError(string msg)
		{
			return new ArgumentException(String.Format("{0}. At line {1}, column {2}",msg,line,column));
		}

		int PeekChar()
		{
			if(!peek.HasValue)
				peek = reader.Read();

			return peek.Value;
		}

		int ReadChar()
		{
			column++;
			if(peek.HasValue && peek.Value == '\n')
			{
				line++;
				column	= 0;
			}

			int v	= PeekChar();
			peek	= null;
			return v;
		}

		void SkipSpaces()
		{
			while(true)
			{
				switch(PeekChar())
				{
					case ' ':
					case '\t':
					case '\r':
					case '\n':
						ReadChar();
						continue;
					default:
						return;
				}
			}
		}

		IConvertible ReadCore()
		{
			SkipSpaces();
			int c = PeekChar();
			if(c < 0)
				throw JsonError("Incomplete JSON input");

			switch(c)
			{
				case '[':
					var list = new JsonArray();

					ReadChar();
					SkipSpaces();
					if(PeekChar() == ']')
					{
						ReadChar();
						return list;
					}
					while(true)
					{
						list.Add(ReadCore());
						SkipSpaces();
						c	= PeekChar();
						if(c != ',')
							break;
						ReadChar();
						continue;
					}
					if(ReadChar() != ']')
						throw JsonError("JSON array must end with ']'");
					return list;

				case '{':
					var obj	= new JsonObject();

					ReadChar();
					SkipSpaces();
					if(PeekChar() == '}')
					{
						ReadChar();
						return obj;
					}
					while(true)
					{
						SkipSpaces();
						if(PeekChar() == '}')
							break;
						string name	= new string(ReadStringLiteral().ToArray());
						SkipSpaces();
						Expect(':');
						SkipSpaces();
						obj[name]	= ReadCore(); // it does not reject duplicate names.
						SkipSpaces();
						c = ReadChar();
						if(c == ',')
							continue;
						if(c == '}')
							break;
					}
					return obj;

				case 't':
					Expect("true");
					return true;
				case 'f':
					Expect("false");
					return false;
				case 'n':
					Expect("null");
					return null;
				case '"':
					return new string(ReadStringLiteral().ToArray());

				case '-':
				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
				case 'I':
				case 'N':
					return ReadNumericLiteral();
				default:
					throw JsonError(String.Format("Unexpected character '{0}' ({1})",(char)c,(int)c));
			}
		}

		// It could return either int, long or decimal, depending on the parsed value.
		IConvertible ReadNumericLiteral()
		{
			bool negative	= PeekChar() == '-';
			if(negative)
			{
				ReadChar();
				if(PeekChar() < 0)
					throw JsonError("Invalid JSON numeric literal; extra negation");
			}

			if(PeekChar() == 'I')
			{
				Expect("Infinity");
				return negative ? float.NegativeInfinity : float.PositiveInfinity;
			}

			if(PeekChar() == 'N')
			{
				Expect("NaN");
				return float.NaN;
			}

			decimal val	= 0;
			while(true)
			{
				int c	= PeekChar();
				if(c < '0' || '9' < c)
					break;
				val	= (val * 10) + (c - '0');
				ReadChar();
			}

			// fraction

			bool hasFrag	= PeekChar() == '.';
			if(hasFrag)
			{
				ReadChar();
				if(PeekChar() < 0)
					throw JsonError("Invalid JSON numeric literal; extra dot");

				decimal frac	= 0;
				decimal d	= 10;
				int fdigits	= 0;
				while(true)
				{
					int c	= PeekChar();
					if(c < '0' || '9' < c)
						break;
					frac += (c - '0') / d;
					d *= 10;
					fdigits++;

					ReadChar();
				}

				if(fdigits == 0)
					throw JsonError("Invalid JSON numeric literal; extra dot");

				val	+= Decimal.Round(frac,fdigits);
			}

			if(negative)
				val	= -val;

			int e	= PeekChar();
			if(e != 'e' && e != 'E')
			{
				if(!hasFrag)
				{
					if(int.MinValue <= val && val <= int.MaxValue)
						return (int)val;

					if(long.MinValue <= val && val <= long.MaxValue)
						return (long)val;
				}

				return val;
			}

			// exponent

			ReadChar();
			if(PeekChar() < 0)
				throw new ArgumentException("Invalid JSON numeric literal; incomplete exponent");

			int sign = PeekChar();
			bool negexp	= sign == '-';
			if(negexp || sign == '+')
				ReadChar();

			if(PeekChar() < 0)
				throw JsonError("Invalid JSON numeric literal; incomplete exponent");

			int exp	= 0;
			while(true)
			{
				int c = PeekChar();
				if(c < '0' || '9' < c)
					break;
				exp	= (exp * 10) + (c - '0');
				ReadChar();
			}

			// it is messy to handle exponent, so I just use Decimal.Parse() with assured JSON format.
			if(negexp)
				return (double)val / Math.Pow(10,exp);

			return (double)val * Math.Pow(10,exp);
		}

		IEnumerable<char> ReadStringLiteral()
		{
			if(PeekChar() != '"')
				throw JsonError("Invalid JSON string literal format");

			ReadChar();
			while(true)
			{
				int c = ReadChar();
				if(c < 0)
					throw JsonError("JSON string is not closed");
				if(c == '"')
					yield break;
				else if(c != '\\')
				{
					yield return (char)c;
					continue;
				}

				// escaped expression
				c = ReadChar();
				if(c < 0)
					throw JsonError("Invalid JSON string literal; incomplete escape sequence");
				switch(c)
				{
					case '"':
					case '\\':
					case '/':
						yield return (char)c;
						break;
					case 'b':
						yield return '\x8';
						break;
					case 'f':
						yield return '\f';
						break;
					case 'r':
						yield return '\r';
						break;
					case 'n':
						yield return '\n';
						break;
					case 't':
						yield return '\t';
						break;
					case 'u':
						ushort cp = 0;
						for(int i = 0;i < 4;i++)
						{
							cp <<= 4;
							if((c = ReadChar()) < 0)
								throw JsonError("Incomplete unicode character escape literal");
							if('0' <= c && c <= '9')
								cp += (ushort)(c - '0');
							if('A' <= c && c <= 'F')
								cp += (ushort)(c - 'A' + 10);
							if('a' <= c && c <= 'f')
								cp += (ushort)(c - 'a' + 10);
						}
						yield return (char)cp;
						break;
					default:
						throw JsonError("Invalid JSON string literal; unexpected escape character");
				}
			}
		}

		void Expect(char expected)
		{
			int c;
			if((c = ReadChar()) != expected)
				throw JsonError(String.Format("Expected '{0}', got '{1}'",expected,(char)c));
		}

		void Expect(string expected)
		{
			for(int i = 0;i < expected.Length;i++)
				if(ReadChar() != expected[i])
					throw JsonError(String.Format("Expected '{0}', differed at {1}",expected,i));
		}
	}
}