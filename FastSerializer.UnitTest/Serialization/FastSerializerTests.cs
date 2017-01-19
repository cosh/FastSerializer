#if DEBUG
using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Reflection;

using NUnit.Framework;
using CategoryAttribute = NUnit.Framework.CategoryAttribute;

using System.Collections.Generic;

// ReSharper disable ConvertToConstant.Local

namespace Framework.Serialization
{
	
	[TestFixture]
	public class FastSerializerTests
	{
		const int FullHeaderSize = 12;
		const int MinHeaderSize = 4;
		static readonly TimeSpan NotOptimizableTimeSpan = TimeSpan.FromTicks(1);
		static readonly DateTime OptimizableDateTime = new DateTime(2006, 11, 8);
		static readonly DateTime NotOptimizableDateTime = OptimizableDateTime.AddTicks(1);

		SerializationReader reader;
		SerializationWriter writer;
		int writerPosition;
		long writerLength;
		long objectWriterLength;

		[SetUp]
		public void CreateWriter()
		{
			writer = new SerializationWriter();
			reader = null;
			writerPosition = 0;
			writerLength = -1;
			objectWriterLength = -1;
		}

		SerializationWriter Writer
		{
			get { return writer; }
		}

		[Browsable(false)]
		SerializationReader Reader
		{
			get
			{
				if (reader == null)
				{
					writerPosition = (int) Writer.BaseStream.Position;
					reader = GetReaderFromWriter(Writer);
				}
				return reader;
			}
		}

		void CheckValueAsObject(int expectedSizeAsObject, object value, IComparer comparer, bool preserveDecimalScale)
		{
			var objectWriter = new SerializationWriter
			                   	{
			                   		PreserveDecimalScale = preserveDecimalScale
			                   	};

			objectWriter.WriteObject(value);

			var writerActualSize = objectWriter.BaseStream.Position - FullHeaderSize - objectWriter.TableBytes;
			Assert.AreEqual(expectedSizeAsObject, writerActualSize, "Incorrect length on Write As Object");

			var objectReader = GetReaderFromWriter(objectWriter);

			var newValue = objectReader.ReadObject();
			var readerActualSize = objectReader.BaseStream.Position - FullHeaderSize - objectWriter.TableBytes;

			Assert.AreEqual(expectedSizeAsObject, readerActualSize, "Incorrect length on Read As Object");
			if (comparer == null)
			{
				Assert.AreEqual(value, newValue, "Object Value Read does not match Object Value Write");
			}
			else
			{
				Assert.AreEqual(0, comparer.Compare(value, newValue), "Object Value Read does not match Object Value Write");
			}

			Assert.AreEqual(0, objectReader.BytesRemaining);
		}

		void CheckValue(int expectedSize, int expectedSizeAsObject, object value, object newValue)
		{
			CheckValue(expectedSize, expectedSizeAsObject, value, newValue, value is IList ? new ListComparer() : null);
		}

		void CheckValue(int expectedSize, int expectedSizeAsObject, object value, object newValue, IComparer comparer)
		{
			if (expectedSize != -1)
			{
				var writerActualSize = writerPosition - FullHeaderSize - Writer.TableBytes;
				var readerActualSize = Reader.BaseStream.Position - FullHeaderSize - Writer.TableBytes;
				Assert.AreEqual(expectedSize, writerActualSize, "Incorrect length on Write Direct");
				Assert.AreEqual(expectedSize, readerActualSize, "Incorrect length on Read Direct");
			}

			if (value != null)
			{
				Assert.AreSame(value.GetType(), newValue.GetType());
			}

			if (comparer == null)
			{
				Assert.AreEqual(value, newValue, "Direct Value Read does not match Direct Value Write");
			}
			else
			{
				Assert.AreEqual(0, comparer.Compare(value, newValue), "Direct Value Read does not match Direct Value Write");
			}

			if (expectedSizeAsObject != -1)
			{
				CheckValueAsObject(expectedSizeAsObject, value, comparer, Writer.PreserveDecimalScale);
			}

			Assert.AreEqual(0, Reader.BytesRemaining);
		}

		SerializationReader GetReaderFromWriter(SerializationWriter serializationWriter)
		{
			var data = serializationWriter.ToArray();

			if (writerLength == -1)
			{
				writerLength = data.Length;
			}
			else
			{
				objectWriterLength = data.Length;
			}

			return new SerializationReader(data);
		}

		[Test]
		public void CheckTrueBoolean()
		{
			var value = true;
// ReSharper disable ConditionIsAlwaysTrueOrFalse
			Writer.Write(value);
// ReSharper restore ConditionIsAlwaysTrueOrFalse
			CheckValue(1, 1, value, Reader.ReadBoolean());
		}

		[Test]
		public void CheckFalseBoolean()
		{
			var value = false;
// ReSharper disable ConditionIsAlwaysTrueOrFalse
			Writer.Write(value);
// ReSharper restore ConditionIsAlwaysTrueOrFalse
			CheckValue(1, 1, value, Reader.ReadBoolean());
		}

		[Test]
		public void CheckByte()
		{
			Byte value = 33;
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadByte());
		}

		[Test]
		public void CheckByteAsZero()
		{
			Byte value = 0;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadByte());
		}

		[Test]
		public void CheckByteAsOne()
		{
			Byte value = 1;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadByte());
		}

		[Test]
		public void CheckByteAsMaxValue()
		{
			var value = Byte.MaxValue;
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadByte());
		}

		[Test]
		public void CheckSByte()
		{
			SByte value = 33;
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadSByte());
		}

		[Test]
		public void CheckSByteNegative()
		{
			SByte value = -33;
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadSByte());
		}

		[Test]
		public void CheckSByteAsZero()
		{
			SByte value = 0;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadSByte());
		}

		[Test]
		public void CheckSByteAsMinValue()
		{
			var value = SByte.MinValue;
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadSByte());
		}

		[Test]
		public void CheckSByteAsMaxValue()
		{
			var value = SByte.MaxValue;
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadSByte());
		}

		[Test]
		public void CheckSByteAsOne()
		{
			SByte value = 1;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadSByte());
		}

		[Test]
		public void CheckChar()
		{
			var value = (Char) 33;
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadChar());
		}

		[Test]
		public void CheckCharAsZero()
		{
			var value = (char) 0;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadChar());
		}

		[Test]
		public void CheckCharAsOne()
		{
			var value = (Char) 1;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadChar());
		}

		[Test]
		public void CheckDecimal()
		{
			Decimal value = 33;
			Writer.Write(value);
			CheckValue(16, 3, value, Reader.ReadDecimal());
		}

		[Test]
		public void CheckDecimalLarge()
		{
			var value = Decimal.MaxValue - 1;
			Writer.Write(value);
			CheckValue(16, 14, value, Reader.ReadDecimal());
		}

		[Test]
		public void CheckDecimalNegative()
		{
			Decimal value = 33;
			Writer.Write(value);
			CheckValue(16, 3, value, Reader.ReadDecimal());
		}

		[Test]
		public void CheckDecimalLargeNegative()
		{
			var value = Decimal.MinValue + 1;
			Writer.Write(value);
			CheckValue(16, 14, value, Reader.ReadDecimal());
		}

		[Test]
		public void CheckDecimalAsZero()
		{
			Decimal value = 0;
			Writer.Write(value);
			CheckValue(16, 1, value, Reader.ReadDecimal());
		}

		[Test]
		public void CheckDecimalAsOne()
		{
			Decimal value = 1;
			Writer.Write(value);
			CheckValue(16, 1, value, Reader.ReadDecimal());
		}

		decimal GetDecimalAsObject(decimal value, bool preserveDecimalScale)
		{
			writer = new SerializationWriter();
			Writer.PreserveDecimalScale = preserveDecimalScale;
			Writer.WriteObject(value);
			reader = null;
			return (decimal) Reader.ReadObject();
		}

		[Test]
		public void CheckDecimalAsTwo()
		{
			var value = 2m;
			Assert.AreEqual("2", value.ToString());
			Writer.Write(value);
			CheckValue(16, 3, value, Reader.ReadDecimal());

			var result = GetDecimalAsObject(value, false);
			Assert.AreEqual(value, result);
			Assert.AreEqual("2", result.ToString());
		}

		[Test]
		public void CheckDecimalAsTwoWithTrailingZeroes()
		{
			var value = 2.00m;
			Assert.AreEqual("2.00", value.ToString());
			Writer.Write(value);
			CheckValue(16, 3, value, Reader.ReadDecimal());

			var result = GetDecimalAsObject(value, false);
			Assert.AreEqual(value, result);
			Assert.AreEqual("2", result.ToString());
		}

		[Test]
		public void CheckDecimalAsTwoWithPreservedTrailingZeroes()
		{
			var value = 2.00m;
			Assert.AreEqual("2.00", value.ToString());
			Writer.PreserveDecimalScale = true;
			Writer.Write(value);
			CheckValue(16, 5, value, Reader.ReadDecimal());

			var result = GetDecimalAsObject(value, true);
			Assert.AreEqual(value, result);
			Assert.AreEqual("2.00", result.ToString());
		}

		[Test]
		public void CheckDecimalAsMillion()
		{
			var value = 1000000m;
			Writer.Write(value);
			CheckValue(16, 5, value, Reader.ReadDecimal());

			var result = GetDecimalAsObject(value, false);
			Assert.AreEqual(value, result);
			Assert.AreEqual("1000000", result.ToString());
			
		}

		[Test]
		public void CheckDecimalAsMillionWithTrailingZeroes()
		{
			var value = 1000000.0000m;
			Writer.Write(value);
			CheckValue(16, 5, value, Reader.ReadDecimal());

			var result = GetDecimalAsObject(value, false);
			Assert.AreEqual(value, result);
			Assert.AreEqual("1000000", result.ToString());
		}

		[Test]
		public void CheckDecimalAsMillionWithPreservedTrailingZeroes()
		{
			var value = 1000000.0000m;
			Writer.PreserveDecimalScale = true;
			Writer.Write(value);
			CheckValue(16, 8, value, Reader.ReadDecimal());

			var result = GetDecimalAsObject(value, true);
			Assert.AreEqual(value, result);
			Assert.AreEqual("1000000.0000", result.ToString());
		}

		[Test]
		public void CheckDecimalAsMinValue()
		{
			var value = Decimal.MinValue;
			Writer.Write(value);
			CheckValue(16, 14, value, Reader.ReadDecimal());
		}

		[Test]
		public void CheckDecimalAsMaxValue()
		{
			var value = Decimal.MaxValue;
			Writer.Write(value);
			CheckValue(16, 14, value, Reader.ReadDecimal());
		}

		[Test]
		public void CheckDecimalOptimized()
		{
			Decimal value = 33;
			Writer.WriteOptimized(value);
			CheckValue(2, 3, value, Reader.ReadOptimizedDecimal());
		}

		[Test]
		public void CheckDecimalOptimizedWithTrailingZeroes()
		{
			var value = 33.00m;
			Writer.WriteOptimized(value);
			CheckValue(2, 3, value, Reader.ReadOptimizedDecimal());
		}

		[Test]
		public void CheckDecimalOptimizedWithPreservedTrailingZeroes()
		{
			var value = 33.00m;
			Writer.PreserveDecimalScale = true;
			Writer.WriteOptimized(value);
			CheckValue(4, 5, value, Reader.ReadOptimizedDecimal());
		}

		[Test]
		public void CheckDecimalOptimizedLarge()
		{
			var value = Decimal.MaxValue - 1;
			Writer.WriteOptimized(value);
			CheckValue(13, 14, value, Reader.ReadOptimizedDecimal());
		}

		[Test]
		public void CheckDecimalOptimizedNegative()
		{
			Decimal value = -33;
			Writer.WriteOptimized(value);
			CheckValue(2, 3, value, Reader.ReadOptimizedDecimal());
		}

		[Test]
		public void CheckDecimalOptimizedLargeNegative()
		{
			var value = Decimal.MinValue + 1;
			Writer.WriteOptimized(value);
			CheckValue(13, 14, value, Reader.ReadOptimizedDecimal());
		}

		[Test]
		public void CheckDecimalOptimizedAsZero()
		{
			Decimal value = 0;
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedDecimal());
		}

		[Test]
		public void CheckDecimalOptimizedAsOne()
		{
			Decimal value = 1;
			Writer.WriteOptimized(value);
			CheckValue(2, 1, value, Reader.ReadOptimizedDecimal());
		}

		public void CheckDecimalOptimizedAsMinValue()
		{
			var value = Decimal.MinValue;
			Writer.WriteOptimized(value);
			CheckValue(16, 13, value, Reader.ReadOptimizedDecimal());
		}

		public void CheckDecimalOptimizedAsMaxValue()
		{
			var value = Decimal.MaxValue;
			Writer.WriteOptimized(value);
			CheckValue(16, 13, value, Reader.ReadOptimizedDecimal());
		}

		[Test]
		public void CheckDouble()
		{
			Double value = 33;
			Writer.Write(value);
			CheckValue(8, 9, value, Reader.ReadDouble());
		}

		[Test]
		public void CheckDoubleLarge()
		{
			var value = Double.MaxValue - 1;
			Writer.Write(value);
			CheckValue(8, 9, value, Reader.ReadDouble());
		}

		[Test]
		public void CheckDoubleNegative()
		{
			Double value = -33;
			Writer.Write(value);
			CheckValue(8, 9, value, Reader.ReadDouble());
		}

		[Test]
		public void CheckDoubleLargeNegative()
		{
			var value = Double.MinValue + 1;
			Writer.Write(value);
			CheckValue(8, 9, value, Reader.ReadDouble());
		}

		[Test]
		public void CheckDoubleAsZero()
		{
			Double value = 0;
			Writer.Write(value);
			CheckValue(8, 1, value, Reader.ReadDouble());
		}

		[Test]
		public void CheckDoubleAsOne()
		{
			Double value = 1;
			Writer.Write(value);
			CheckValue(8, 1, value, Reader.ReadDouble());
		}

		[Test]
		public void CheckDoubleAsMinValue()
		{
			var value = Double.MinValue;
			Writer.Write(value);
			CheckValue(8, 9, value, Reader.ReadDouble());
		}

		[Test]
		public void CheckDoubleAsMaxValue()
		{
			var value = Double.MaxValue;
			Writer.Write(value);
			CheckValue(8, 9, value, Reader.ReadDouble());
		}

		[Test]
		public void CheckSingle()
		{
			Single value = 33;
			Writer.Write(value);
			CheckValue(4, 5, value, Reader.ReadSingle());
		}

		[Test]
		public void CheckSingleLarge()
		{
			var value = Single.MaxValue - 1;
			Writer.Write(value);
			CheckValue(4, 5, value, Reader.ReadSingle());
		}

		[Test]
		public void CheckSingleNegative()
		{
			Single value = -33;
			Writer.Write(value);
			CheckValue(4, 5, value, Reader.ReadSingle());
		}

		[Test]
		public void CheckSingleLargeNegative()
		{
			var value = Single.MinValue + 1;
			Writer.Write(value);
			CheckValue(4, 5, value, Reader.ReadSingle());
		}

		[Test]
		public void CheckSingleAsZero()
		{
			Single value = 0;
			Writer.Write(value);
			CheckValue(4, 1, value, Reader.ReadSingle());
		}

		[Test]
		public void CheckSingleAsOne()
		{
			Single value = 1;
			Writer.Write(value);
			CheckValue(4, 1, value, Reader.ReadSingle());
		}

		[Test]
		public void CheckSingleAsMinValue()
		{
			var value = Single.MinValue;
			Writer.Write(value);
			CheckValue(4, 5, value, Reader.ReadSingle());
		}

		[Test]
		public void CheckSingleAsMaxValue()
		{
			var value = Single.MaxValue;
			Writer.Write(value);
			CheckValue(4, 5, value, Reader.ReadSingle());
		}

		[Test]
		public void CheckInt16()
		{
			Int16 value = 33;
			Writer.Write(value);
			CheckValue(2, 2, value, Reader.ReadInt16());
		}

		[Test]
		public void CheckInt16Large()
		{
			Int16 value = Int16.MaxValue - 1;
			Writer.Write(value);
			CheckValue(2, 3, value, Reader.ReadInt16());
		}

		[Test]
		public void CheckInt16Negative()
		{
			Int16 value = -33;
			Writer.Write(value);
			CheckValue(2, 2, value, Reader.ReadInt16());
		}

		[Test]
		public void CheckInt16NegativeAtOptimizableLimit()
		{
			Int16 value = -(SerializationWriter.HighestOptimizable16BitValue + 1);
			Writer.Write(value);
			CheckValue(2, 2, value, Reader.ReadInt16());
		}

		[Test]
		public void CheckInt16NegativeOutsideOptimizableLimit()
		{
			Int16 value = -(SerializationWriter.HighestOptimizable16BitValue + 2);
			Writer.Write(value);
			CheckValue(2, 3, value, Reader.ReadInt16());
		}

		[Test]
		public void CheckInt16LargeNegative()
		{
			Int16 value = Int16.MinValue + 1;
			Writer.Write(value);
			CheckValue(2, 3, value, Reader.ReadInt16());
		}

		[Test]
		public void CheckInt16AsZero()
		{
			Int16 value = 0;
			Writer.Write(value);
			CheckValue(2, 1, value, Reader.ReadInt16());
		}

		[Test]
		public void CheckInt16AsMinusOne()
		{
			Int16 value = -1;
			Writer.Write(value);
			CheckValue(2, 1, value, Reader.ReadInt16());
		}

		[Test]
		public void CheckInt16AsOne()
		{
			Int16 value = 1;
			Writer.Write(value);
			CheckValue(2, 1, value, Reader.ReadInt16());
		}

		[Test]
		public void CheckInt16AsMinValue()
		{
			var value = Int16.MinValue;
			Writer.Write(value);
			CheckValue(2, 3, value, Reader.ReadInt16());
		}

		[Test]
		public void CheckInt16AsMaxValue()
		{
			var value = Int16.MaxValue;
			Writer.Write(value);
			CheckValue(2, 3, value, Reader.ReadInt16());
		}

		[Test]
		public void CheckInt16Optimized()
		{
			Int16 value = 33;
			Writer.WriteOptimized(value);
			CheckValue(1, 2, value, Reader.ReadOptimizedInt16());
		}

		[Test]
		public void CheckInt16OptimizedAsZero()
		{
			Int16 value = 0;
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedInt16());
		}

		[Test]
		public void CheckInt16OptimizedAsOne()
		{
			Int16 value = 1;
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedInt16());
		}


		[Test]
		public void CheckInt32()
		{
			var value = 33;
			Writer.Write(value);
			CheckValue(4, 2, value, Reader.ReadInt32());
		}

		[Test]
		public void CheckInt32Large()
		{
			var value = Int32.MaxValue - 1;
			Writer.Write(value);
			CheckValue(4, 5, value, Reader.ReadInt32());
		}

		[Test]
		public void CheckInt32Negative()
		{
			var value = -33;
			Writer.Write(value);
			CheckValue(4, 2, value, Reader.ReadInt32());
		}

		[Test]
		public void CheckInt32NegativeAtOptimizableLimit()
		{
			var value = -(SerializationWriter.HighestOptimizable32BitValue + 1);
			Writer.Write(value);
			CheckValue(4, 4, value, Reader.ReadInt32());
		}

		[Test]
		public void CheckInt32NegativeOutsideOptimizableLimit()
		{
			var value = -(SerializationWriter.HighestOptimizable32BitValue + 2);
			Writer.Write(value);
			CheckValue(4, 5, value, Reader.ReadInt32());
		}


		[Test]
		public void CheckInt32LargeNegative()
		{
			var value = Int32.MinValue + 1;
			Writer.Write(value);
			CheckValue(4, 5, value, Reader.ReadInt32());
		}

		[Test]
		public void CheckInt32AsZero()
		{
			var value = 0;
			Writer.Write(value);
			CheckValue(4, 1, value, Reader.ReadInt32());
		}

		[Test]
		public void CheckInt32AsMinusOne()
		{
			var value = -1;
			Writer.Write(value);
			CheckValue(4, 1, value, Reader.ReadInt32());
		}

		[Test]
		public void CheckInt32AsOne()
		{
			var value = 1;
			Writer.Write(value);
			CheckValue(4, 1, value, Reader.ReadInt32());
		}

		[Test]
		public void CheckInt32AsMinValue()
		{
			var value = Int32.MinValue;
			Writer.Write(value);
			CheckValue(4, 5, value, Reader.ReadInt32());
		}

		[Test]
		public void CheckInt32AsMaxValue()
		{
			var value = Int32.MaxValue;
			Writer.Write(value);
			CheckValue(4, 5, value, Reader.ReadInt32());
		}

		[Test]
		public void CheckInt32Optimized()
		{
			var value = 33;
			Writer.WriteOptimized(value);
			CheckValue(1, 2, value, Reader.ReadOptimizedInt32());
		}

		[Test]
		public void CheckInt32OptimizedAsZero()
		{
			var value = 0;
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedInt32());
		}
	
		[Test]
		public void CheckInt32OptimizedAsOne()
		{
			var value = 1;
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedInt32());
		}

		[Test]
		public void CheckInt64()
		{
			Int64 value = 33;
			Writer.Write(value);
			CheckValue(8, 2, value, Reader.ReadInt64());
		}

		[Test]
		public void CheckInt64Large()
		{
			var value = Int64.MaxValue - 1;
			Writer.Write(value);
			CheckValue(8, 9, value, Reader.ReadInt64());
		}

		[Test]
		public void CheckInt64Negative()
		{
			Int64 value = -33;
			Writer.Write(value);
			CheckValue(8, 2, value, Reader.ReadInt64());
		}

		[Test]
		public void CheckInt64NegativeAtOptimizableLimit()
		{
			var value = -(SerializationWriter.HighestOptimizable64BitValue + 1);
			Writer.Write(value);
			CheckValue(8, 8, value, Reader.ReadInt64());
		}

		[Test]
		public void CheckInt64NegativeOutsideOptimizableLimit()
		{
			var value = -(SerializationWriter.HighestOptimizable64BitValue + 2);
			Writer.Write(value);
			CheckValue(8, 9, value, Reader.ReadInt64());
		}

		[Test]
		public void CheckInt64LargeNegative()
		{
			var value = Int64.MinValue + 1;
			Writer.Write(value);
			CheckValue(8, 9, value, Reader.ReadInt64());
		}

		[Test]
		public void CheckInt64AsZero()
		{
			Int64 value = 0;
			Writer.Write(value);
			CheckValue(8, 1, value, Reader.ReadInt64());
		}

		[Test]
		public void CheckInt64AsMinusOne()
		{
			Int64 value = -1;
			Writer.Write(value);
			CheckValue(8, 1, value, Reader.ReadInt64());
		}

		[Test]
		public void CheckInt64AsOne()
		{
			Int64 value = 1;
			Writer.Write(value);
			CheckValue(8, 1, value, Reader.ReadInt64());
		}

		[Test]
		public void CheckInt64AsMinValue()
		{
			var value = Int64.MinValue;
			Writer.Write(value);
			CheckValue(8, 9, value, Reader.ReadInt64());
		}

		[Test]
		public void CheckInt64AsMaxValue()
		{
			var value = Int64.MaxValue;
			Writer.Write(value);
			CheckValue(8, 9, value, Reader.ReadInt64());
		}

		[Test]
		public void CheckInt64Optimized()
		{
			Int64 value = 33;
			Writer.WriteOptimized(value);
			CheckValue(1, 2, value, Reader.ReadOptimizedInt64());
		}

		[Test]
		public void CheckInt64OptimizedAsZero()
		{
			Int64 value = 0;
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedInt64());
		}

		[Test]
		public void CheckInt64OptimizedAsOne()
		{
			Int64 value = 1;
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedInt64());
		}

		[Test]
		public void CheckUInt16()
		{
			UInt16 value = 33;
			Writer.Write(value);
			CheckValue(2, 2, value, Reader.ReadUInt16());
		}

		[Test]
		public void CheckUInt16Large()
		{
			UInt16 value = UInt16.MaxValue - 1;
			Writer.Write(value);
			CheckValue(2, 3, value, Reader.ReadUInt16());
		}

		[Test]
		public void CheckUInt16AsZero()
		{
			UInt16 value = 0;
			Writer.Write(value);
			CheckValue(2, 1, value, Reader.ReadUInt16());
		}

		[Test]
		public void CheckUInt16AsOne()
		{
			UInt16 value = 1;
			Writer.Write(value);
			CheckValue(2, 1, value, Reader.ReadUInt16());
		}

		[Test]
		public void CheckUInt16AsMaxValue()
		{
			var value = UInt16.MaxValue;
			Writer.Write(value);
			CheckValue(2, 3, value, Reader.ReadUInt16());
		}

		[Test]
		public void CheckUInt16Optimized()
		{
			UInt16 value = 33;
			Writer.WriteOptimized(value);
			CheckValue(1, 2, value, Reader.ReadOptimizedUInt16());
		}

		[Test]
		public void CheckUInt16OptimizedAsZero()
		{
			UInt16 value = 0;
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedUInt16());
		}

		[Test]
		public void CheckUInt16OptimizedAsOne()
		{
			UInt16 value = 1;
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedUInt16());
		}

		[Test]
		public void CheckUInt32()
		{
			UInt32 value = 33;
			Writer.Write(value);
			CheckValue(4, 2, value, Reader.ReadUInt32());
		}

		[Test]
		public void CheckUInt32Large()
		{
			var value = UInt32.MaxValue - 1;
			Writer.Write(value);
			CheckValue(4, 5, value, Reader.ReadUInt32());
		}

		[Test]
		public void CheckUInt32AsZero()
		{
			UInt32 value = 0;
			Writer.Write(value);
			CheckValue(4, 1, value, Reader.ReadUInt32());
		}

		[Test]
		public void CheckUInt32AsOne()
		{
			UInt32 value = 1;
			Writer.Write(value);
			CheckValue(4, 1, value, Reader.ReadUInt32());
		}

		[Test]
		public void CheckUInt32AsMaxValue()
		{
			var value = UInt32.MaxValue;
			Writer.Write(value);
			CheckValue(4, 5, value, Reader.ReadUInt32());
		}

		[Test]
		public void CheckUInt32Optimized()
		{
			UInt32 value = 33;
			Writer.WriteOptimized(value);
			CheckValue(1, 2, value, Reader.ReadOptimizedUInt32());
		}

		[Test]
		public void CheckUInt32OptimizedAsZero()
		{
			UInt32 value = 0;
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedUInt32());
		}

		[Test]
		public void CheckUInt32OptimizedAsOne()
		{
			UInt32 value = 1;
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedUInt32());
		}

		[Test]
		public void CheckUInt64()
		{
			UInt64 value = 33;
			Writer.Write(value);
			CheckValue(8, 2, value, Reader.ReadUInt64());
		}

		[Test]
		public void CheckUInt64Large()
		{
			var value = UInt64.MaxValue - 1;
			Writer.Write(value);
			CheckValue(8, 9, value, Reader.ReadUInt64());
		}

		[Test]
		public void CheckUInt64AsZero()
		{
			UInt64 value = 0;
			Writer.Write(value);
			CheckValue(8, 1, value, Reader.ReadUInt64());
		}

		[Test]
		public void CheckUInt64AsOne()
		{
			UInt64 value = 1;
			Writer.Write(value);
			CheckValue(8, 1, value, Reader.ReadUInt64());
		}

		[Test]
		public void CheckUInt64AsMaxValue()
		{
			var value = UInt64.MaxValue;
			Writer.Write(value);
			CheckValue(8, 9, value, Reader.ReadUInt64());
		}

		[Test]
		public void CheckString()
		{
			var value = "Fast";
			Writer.WriteOptimized(value);
			CheckValue(2, 2, value, Reader.ReadOptimizedString());
		}

		[Test]
		public void CheckStringAsNull()
		{
			string value = null;
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedString());
		}

		[Test]
		public void CheckStringAsEmpty()
		{
			var value = "";
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedString());
		}

		[Test]
		public void CheckStringAsSingleChar()
		{
			var value = "X";
			Writer.WriteOptimized(value);
			CheckValue(2, 2, value, Reader.ReadOptimizedString());
		}

		[Test]
		public void CheckStringAsY()
		{
			var value = "Y";
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedString());
		}

		[Test]
		public void CheckStringAsN()
		{
			var value = "N";
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedString());
		}

		[Test]
		public void CheckStringDirect()
		{
			var value = "Fast";
			Writer.WriteStringDirect(value);
			CheckValue(5, 2, value, Reader.ReadStringDirect());
		}

		[Test]
		public void CheckStringDirectAsEmpty()
		{
			var value = "";
			Writer.WriteStringDirect(value);
			CheckValue(1, 1, value, Reader.ReadStringDirect());
		}

		[Test]
		public void CheckStringDirectAsSingleChar()
		{
			var value = "X";
			Writer.WriteStringDirect(value);
			CheckValue(2, 2, value, Reader.ReadStringDirect());
		}

		[Test]
		public void CheckDateTime()
		{
			var value = new DateTime(2006, 11, 16, 10, 31, 11, 11).AddTicks(1);
			Writer.Write(value);
			CheckValue(8, 9, value, Reader.ReadDateTime());
		}

		[Test]
		public void CheckDateTimeAsMinValue()
		{
			var value = DateTime.MinValue;
			Writer.Write(value);
			CheckValue(8, 1, value, Reader.ReadDateTime());
		}

		[Test]
		public void CheckDateTimeAsMaxValue()
		{
			var value = DateTime.MaxValue;
			Writer.Write(value);
			CheckValue(8, 1, value, Reader.ReadDateTime());
		}

		[Test]
		public void CheckTimeSpan()
		{
			var value = TimeSpan.FromDays(1);
			Writer.Write(value);
			CheckValue(8, 4, value, Reader.ReadTimeSpan());
		}

		[Test]
		public void CheckTimeSpanNegative()
		{
			var value = TimeSpan.FromDays(-6.44);
			Writer.Write(value);
			CheckValue(8, 5, value, Reader.ReadTimeSpan());
		}

		[Test]
		public void CheckTimeSpanMinValue()
		{
			var value = TimeSpan.MinValue;
			Writer.Write(value);
			CheckValue(8, 9, value, Reader.ReadTimeSpan());
		}

		[Test]
		public void CheckTimeSpanMaxValue()
		{
			var value = TimeSpan.MaxValue;
			Writer.Write(value);
			CheckValue(8, 9, value, Reader.ReadTimeSpan());
		}

		[Test]
		public void CheckTimeSpanMaxValueNoOddTicks()
		{
			var value = new TimeSpan(TimeSpan.MaxValue.Ticks - (TimeSpan.MaxValue.Ticks % TimeSpan.TicksPerMillisecond));
			Writer.Write(value);
			CheckValue(8, 9, value, Reader.ReadTimeSpan());
		}

		[Test]
		public void CheckTimeSpanMaxDaysValue()
		{
			var value = new TimeSpan((int) TimeSpan.MaxValue.TotalDays, 0, 0);
			Writer.Write(value);
			CheckValue(8, 6, value, Reader.ReadTimeSpan());
		}

		[Test]
		public void CheckTimeSpanAsZero()
		{
			var value = TimeSpan.Zero;
			Writer.Write(value);
			CheckValue(8, 1, value, Reader.ReadTimeSpan());
		}

		[Test]
		public void CheckTimeSpanOptimized()
		{
			var value = TimeSpan.FromDays(1);
			Writer.WriteOptimized(value);
			CheckValue(3, 4, value, Reader.ReadOptimizedTimeSpan());
		}

		[Test]
		public void CheckTimeSpanOptimizedNegative()
		{
			var value = TimeSpan.FromDays(-6.44);
			Writer.WriteOptimized(value);
			CheckValue(4, 5, value, Reader.ReadOptimizedTimeSpan());
		}

		[Test]
		public void CheckTimeSpanOptimizedMaxValueNoOddTicks()
		{
			var value = new TimeSpan(TimeSpan.MaxValue.Ticks - (TimeSpan.MaxValue.Ticks % TimeSpan.TicksPerMillisecond));
			Writer.WriteOptimized(value);
			CheckValue(8, 9, value, Reader.ReadOptimizedTimeSpan());
		}

		[Test]
		public void CheckTimeSpanOptimizedMaxDaysValue()
		{
			var value = new TimeSpan((int) TimeSpan.MaxValue.TotalDays, 0, 0);
			Writer.WriteOptimized(value);
			CheckValue(5, 6, value, Reader.ReadOptimizedTimeSpan());
		}

		[Test]
		public void CheckTimeSpanOptimizedMinValueNoOddTicks()
		{
			var value = new TimeSpan(TimeSpan.MinValue.Ticks - (TimeSpan.MinValue.Ticks % TimeSpan.TicksPerMillisecond));
			Writer.WriteOptimized(value);
			CheckValue(8, 9, value, Reader.ReadOptimizedTimeSpan());
		}

		[Test]
		public void CheckTimeSpanOptimizedMinDaysValue()
		{
			var value = new TimeSpan((int) TimeSpan.MinValue.TotalDays, 0, 0);
			Writer.WriteOptimized(value);
			CheckValue(5, 6, value, Reader.ReadOptimizedTimeSpan());
		}

		[Test]
		public void CheckTimeSpanOptimizedAsZero()
		{
			var value = TimeSpan.Zero;
			Writer.WriteOptimized(value);
			CheckValue(2, 1, value, Reader.ReadOptimizedTimeSpan());
		}

		[Test]
		public void CheckTimeSpanOptimizedSecondsOnly()
		{
			var value = TimeSpan.FromSeconds(30);
			Writer.WriteOptimized(value);
			CheckValue(2, 3, value, Reader.ReadOptimizedTimeSpan());
		}

		[Test]
		public void CheckTimeSpanOptimizedMinutesOnly()
		{
			var value = TimeSpan.FromMinutes(59);
			Writer.WriteOptimized(value);
			CheckValue(2, 3, value, Reader.ReadOptimizedTimeSpan());
		}

		[Test]
		public void CheckTimeSpanOptimizedHoursOnly()
		{
			var value = TimeSpan.FromHours(23);
			Writer.WriteOptimized(value);
			CheckValue(2, 3, value, Reader.ReadOptimizedTimeSpan());
		}

		[Test]
		public void CheckTimeSpanOptimizedMillisecondsOnly()
		{
			var value = TimeSpan.FromMilliseconds(999);
			Writer.WriteOptimized(value);
			CheckValue(4, 5, value, Reader.ReadOptimizedTimeSpan());
		}

		[Test]
		public void CheckTimeSpanOptimizedTimeOnly()
		{
			var value = new TimeSpan(21, 10, 0);
			Writer.WriteOptimized(value);
			CheckValue(2, 3, value, Reader.ReadOptimizedTimeSpan());
		}

		[Test]
		public void CheckTimeSpanOptimizedTimeAndSeconds()
		{
			var value = new TimeSpan(0, 21, 10, 1);
			Writer.WriteOptimized(value);
			CheckValue(3, 4, value, Reader.ReadOptimizedTimeSpan());
		}

		[Test]
		public void CheckTimeSpanOptimizedTimeAndMilliseconds()
		{
			var value = new TimeSpan(0, 21, 10, 1);
			value = value.Add(TimeSpan.FromMilliseconds(4));
			Writer.WriteOptimized(value);
			CheckValue(4, 5, value, Reader.ReadOptimizedTimeSpan());
		}

		[Test]
		public void CheckTimeSpanOptimizedDayAndSecondsOnly()
		{
			var value = TimeSpan.FromSeconds(30);
			value = value.Add(TimeSpan.FromDays(14));
			Writer.WriteOptimized(value);
			CheckValue(3, 4, value, Reader.ReadOptimizedTimeSpan());
		}

		[Test]
		public void CheckTimeSpanOptimizedDayAndMinutesOnly()
		{
			var value = TimeSpan.FromMinutes(59);
			value = value.Add(TimeSpan.FromDays(14));
			Writer.WriteOptimized(value);
			CheckValue(3, 4, value, Reader.ReadOptimizedTimeSpan());
		}

		[Test]
		public void CheckTimeSpanOptimizedDayAndHoursOnly()
		{
			var value = TimeSpan.FromHours(23);
			value = value.Add(TimeSpan.FromDays(14));
			Writer.WriteOptimized(value);
			CheckValue(3, 4, value, Reader.ReadOptimizedTimeSpan());
		}

		[Test]
		public void CheckTimeSpanOptimizedDayAndMillisecondsOnly()
		{
			var value = TimeSpan.FromMilliseconds(999);
			value = value.Add(TimeSpan.FromDays(14));
			Writer.WriteOptimized(value);
			CheckValue(5, 6, value, Reader.ReadOptimizedTimeSpan());
		}

		[Test]
		public void CheckTimeSpanOptimizedDayAndTimeOnly()
		{
			var value = new TimeSpan(21, 10, 0);
			value = value.Add(TimeSpan.FromDays(14));
			Writer.WriteOptimized(value);
			CheckValue(3, 4, value, Reader.ReadOptimizedTimeSpan());
		}

		[Test]
		public void CheckTimeSpanOptimizedDayAndTimeAndSeconds()
		{
			var value = new TimeSpan(0, 21, 10, 1);
			value = value.Add(TimeSpan.FromDays(140000));
			Writer.WriteOptimized(value);
			CheckValue(6, 7, value, Reader.ReadOptimizedTimeSpan());
		}

		[Test]
		public void CheckTimeSpanOptimizedTimeDayAndMilliseconds()
		{
			var value = new TimeSpan(0, 21, 10, 1);
			value = value.Add(TimeSpan.FromMilliseconds(4));
			value = value.Add(TimeSpan.FromDays(140000));
			Writer.WriteOptimized(value);
			CheckValue(7, 8, value, Reader.ReadOptimizedTimeSpan());
		}

		[Test]
		public void CheckGuid()
		{
			var value = Guid.NewGuid();
			Writer.Write(value);
			CheckValue(16, 17, value, Reader.ReadGuid());
		}

		[Test]
		public void CheckGuidAsEmpty()
		{
			var value = Guid.Empty;
			Writer.Write(value);
			CheckValue(16, 1, value, Reader.ReadGuid());
		}

		[Test]
		public void CheckObjectArray()
		{
			// 1+2 1+5 1+8 1 1+16   + 1 for length
			var value = new object[] {132, "Fast", 10.64, DateTime.MinValue, Guid.NewGuid()};
			Writer.Write(value);
			CheckValue(34, 34, value, Reader.ReadObjectArray());
		}

		[Test]
		public void CheckObjectArrayAsNull()
		{
			object[] value = null;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadObjectArray());
		}

		[Test]
		public void CheckObjectArrayAsEmpty()
		{
			var value = new object[0];
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadObjectArray());
		}

		[Test]
		public void CheckObjectArrayWithAllEmbeddedNulls()
		{
			var value = new object[20];
			Writer.Write(value);
			CheckValue(4, 4, value, Reader.ReadObjectArray());
		}

		[Test]
		public void CheckObjectArrayWithAllEmbeddedDBNulls()
		{
			var value = new object[20];
			for(var i = 0; i < value.Length; i++) value[i] = DBNull.Value;
			Writer.Write(value);
			CheckValue(4, 4, value, Reader.ReadObjectArray());
		}

		[Test]
		public void CheckObjectArrayWithEmbeddedNullsAtEnd()
		{
			var value = new object[20];
			for(var i = 0; i < 10; i++) value[i] = i;
			Writer.Write(value);
			CheckValue(22, 22, value, Reader.ReadObjectArray());
		}

		[Test]
		public void CheckObjectArrayWithEmbeddedDBNullsAtEnd()
		{
			var value = new object[20];
			for(var i = 0; i < value.Length; i++) value[i] = DBNull.Value;
			for(var i = 0; i < 10; i++) value[i] = i;
			Writer.Write(value);
			CheckValue(22, 22, value, Reader.ReadObjectArray());
		}

		[Test]
		public void CheckObjectArrayWithEmbeddedNullsAtStart()
		{
			var value = new object[20];
			for(var i = 10; i < 20; i++) value[i] = i - 10;
			Writer.Write(value);
			CheckValue(22, 22, value, Reader.ReadObjectArray());
		}

		[Test]
		public void CheckObjectArrayWithEmbeddedDBNullsAtStart()
		{
			var value = new object[20];
			for(var i = 0; i < value.Length; i++) value[i] = DBNull.Value;
			for(var i = 10; i < 20; i++) value[i] = i - 10;
			Writer.Write(value);
			CheckValue(22, 22, value, Reader.ReadObjectArray());
		}

		[Test]
		public void CheckObjectArrayWithEmbeddedNullsInMiddle()
		{
			var value = new object[20];
			for(var i = 0; i < 5; i++) value[i] = i;
			for(var i = 15; i < 20; i++) value[i] = i;
			Writer.Write(value);
			CheckValue(22, 22, value, Reader.ReadObjectArray());
		}

		[Test]
		public void CheckObjectArrayWithEmbeddedDBNullsInMiddle()
		{
			var value = new object[20];
			for(var i = 0; i < value.Length; i++) value[i] = DBNull.Value;
			for(var i = 0; i < 5; i++) value[i] = i;
			for(var i = 15; i < 20; i++) value[i] = i;
			Writer.Write(value);
			CheckValue(22, 22, value, Reader.ReadObjectArray());
		}

		[Test]
		public void CheckObjectArrayWithAllEmbeddedNullsExceptOneAtStart()
		{
			var value = new object[20];
			value[0] = 1;
			Writer.Write(value);
			CheckValue(5, 5, value, Reader.ReadObjectArray());
		}

		[Test]
		public void CheckObjectArrayWithAllEmbeddedDBNullsExceptOneAtStart()
		{
			var value = new object[20];
			for(var i = 0; i < value.Length; i++) value[i] = DBNull.Value;
			value[0] = 1;
			Writer.Write(value);
			CheckValue(5, 5, value, Reader.ReadObjectArray());
		}

		[Test]
		public void CheckObjectArrayWithAllEmbeddedNullsExceptOneAtEnd()
		{
			var value = new object[20];
			value[19] = 1;
			Writer.Write(value);
			CheckValue(5, 5, value, Reader.ReadObjectArray());
		}

		[Test]
		public void CheckObjectArrayWithAllEmbeddedDBNullsExceptOneAtEnd()
		{
			var value = new object[20];
			for(var i = 0; i < value.Length; i++) value[i] = DBNull.Value;
			value[19] = 1;
			Writer.Write(value);
			CheckValue(5, 5, value, Reader.ReadObjectArray());
		}

		[Test]
		public void CheckOptimizedObjectArray()
		{
			// 1+2 1+5 1+8 1 1+16   + 1 for length
			var value = new object[] {132, "Fast", 10.64, DateTime.MinValue, Guid.NewGuid()};
			Writer.WriteOptimized(value);
			CheckValue(33, 34, value, Reader.ReadOptimizedObjectArray());
		}

		[Test]
		public void CheckObjectArrayNotNullAsEmpty()
		{
			var value = new object[0];
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedObjectArray());
		}

		[Test]
		public void CheckObjectArrayNotNullWithAllEmbeddedNulls()
		{
			var value = new object[20];
			Writer.WriteOptimized(value);
			CheckValue(3, 4, value, Reader.ReadOptimizedObjectArray());
		}

		[Test]
		public void CheckObjectArrayNotNullWithEmbeddedNullsAtEnd()
		{
			var value = new object[20];
			for(var i = 0; i < 10; i++) value[i] = i;
			Writer.WriteOptimized(value);
			CheckValue(21, 22, value, Reader.ReadOptimizedObjectArray());
		}

		[Test]
		public void CheckObjectArrayNotNullWithEmbeddedNullsAtStart()
		{
			var value = new object[20];
			for(var i = 10; i < 20; i++) value[i] = i - 10;
			Writer.WriteOptimized(value);
			CheckValue(21, 22, value, Reader.ReadOptimizedObjectArray());
		}

		[Test]
		public void CheckObjectArrayNotNullWithEmbeddedNullsInMiddle()
		{
			var value = new object[20];
			for(var i = 0; i < 5; i++) value[i] = i;
			for(var i = 15; i < 20; i++) value[i] = i;
			Writer.WriteOptimized(value);
			CheckValue(21, 22, value, Reader.ReadOptimizedObjectArray());
		}

		[Test]
		public void CheckObjectArrayNotNullWithAllEmbeddedNullsExceptOneAtStart()
		{
			var value = new object[20];
			value[0] = 1;
			Writer.WriteOptimized(value);
			CheckValue(4, 5, value, Reader.ReadOptimizedObjectArray());
		}

		[Test]
		public void CheckObjectArrayNotNullWithAllEmbeddedNullsExceptOneAtEnd()
		{
			var value = new object[20];
			value[19] = 1;
			Writer.WriteOptimized(value);
			CheckValue(4, 5, value, Reader.ReadOptimizedObjectArray());
		}

		[Test]
		public void CheckBitVector32()
		{
			var value = new BitVector32();
			Writer.Write(value);
			CheckValue(4, 5, value, Reader.ReadBitVector32());
		}

		[Test]
		public void CheckBitVector32Optimized()
		{
			var value = new BitVector32();
			Writer.WriteOptimized(value);
			CheckValue(1, 5, value, Reader.ReadOptimizedBitVector32());
		}

		[Test]
		public void CheckArrayList()
		{
			var value = new ArrayList { 123456, "ABC" };
			Writer.Write(value);
			CheckValue(8, 8, value, Reader.ReadArrayList(), new ListComparer());
		}

		[Test]
		public void CheckArrayListNull()
		{
			ArrayList value = null;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadArrayList());
		}

		[Test]
		public void CheckArrayListEmpty()
		{
			var value = new ArrayList();
			Writer.Write(value);
			CheckValue(2, 2, value, Reader.ReadArrayList(), new ListComparer());
		}

		[Test]
		public void CheckArrayListOptimized()
		{
			var value = new ArrayList { 123456, "ABC" };
			Writer.WriteOptimized(value);
			CheckValue(7, 8, value, Reader.ReadOptimizedArrayList(), new ListComparer());
		}

		[Test]
		public void CheckArrayListOptimizedEmpty()
		{
			var value = new ArrayList();
			Writer.WriteOptimized(value);
			CheckValue(1, 2, value, Reader.ReadOptimizedArrayList(), new ListComparer());
		}

		[Test]
		public void CheckObjectArrayPairAsAllNulls()
		{
			var values1 = new object[10];
			var values2 = new object[10];
			Writer.WriteOptimized(values1, values2);
			object[] newValues1, newValues2;
			Reader.ReadOptimizedObjectArrayPair(out newValues1, out newValues2);
			CheckValue(5, -1, values1, newValues1);
			CheckValue(-1, -1, values2, newValues2);
		}

		[Test]
		public void CheckObjectArrayPairAsAllDBNulls()
		{
			var values1 = new object[10];
			var values2 = new object[10];
			for(var i = 0; i < values1.Length; i++) values1[i] = values2[i] = DBNull.Value;
			Writer.WriteOptimized(values1, values2);
			object[] newValues1, newValues2;
			Reader.ReadOptimizedObjectArrayPair(out newValues1, out newValues2);
			CheckValue(5, -1, values1, newValues1);
			CheckValue(-1, -1, values2, newValues2);
		}

		[Test]
		public void CheckObjectArrayPairAsDifferentNullTypes()
		{
			var values1 = new object[10];
			var values2 = new object[10];
			for(var i = 0; i < values1.Length; i++) values1[i] = DBNull.Value;
			Writer.WriteOptimized(values1, values2);
			object[] newValues1, newValues2;
			Reader.ReadOptimizedObjectArrayPair(out newValues1, out newValues2);
			CheckValue(5, -1, values1, newValues1);
			CheckValue(-1, -1, values2, newValues2);
		}

		[Test]
		public void CheckObjectArrayPairAsSameIntValues()
		{
			var values1 = new object[10];
			var values2 = new object[10];
			for(var i = 0; i < values1.Length; i++) values1[i] = values2[i] = i + 20;
			Writer.WriteOptimized(values1, values2);
			object[] newValues1, newValues2;
			Reader.ReadOptimizedObjectArrayPair(out newValues1, out newValues2);
			CheckValue(23, -1, values1, newValues1);
			CheckValue(-1, -1, values2, newValues2);
		}

		[Test]
		public void CheckObjectArrayPairAsSameStringValues()
		{
			var values1 = new object[10];
			var values2 = new object[10];
			for(var i = 0; i < values1.Length; i++) values1[i] = values2[i] = "abc";
			Writer.WriteOptimized(values1, values2);
			object[] newValues1, newValues2;
			Reader.ReadOptimizedObjectArrayPair(out newValues1, out newValues2);
			CheckValue(7, -1, values1, newValues1);
			CheckValue(-1, -1, values2, newValues2);
		}

		[Test]
		public void CheckObjectArrayPairAsDifferentIntValues()
		{
			var values1 = new object[10];
			var values2 = new object[10];
			for(var i = 0; i < values1.Length; i++)
			{
				values1[i] = -(i + 20);
				values2[i] = -(i + 21);
			}
			Writer.WriteOptimized(values1, values2);
			object[] newValues1, newValues2;
			Reader.ReadOptimizedObjectArrayPair(out newValues1, out newValues2);
			CheckValue(41, -1, values1, newValues1);
			CheckValue(-1, -1, values2, newValues2);
		}

		[Test]
		public void CheckObjectArrayPairAsDifferentStringValues()
		{
			var values1 = new object[10];
			var values2 = new object[10];
			for(var i = 0; i < values1.Length; i++)
			{
				values1[i] = "abc";
				values2[i] = "abcd";
			}
			Writer.WriteOptimized(values1, values2);
			object[] newValues1, newValues2;
			Reader.ReadOptimizedObjectArrayPair(out newValues1, out newValues2);
			CheckValue(25, -1, values1, newValues1);
			CheckValue(-1, -1, values2, newValues2);
		}

		[Test]
		public void CheckObjectArrayPairAsPartlyDifferentLongValues()
		{
			var values1 = new object[10];
			var values2 = new object[10];
			for(var i = 0; i < values1.Length; i++)
			{
				values1[i] = (long) -(i + 20);
				values2[i] = (long) -(i + 20 + (i % 2));
			}
			Writer.WriteOptimized(values1, values2);
			object[] newValues1, newValues2;
			Reader.ReadOptimizedObjectArrayPair(out newValues1, out newValues2);
			CheckValue(36, -1, values1, newValues1);
			CheckValue(-1, -1, values2, newValues2);
		}

		[Test]
		public void CheckObjectArrayPairAsPartlyDifferentStringValues()
		{
			var values1 = new object[10];
			var values2 = new object[10];
			for(var i = 0; i < values1.Length; i++)
			{
				values1[i] = "abc";
				values2[i] = "abc" + (((i % 2) == 0) ? "" : "d");
			}
			Writer.WriteOptimized(values1, values2);
			object[] newValues1, newValues2;
			Reader.ReadOptimizedObjectArrayPair(out newValues1, out newValues2);
			CheckValue(20, -1, values1, newValues1);
			CheckValue(-1, -1, values2, newValues2);
		}

		[Test]
		public void CheckTypeAsNull()
		{
			Type value = null;
			Writer.Write(value, true);
			CheckValue(1, 1, value, Reader.ReadType());
		}

		[Test]
		public void CheckSystemTypeFullyQualified()
		{
			var value = typeof(string);
			Writer.Write(value, true);
			CheckValue(3, 3, value, Reader.ReadType());
			Assert.AreEqual(16 + typeof(string).AssemblyQualifiedName.Length, writerLength);
			Assert.AreEqual(29, objectWriterLength);
		}

		[Test]
		public void CheckSystemTypeNotFullyQualified()
		{
			var value = typeof(string);
			Writer.Write(value, false);
			CheckValue(3, 3, value, Reader.ReadType());
			Assert.AreEqual(29, writerLength);
			Assert.AreEqual(29, objectWriterLength);
		}

		[Test]
		public void CheckNonSystemTypeFullyQualified()
		{
			var value = typeof(DataTable);
			Writer.Write(value, true);
			CheckValue(3, 3, value, Reader.ReadType());
			Assert.AreEqual(16 + typeof(DataTable).AssemblyQualifiedName.Length, writerLength);
			Assert.AreEqual(16 + typeof(DataTable).AssemblyQualifiedName.Length, objectWriterLength);
		}

		[Test]
		public void CheckSystemOptimizedType()
		{
			var value = typeof(string);
			Writer.WriteOptimized(value);
			CheckValue(2, 3, value, Reader.ReadOptimizedType());
			Assert.AreEqual(28, writerLength);
			Assert.AreEqual(29, objectWriterLength);
		}

		[Test]
		public void CheckNonSystemOptimizedType()
		{
			var value = typeof(DataTable);
			Writer.WriteOptimized(value);
			CheckValue(2, 3, value, Reader.ReadOptimizedType());
			Assert.AreEqual(15 + typeof(DataTable).AssemblyQualifiedName.Length, writerLength);
			Assert.AreEqual(16 + typeof(DataTable).AssemblyQualifiedName.Length, objectWriterLength);
		}

		[Test]
		public void CheckFactoryClass()
		{
			object value = new SampleFactoryClass();
			Writer.WriteTokenizedObject(value);
			var value1 = Reader.ReadTokenizedObject();
			Assert.IsFalse(value == value1);
			Assert.AreEqual(50 + value.GetType().AssemblyQualifiedName.Length, Reader.BaseStream.Length);
		}

		[Test]
		public void CheckFactoryClassMultipleTokens()
		{
			object value = new SampleFactoryClass();
			Writer.WriteTokenizedObject(value);
			Writer.WriteTokenizedObject(value);
			var value1 = Reader.ReadTokenizedObject();
			var value2 = Reader.ReadTokenizedObject();
			Assert.IsFalse(value == value1);
			Assert.AreSame(value1, value2);
			Assert.AreEqual(51 + value.GetType().AssemblyQualifiedName.Length, Reader.BaseStream.Length);
		}

		[Test]
		public void CheckFactoryClassAsType()
		{
			object value = new SampleFactoryClass();
			Writer.WriteTokenizedObject(value, true);
			var value1 = Reader.ReadTokenizedObject();
			Assert.IsFalse(value == value1);

			// This test gets around a .Net serialization feature where the size is off by 1 *sometimes*
			var typeSize = (int) (Reader.BaseStream.Length - value.GetType().AssemblyQualifiedName.Length);
			Assert.IsTrue(typeSize == 15 || typeSize == 16);
		}

		[Test]
		public void CheckFactoryClassAsTypeMultipleTokens()
		{
			object value = new SampleFactoryClass();
			Writer.WriteTokenizedObject(value, true);
			Writer.WriteTokenizedObject(value, true);
			var value1 = Reader.ReadTokenizedObject();
			var value2 = Reader.ReadTokenizedObject();
			Assert.IsFalse(value == value1);
			Assert.AreSame(value1, value2);

			// This test gets around a .Net serialization feature where the size is off by 1 *sometimes*
			var typeSize = (int) (Reader.BaseStream.Length - value.GetType().AssemblyQualifiedName.Length);
			Assert.IsTrue(typeSize == 16 || typeSize == 17);
		}

		[Test]
		public void CheckInt16OptimizedRange()
		{
			Int16 value = 0;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(1, Writer.BaseStream.Position);
			value = 1;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(1, Writer.BaseStream.Position);
			value = 127;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(1, Writer.BaseStream.Position);

			value = 128;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(2, Writer.BaseStream.Position);
			value = 16383;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(2, Writer.BaseStream.Position);
		}

		[Test]
		public void CheckInt32OptimizedRange()
		{
			var value = 0;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(1, Writer.BaseStream.Position);
			value = 1;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(1, Writer.BaseStream.Position);
			value = 127;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(1, Writer.BaseStream.Position);

			value = 128;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(2, Writer.BaseStream.Position);
			value = 16383;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(2, Writer.BaseStream.Position);

			value = 16384;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(3, Writer.BaseStream.Position);
			value = 2097151;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(3, Writer.BaseStream.Position);

			value = 2097152;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(4, Writer.BaseStream.Position);
			value = 268435455;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(4, Writer.BaseStream.Position);
		}

		[Test]
		public void CheckInt64OptimizedRange()
		{
			Int64 value = 0;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(1, Writer.BaseStream.Position);
			value = 1;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(1, Writer.BaseStream.Position);
			value = 127;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(1, Writer.BaseStream.Position);

			value = 128;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(2, Writer.BaseStream.Position);
			value = 16383;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(2, Writer.BaseStream.Position);

			value = 16384;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(3, Writer.BaseStream.Position);
			value = 2097151;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(3, Writer.BaseStream.Position);

			value = 2097152;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(4, Writer.BaseStream.Position);
			value = 268435455;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(4, Writer.BaseStream.Position);

			value = 268435456;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(5, Writer.BaseStream.Position);
			value = 34359738367;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(5, Writer.BaseStream.Position);

			value = 34359738368;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(6, Writer.BaseStream.Position);
			value = 4398046511103;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(6, Writer.BaseStream.Position);

			value = 4398046511104;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(7, Writer.BaseStream.Position);
			value = 562949953421311;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(7, Writer.BaseStream.Position);

			value = 562949953421312;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(8, Writer.BaseStream.Position);
			value = 72057594037927935;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(8, Writer.BaseStream.Position);
		}

		[Test]
		public void CheckUInt16OptimizedRange()
		{
			var value = UInt16.MinValue;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(1, Writer.BaseStream.Position);
			value = 0;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(1, Writer.BaseStream.Position);
			value = 1;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(1, Writer.BaseStream.Position);
			value = 127;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(1, Writer.BaseStream.Position);

			value = 128;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(2, Writer.BaseStream.Position);
			value = 16383;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(2, Writer.BaseStream.Position);
		}

		[Test]
		public void CheckUInt32OptimizedRange()
		{
			var value = UInt32.MinValue;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(1, Writer.BaseStream.Position);
			value = 0;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(1, Writer.BaseStream.Position);
			value = 1;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(1, Writer.BaseStream.Position);
			value = 127;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(1, Writer.BaseStream.Position);

			value = 128;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(2, Writer.BaseStream.Position);
			value = 16383;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(2, Writer.BaseStream.Position);

			value = 16384;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(3, Writer.BaseStream.Position);
			value = 2097151;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(3, Writer.BaseStream.Position);

			value = 2097152;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(4, Writer.BaseStream.Position);
			value = 268435455;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(4, Writer.BaseStream.Position);
		}

		[Test]
		public void CheckUInt64OptimizedRange()
		{
			var value = UInt64.MinValue;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(1, Writer.BaseStream.Position);
			value = 0;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(1, Writer.BaseStream.Position);
			value = 1;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(1, Writer.BaseStream.Position);
			value = 127;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(1, Writer.BaseStream.Position);

			value = 128;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(2, Writer.BaseStream.Position);
			value = 16383;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(2, Writer.BaseStream.Position);

			value = 16384;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(3, Writer.BaseStream.Position);
			value = 2097151;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(3, Writer.BaseStream.Position);

			value = 2097152;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(4, Writer.BaseStream.Position);
			value = 268435455;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(4, Writer.BaseStream.Position);

			value = 268435456;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(5, Writer.BaseStream.Position);
			value = 34359738367;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(5, Writer.BaseStream.Position);

			value = 34359738368;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(6, Writer.BaseStream.Position);
			value = 4398046511103;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(6, Writer.BaseStream.Position);

			value = 4398046511104;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(7, Writer.BaseStream.Position);
			value = 562949953421311;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(7, Writer.BaseStream.Position);

			value = 562949953421312;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(8, Writer.BaseStream.Position);
			value = 72057594037927935;
			Writer.BaseStream.Position = 0;
			Writer.WriteOptimized(value);
			Assert.AreEqual(8, Writer.BaseStream.Position);
		}

		[Test]
		public void CheckOptimizedDateTimeWithDateOnly()
		{
			var value = new DateTime(2006, 9, 17);
			Writer.WriteOptimized(value);
			CheckValue(3, 4, value, Reader.ReadOptimizedDateTime());
		}

		[Test]
		public void CheckOptimizedDateTimeWithDateHoursAndMinutes()
		{
			var value = new DateTime(2006, 9, 17, 12, 20, 0);
			Writer.WriteOptimized(value);
			CheckValue(5, 6, value, Reader.ReadOptimizedDateTime());
		}

		[Test]
		public void CheckOptimizedDateTimeWithDateHoursAndMinutesAndSeconds()
		{
			var value = new DateTime(2006, 9, 17, 12, 20, 22);
			Writer.WriteOptimized(value);
			CheckValue(6, 7, value, Reader.ReadOptimizedDateTime());
		}

		[Test]
		public void CheckCustomClassArrayNull()
		{
			CustomClass[] value = null;
			Writer.Write(value);
			var result = (CustomClass[]) Reader.ReadObjectArray(typeof(CustomClass));
			CheckValue(1, 1, value, result, new ListComparer());
		}

		[Test]
		public void CheckCustomClassArrayEmpty()
		{
			var value = new CustomClass[0];
			Writer.Write(value);
			var result = (CustomClass[]) Reader.ReadObjectArray(typeof(CustomClass));
			CheckValue(1, 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckCustomClassArrayOne()
		{
			var value = new[] { new CustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			Writer.Write(value);
			var result = (CustomClass[]) Reader.ReadObjectArray(typeof(CustomClass));
			CheckValue(CustomClass.FullTypeSize + 76, CustomClass.FullTypeSize + 76 + 2, value, result, new ListComparer());
		}

		[Test]
		public void CheckCustomClassArrayMulti()
		{
			var value = new[] { new CustomClass(), new CustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.Write(value);
			var result = (CustomClass[]) Reader.ReadObjectArray(typeof(CustomClass));
			CheckValue(CustomClass.FullTypeSize * value.Length + 150, CustomClass.FullTypeSize * value.Length + 150 + 2, value, result, new ListComparer());
		}

		[Test]
		public void CheckCustomClassArrayMultiNulls()
		{
			var value = new CustomClass[] { null, null, null };
			Writer.Write(value);
			var result = (CustomClass[]) Reader.ReadObjectArray(typeof(CustomClass));
			CheckValue(4, 6, value, result, new ListComparer());
		}

		[Test]
		public void CheckCustomClassArrayMultiNoNulls()
		{
			var value = new[] { null, new CustomClass(), null };
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.Write(value);
			var result = (CustomClass[]) Reader.ReadObjectArray(typeof(CustomClass));
			CheckValue(CustomClass.FullTypeSize + 78, CustomClass.FullTypeSize + 78 + 2, value, result, new ListComparer());
		}

		[Test]
		public void CheckCustomClassArrayMixed()
		{
			var value = new[] { new CustomClass(), null, new InheritedCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			value[2].IntValue = 6;
			value[2].StringValue = "def";
			Writer.Write(value);
			var result = (CustomClass[]) Reader.ReadObjectArray(typeof(CustomClass));
			CheckValue(CustomClass.FullTypeSize + InheritedCustomClass.FullTypeSize + 168, CustomClass.FullTypeSize + InheritedCustomClass.FullTypeSize + 168 + 2, value, result, new ListComparer());
		}

		[Test]
		public void CheckCustomClassArrayOptimizedEmpty()
		{
			var value = new CustomClass[0];
			Writer.WriteOptimized(value);
			var result = (CustomClass[]) Reader.ReadOptimizedObjectArray(typeof(CustomClass));
			CheckValue(1, 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckCustomClassArrayOptimizedOne()
		{
			var value = new[] { new CustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			Writer.WriteOptimized(value);
			var result = (CustomClass[]) Reader.ReadOptimizedObjectArray(typeof(CustomClass));
			CheckValue(CustomClass.FullTypeSize + 75, CustomClass.FullTypeSize + 75 + 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckCustomClassArrayOptimizedMulti()
		{
			var value = new[] { new CustomClass(), new CustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.WriteOptimized(value);
			var result = (CustomClass[]) Reader.ReadOptimizedObjectArray(typeof(CustomClass));
			CheckValue(CustomClass.FullTypeSize * value.Length + 149, CustomClass.FullTypeSize * value.Length + 149 + 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckCustomClassArrayOptimizedMultiNulls()
		{
			var value = new CustomClass[] { null, null, null };
			Writer.WriteOptimized(value);
			var result = (CustomClass[]) Reader.ReadOptimizedObjectArray(typeof(CustomClass));
			CheckValue(3, 6, value, result, new ListComparer());
		}

		[Test]
		public void CheckCustomClassArrayOptimizedMultiNoNulls()
		{
			var value = new[] { null, new CustomClass(), null };
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.WriteOptimized(value);
			var result = (CustomClass[]) Reader.ReadOptimizedObjectArray(typeof(CustomClass));
			CheckValue(CustomClass.FullTypeSize + 77, CustomClass.FullTypeSize + 77 + 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckCustomClassArrayOptimizedMixed()
		{
			var value = new[] { new CustomClass(), null, new InheritedCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			value[2].IntValue = 6;
			value[2].StringValue = "def";
			Writer.WriteOptimized(value);
			var result = (CustomClass[])Reader.ReadOptimizedObjectArray(typeof(CustomClass));
			CheckValue(CustomClass.FullTypeSize + InheritedCustomClass.FullTypeSize + 167, CustomClass.FullTypeSize + InheritedCustomClass.FullTypeSize + 167 + 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckCustomClassTypedArrayNull()
		{
			CustomClass[] value = null;
			Writer.WriteTypedArray(value);
			var result = (CustomClass[]) Reader.ReadTypedArray();
			CheckValue(1, 1, value, result, new ListComparer());
		}

		[Test]
		public void CheckCustomClassTypedArrayEmpty()
		{
			var value = new CustomClass[0];
			Writer.WriteTypedArray(value);
			var result = (CustomClass[]) Reader.ReadTypedArray();
			CheckValue(3, 3, value, result, new ListComparer());
		}

		[Test, Category("Improve this")]
		public void CheckCustomClassTypedArrayOne()
		{
			var value = new[] { new CustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			Writer.WriteTypedArray(value);
			var result = (CustomClass[]) Reader.ReadTypedArray();
			CheckValue(CustomClass.FullTypeSize + 78, CustomClass.FullTypeSize + 78, value, result, new ListComparer());
		}

		[Test, Category("Improve this")]
		public void CheckCustomClassTypedArrayMulti()
		{
			var value = new[] { new CustomClass(), new CustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.WriteTypedArray(value);
			var result = (CustomClass[]) Reader.ReadTypedArray();
			CheckValue(CustomClass.FullTypeSize * value.Length + 152, CustomClass.FullTypeSize * value.Length + 152, value, result, new ListComparer());
		}

		[Test, Category("Improve this")]
		public void CheckCustomClassTypedArrayMultiNulls()
		{
			var value = new CustomClass[] { null, null, null };
			Writer.WriteTypedArray(value);
			var result = (CustomClass[]) Reader.ReadTypedArray();
			CheckValue(6, 6, value, result, new ListComparer());
		}

		[Test]
		public void CheckCustomClassTypedArrayMultiNoNulls()
		{
			var value = new[] { null, new CustomClass(), null };
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.WriteTypedArray(value);
			var result = (CustomClass[]) Reader.ReadTypedArray();
			CheckValue(CustomClass.FullTypeSize + 80, CustomClass.FullTypeSize + 80, value, result, new ListComparer());
		}

		[Test]
		public void CheckInheritedCustomClassArrayNull()
		{
			InheritedCustomClass[] value = null;
			Writer.Write(value);
			var result = (InheritedCustomClass[]) Reader.ReadObjectArray(typeof(InheritedCustomClass));
			CheckValue(1, 1, value, result, new ListComparer());
		}

		[Test]
		public void CheckInheritedCustomClassArrayEmpty()
		{
			var value = new InheritedCustomClass[0];
			Writer.Write(value);
			var result = (InheritedCustomClass[]) Reader.ReadObjectArray(typeof(InheritedCustomClass));
			CheckValue(1, 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckInheritedCustomClassArrayOne()
		{
			var value = new[] { new InheritedCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			Writer.Write(value);
			var result = (InheritedCustomClass[]) Reader.ReadObjectArray(typeof(InheritedCustomClass));
			CheckValue(InheritedCustomClass.FullTypeSize + 93, InheritedCustomClass.FullTypeSize + 93 + 2, value, result, new ListComparer());
		}

		[Test]
		public void CheckInheritedCustomClassArrayMulti()
		{
			var value = new[] { new InheritedCustomClass(), new InheritedCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.Write(value);
			var result = (InheritedCustomClass[]) Reader.ReadObjectArray(typeof(InheritedCustomClass));
			CheckValue(InheritedCustomClass.FullTypeSize * 2 + 184, InheritedCustomClass.FullTypeSize * 2 + 184 + 2, value, result, new ListComparer());
		}

		[Test]
		public void CheckInheritedCustomClassArrayMultiNulls()
		{
			var value = new InheritedCustomClass[] { null, null, null };
			Writer.Write(value);
			var result = (InheritedCustomClass[]) Reader.ReadObjectArray(typeof(InheritedCustomClass));
			CheckValue(4, 6, value, result, new ListComparer());
		}

		[Test]
		public void CheckInheritedCustomClassArrayMultiNoNulls()
		{
			var value = new[] { null, new InheritedCustomClass(), null };
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.Write(value);
			var result = (InheritedCustomClass[]) Reader.ReadObjectArray(typeof(InheritedCustomClass));
			CheckValue(InheritedCustomClass.FullTypeSize + 95, InheritedCustomClass.FullTypeSize + 95 + 2, value, result, new ListComparer());
		}

		[Test]
		public void CheckInheritedCustomClassTypedArrayNull()
		{
			InheritedCustomClass[] value = null;
			Writer.WriteTypedArray(value);
			var result = (InheritedCustomClass[]) Reader.ReadTypedArray();
			CheckValue(1, 1, value, result, new ListComparer());
		}

		[Test, Category("Improve this")]
		public void CheckInheritedCustomClassTypedArrayEmpty()
		{
			var value = new InheritedCustomClass[0];
			Writer.WriteTypedArray(value);
			var result = (InheritedCustomClass[]) Reader.ReadTypedArray();
			CheckValue(3, 3, value, result, new ListComparer());
		}

		[Test, Category("Improve this")]
		public void CheckInheritedCustomClassTypedArrayOne()
		{
			var value = new[] { new InheritedCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			Writer.WriteTypedArray(value);
			var result = (InheritedCustomClass[]) Reader.ReadTypedArray();
			CheckValue(InheritedCustomClass.FullTypeSize + 95, InheritedCustomClass.FullTypeSize + 95, value, result, new ListComparer());
		}

		[Test, Category("Improve this")]
		public void CheckInheritedCustomClassTypedArrayMulti()
		{
			var value = new[] { new InheritedCustomClass(), new InheritedCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.WriteTypedArray(value);
			var result = (InheritedCustomClass[]) Reader.ReadTypedArray();
			CheckValue(InheritedCustomClass.FullTypeSize * 2 + 186, InheritedCustomClass.FullTypeSize * 2 + 186, value, result, new ListComparer());
		}

		[Test, Category("Improve this")]
		public void CheckInheritedCustomClassTypedArrayMultiNulls()
		{
			var value = new InheritedCustomClass[] { null, null, null };
			Writer.WriteTypedArray(value);
			var result = (InheritedCustomClass[]) Reader.ReadTypedArray();
			CheckValue(6, 6, value, result, new ListComparer());
		}

		[Test, Category("Improve this")]
		public void CheckInheritedCustomClassTypedArrayMultiNoNulls()
		{
			var value = new[] { null, new InheritedCustomClass(), null };
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.WriteTypedArray(value);
			var result = (InheritedCustomClass[]) Reader.ReadTypedArray();
			CheckValue(InheritedCustomClass.FullTypeSize + 97, InheritedCustomClass.FullTypeSize + 97, value, result, new ListComparer());
		}

		[Test]
		public void CheckInheritedCustomClassArrayOptimizedEmpty()
		{
			var value = new InheritedCustomClass[0];
			Writer.WriteOptimized(value);
			var result = (InheritedCustomClass[]) Reader.ReadOptimizedObjectArray(typeof(InheritedCustomClass));
			CheckValue(1, 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckInheritedCustomClassArrayOptimizedOne()
		{
			var value = new[] { new InheritedCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			Writer.WriteOptimized(value);
			var result = (InheritedCustomClass[]) Reader.ReadOptimizedObjectArray(typeof(InheritedCustomClass));
			CheckValue(InheritedCustomClass.FullTypeSize + 92, InheritedCustomClass.FullTypeSize + 92 + 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckInheritedCustomClassArrayOptimizedMulti()
		{
			var value = new[] { new InheritedCustomClass(), new InheritedCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.WriteOptimized(value);
			var result = (InheritedCustomClass[]) Reader.ReadOptimizedObjectArray(typeof(InheritedCustomClass));
			CheckValue(InheritedCustomClass.FullTypeSize * 2 + 183, InheritedCustomClass.FullTypeSize * 2 + 183 + 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckInheritedCustomClassArrayOptimizedMultiNulls()
		{
			var value = new InheritedCustomClass[] { null, null, null };
			Writer.WriteOptimized(value);
			var result = (InheritedCustomClass[]) Reader.ReadOptimizedObjectArray(typeof(InheritedCustomClass));
			CheckValue(3, 6, value, result, new ListComparer());
		}

		[Test]
		public void CheckInheritedCustomClassArrayOptimizedMultiNoNulls()
		{
			var value = new[] { null, new InheritedCustomClass(), null };
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.WriteOptimized(value);
			var result = (InheritedCustomClass[]) Reader.ReadOptimizedObjectArray(typeof(InheritedCustomClass));
			CheckValue(InheritedCustomClass.FullTypeSize + 94, InheritedCustomClass.FullTypeSize + 94 + 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckSemiIntelligentCustomClassArrayNull()
		{
			SemiIntelligentCustomClass[] value = null;
			Writer.Write(value);
			var result = (SemiIntelligentCustomClass[]) Reader.ReadObjectArray(typeof(SemiIntelligentCustomClass));
			CheckValue(1, 1, value, result, new ListComparer());
		}

		[Test]
		public void CheckSemiIntelligentCustomClassArrayEmpty()
		{
			var value = new SemiIntelligentCustomClass[0];
			Writer.Write(value);
			var result = (SemiIntelligentCustomClass[]) Reader.ReadObjectArray(typeof(SemiIntelligentCustomClass));
			CheckValue(1, 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckSemiIntelligentCustomClassArrayOne()
		{
			var value = new[] { new SemiIntelligentCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			Writer.Write(value);
			var result = (SemiIntelligentCustomClass[]) Reader.ReadObjectArray(typeof(SemiIntelligentCustomClass));
			CheckValue(SemiIntelligentCustomClass.FullTypeSize + 76, SemiIntelligentCustomClass.FullTypeSize + 76 + 2, value, result, new ListComparer());
		}

		[Test]
		public void CheckSemiIntelligentCustomClassArrayMulti()
		{
			var value = new[] { new SemiIntelligentCustomClass(), new SemiIntelligentCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.Write(value);
			var result = (SemiIntelligentCustomClass[]) Reader.ReadObjectArray(typeof(SemiIntelligentCustomClass));
			CheckValue(SemiIntelligentCustomClass.FullTypeSize * 2 + 150, SemiIntelligentCustomClass.FullTypeSize * 2 + 150 + 2, value, result, new ListComparer());
		}

		[Test]
		public void CheckSemiIntelligentCustomClassArrayMultiNulls()
		{
			var value = new SemiIntelligentCustomClass[] { null, null, null };
			Writer.Write(value);
			var result = (SemiIntelligentCustomClass[]) Reader.ReadObjectArray(typeof(SemiIntelligentCustomClass));
			CheckValue(4, 6, value, result, new ListComparer());
		}

		[Test]
		public void CheckSemiIntelligentCustomClassArrayMultiNoNulls()
		{
			var value = new[] { null, new SemiIntelligentCustomClass(), null };
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.Write(value);
			var result = (SemiIntelligentCustomClass[]) Reader.ReadObjectArray(typeof(SemiIntelligentCustomClass));
			CheckValue(SemiIntelligentCustomClass.FullTypeSize + 78, SemiIntelligentCustomClass.FullTypeSize + 78 + 2, value, result, new ListComparer());
		}

		[Test]
		public void CheckSemiIntelligentCustomClassArrayMixed()
		{
			var value = new[] { new SemiIntelligentCustomClass(), null, new InheritedSemiIntelligentCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			value[2].IntValue = 6;
			value[2].StringValue = "def";
			Writer.Write(value);
			var result = (SemiIntelligentCustomClass[])Reader.ReadObjectArray(typeof(SemiIntelligentCustomClass));
			CheckValue(SemiIntelligentCustomClass.FullTypeSize * 2 + 177, SemiIntelligentCustomClass.FullTypeSize * 2 + 177 + 2, value, result, new ListComparer());
		}

		[Test]
		public void CheckSemiIntelligentCustomClassArrayOptimizedEmpty()
		{
			var value = new SemiIntelligentCustomClass[0];
			Writer.WriteOptimized(value);
			var result = (SemiIntelligentCustomClass[]) Reader.ReadOptimizedObjectArray(typeof(SemiIntelligentCustomClass));
			CheckValue(1, 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckSemiIntelligentCustomClassArrayOptimizedOne()
		{
			var value = new[] { new SemiIntelligentCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			Writer.WriteOptimized(value);
			var result = (SemiIntelligentCustomClass[]) Reader.ReadOptimizedObjectArray(typeof(SemiIntelligentCustomClass));
			CheckValue(SemiIntelligentCustomClass.FullTypeSize + 75, SemiIntelligentCustomClass.FullTypeSize + 75 + 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckSemiIntelligentCustomClassArrayOptimizedMulti()
		{
			var value = new[] { new SemiIntelligentCustomClass(), new SemiIntelligentCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.WriteOptimized(value);
			var result = (SemiIntelligentCustomClass[]) Reader.ReadOptimizedObjectArray(typeof(SemiIntelligentCustomClass));
			CheckValue(SemiIntelligentCustomClass.FullTypeSize * 2 + 149, SemiIntelligentCustomClass.FullTypeSize * 2 + 149 + 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckSemiIntelligentCustomClassArrayOptimizedMultiNulls()
		{
			var value = new SemiIntelligentCustomClass[] { null, null, null };
			Writer.WriteOptimized(value);
			var result = (SemiIntelligentCustomClass[]) Reader.ReadOptimizedObjectArray(typeof(SemiIntelligentCustomClass));
			CheckValue(3, 6, value, result, new ListComparer());
		}

		[Test]
		public void CheckSemiIntelligentCustomClassArrayOptimizedMultiNoNulls()
		{
			var value = new[] { null, new SemiIntelligentCustomClass(), null };
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.WriteOptimized(value);
			var result = (SemiIntelligentCustomClass[]) Reader.ReadOptimizedObjectArray(typeof(SemiIntelligentCustomClass));
			CheckValue(SemiIntelligentCustomClass.FullTypeSize + 77, SemiIntelligentCustomClass.FullTypeSize + 77 + 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckSemiIntelligentCustomClassArrayOptimizedMixed()
		{
			var value = new[] { new SemiIntelligentCustomClass(), null, new InheritedSemiIntelligentCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			value[2].IntValue = 6;
			value[2].StringValue = "def";
			Writer.WriteOptimized(value);
			var result = (SemiIntelligentCustomClass[])Reader.ReadOptimizedObjectArray(typeof(SemiIntelligentCustomClass));
			CheckValue(SemiIntelligentCustomClass.FullTypeSize * 2 + 176, SemiIntelligentCustomClass.FullTypeSize * 2 + 176 + 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckSemiIntelligentCustomClassTypedArrayNull()
		{
			SemiIntelligentCustomClass[] value = null;
			Writer.WriteTypedArray(value);
			var result = (SemiIntelligentCustomClass[]) Reader.ReadTypedArray();
			CheckValue(1, 1, value, result, new ListComparer());
		}

		[Test]
		public void CheckSemiIntelligentCustomClassTypedArrayEmpty()
		{
			var value = new SemiIntelligentCustomClass[0];
			Writer.WriteTypedArray(value);
			var result = (SemiIntelligentCustomClass[]) Reader.ReadTypedArray();
			CheckValue(3, 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckSemiIntelligentCustomClassTypedArrayOne()
		{
			var value = new[] { new SemiIntelligentCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			Writer.WriteTypedArray(value);
			var result = (SemiIntelligentCustomClass[]) Reader.ReadTypedArray();
			CheckValue(SemiIntelligentCustomClass.FullTypeSize + 78, SemiIntelligentCustomClass.FullTypeSize + 78, value, result, new ListComparer());
		}

		[Test]
		public void CheckSemiIntelligentCustomClassTypedArrayMulti()
		{
			var value = new[] { new SemiIntelligentCustomClass(), new SemiIntelligentCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.WriteTypedArray(value);
			var result = (SemiIntelligentCustomClass[]) Reader.ReadTypedArray();
			CheckValue(SemiIntelligentCustomClass.FullTypeSize * 2 + 152, SemiIntelligentCustomClass.FullTypeSize * 2 + 152, value, result, new ListComparer());
		}

		[Test]
		public void CheckSemiIntelligentCustomClassTypedArrayMultiNulls()
		{
			var value = new SemiIntelligentCustomClass[] { null, null, null };
			Writer.WriteTypedArray(value);
			var result = (SemiIntelligentCustomClass[]) Reader.ReadTypedArray();
			CheckValue(6, 6, value, result, new ListComparer());
		}

		[Test]
		public void CheckSemiIntelligentCustomClassTypedArrayMultiNoNulls()
		{
			var value = new[] { null, new SemiIntelligentCustomClass(), null };
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.WriteTypedArray(value);
			var result = (SemiIntelligentCustomClass[]) Reader.ReadTypedArray();
			CheckValue(SemiIntelligentCustomClass.FullTypeSize + 80, SemiIntelligentCustomClass.FullTypeSize + 80, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomClassArrayNull()
		{
			IntelligentCustomClass[] value = null;
			Writer.Write(value);
			var result = (IntelligentCustomClass[]) Reader.ReadObjectArray(typeof(IntelligentCustomClass));
			CheckValue(1, 1, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomClassArrayEmpty()
		{
			var value = new IntelligentCustomClass[0];
			Writer.Write(value);
			var result = (IntelligentCustomClass[]) Reader.ReadObjectArray(typeof(IntelligentCustomClass));
			CheckValue(1, 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomClassArrayOne()
		{
			var value = new[] { new IntelligentCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			Writer.Write(value);
			var result = (IntelligentCustomClass[]) Reader.ReadObjectArray(typeof(IntelligentCustomClass));
			CheckValue(8, 7, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomClassArrayMulti()
		{
			var value = new[] { new IntelligentCustomClass(), new IntelligentCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.Write(value);
			var result = (IntelligentCustomClass[]) Reader.ReadObjectArray(typeof(IntelligentCustomClass));
			CheckValue(14, 10, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomClassArrayMultiNulls()
		{
			var value = new IntelligentCustomClass[] { null, null, null };
			Writer.Write(value);
			var result = (IntelligentCustomClass[]) Reader.ReadObjectArray(typeof(IntelligentCustomClass));
			CheckValue(4, 6, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomClassArrayMultiNoNulls()
		{
			var value = new[] { null, new IntelligentCustomClass(), null };
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.Write(value);
			var result = (IntelligentCustomClass[]) Reader.ReadObjectArray(typeof(IntelligentCustomClass));
			CheckValue(10, 9, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomClassArrayMixed()
		{
			var value = new[] { new IntelligentCustomClass(), null, new InheritedIntelligentCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			value[2].IntValue = 6;
			value[2].StringValue = "def";
			Writer.Write(value);
			var result = (IntelligentCustomClass[])Reader.ReadObjectArray(typeof(IntelligentCustomClass));
			CheckValue(19, 21, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomClassArrayOptimizedEmpty()
		{
			var value = new IntelligentCustomClass[0];
			Writer.WriteOptimized(value);
			var result = (IntelligentCustomClass[]) Reader.ReadOptimizedObjectArray(typeof(IntelligentCustomClass));
			CheckValue(1, 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomClassArrayOptimizedOne()
		{
			var value = new[] { new IntelligentCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			Writer.WriteOptimized(value);
			var result = (IntelligentCustomClass[]) Reader.ReadOptimizedObjectArray(typeof(IntelligentCustomClass));
			CheckValue(7, 7, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomClassArrayOptimizedMulti()
		{
			var value = new[] { new IntelligentCustomClass(), new IntelligentCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.WriteOptimized(value);
			var result = (IntelligentCustomClass[]) Reader.ReadOptimizedObjectArray(typeof(IntelligentCustomClass));
			CheckValue(13, 10, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomClassArrayOptimizedMultiNulls()
		{
			var value = new IntelligentCustomClass[] { null, null, null };
			Writer.WriteOptimized(value);
			var result = (IntelligentCustomClass[]) Reader.ReadOptimizedObjectArray(typeof(IntelligentCustomClass));
			CheckValue(3, 6, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomClassArrayOptimizedMultiNoNulls()
		{
			var value = new[] { null, new IntelligentCustomClass(), null };
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.WriteOptimized(value);
			var result = (IntelligentCustomClass[]) Reader.ReadOptimizedObjectArray(typeof(IntelligentCustomClass));
			CheckValue(9, 9, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomClassArrayOptimizedMixed()
		{
			var value = new[] { new IntelligentCustomClass(), null, new InheritedIntelligentCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			value[2].IntValue = 6;
			value[2].StringValue = "def";
			Writer.WriteOptimized(value);
			var result = (IntelligentCustomClass[])Reader.ReadOptimizedObjectArray(typeof(IntelligentCustomClass));
			CheckValue(18, 21, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomClassTypedArrayNull()
		{
			IntelligentCustomClass[] value = null;
			Writer.WriteTypedArray(value);
			var result = (IntelligentCustomClass[]) Reader.ReadTypedArray();
			CheckValue(1, 1, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomClassTypedArrayEmpty()
		{
			var value = new IntelligentCustomClass[0];
			Writer.WriteTypedArray(value);
			var result = (IntelligentCustomClass[]) Reader.ReadTypedArray();
			CheckValue(3, 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomClassTypedArrayOne()
		{
			var value = new[] { new IntelligentCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			Writer.WriteTypedArray(value);
			var result = (IntelligentCustomClass[]) Reader.ReadTypedArray();
			CheckValue(7, 7, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomClassTypedArrayMulti()
		{
			var value = new[] { new IntelligentCustomClass(), new IntelligentCustomClass() };
			value[0].IntValue = 5;
			value[0].StringValue = "abc";
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.WriteTypedArray(value);
			var result = (IntelligentCustomClass[]) Reader.ReadTypedArray();
			CheckValue(10, 10, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomClassTypedArrayMultiNulls()
		{
			var value = new IntelligentCustomClass[] { null, null, null };
			Writer.WriteTypedArray(value);
			var result = (IntelligentCustomClass[]) Reader.ReadTypedArray();
			CheckValue(6, 6, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomClassTypedArrayMultiNoNulls()
		{
			var value = new[] { null, new IntelligentCustomClass(), null };
			value[1].IntValue = 6;
			value[1].StringValue = "def";
			Writer.WriteTypedArray(value);
			var result = (IntelligentCustomClass[]) Reader.ReadTypedArray();
			CheckValue(9, 9, value, result, new ListComparer());
		}

		[Test]
		public void CheckCustomStruct()
		{
			var value = new CustomStruct(10, "abc");
			Writer.WriteObject(value);
			CheckValue(CustomStruct.FullTypeSize + 74, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckIntelligentCustomStruct()
		{
			var value = new IntelligentCustomStruct(10, "abc");
			Writer.WriteObject(value);
			CheckValue(6, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckStringArrayNull()
		{
			string[] value = null;
			Writer.Write(value);

			CheckValue(1, 1, value, Reader.ReadStringArray());
		}

		[Test]
		public void CheckStringArrayEmpty()
		{
			var value = new string[0];
			Writer.Write(value);

			CheckValue(1, 2, value, Reader.ReadStringArray());
		}

		[Test]
		public void CheckStringArrayOne()
		{
			var value = new[] {"Simon"};
			Writer.Write(value);

			CheckValue(4, 4, value, Reader.ReadStringArray());
		}

		[Test]
		public void CheckStringArrayMulti()
		{
			var value = new[] {"abc", "defgh", "ijkl"};
			Writer.Write(value);

			CheckValue(8, 8, value, Reader.ReadStringArray());
		}

		[Test]
		public void CheckStringArrayMultiNulls()
		{
			var value = new string[] {null, null, null};
			Writer.Write(value);

			CheckValue(4, 4, value, Reader.ReadStringArray());
		}

		[Test]
		public void CheckStringArrayMultiNoNulls()
		{
			var value = new[] {null, "hewitt", null};
			Writer.Write(value);

			CheckValue(6, 6, value, Reader.ReadStringArray());
		}

		[Test]
		public void CheckStringArrayOptimizedEmpty()
		{
			var value = new string[0];
			Writer.WriteOptimized(value);

			CheckValue(1, 2, value, Reader.ReadOptimizedStringArray());
		}

		[Test]
		public void CheckStringArrayOptimizedOne()
		{
			var value = new[] {"Simon"};
			Writer.WriteOptimized(value);

			CheckValue(3, 4, value, Reader.ReadOptimizedStringArray());
		}

		[Test]
		public void CheckStringArrayOptimizedMulti()
		{
			var value = new[] {"abc", "defgh", "ijkl"};
			Writer.WriteOptimized(value);

			CheckValue(7, 8, value, Reader.ReadOptimizedStringArray());
		}

		[Test]
		public void CheckStringArrayOptimizedMultiNulls()
		{
			var value = new string[] {null, null, null};
			Writer.WriteOptimized(value);

			CheckValue(3, 4, value, Reader.ReadOptimizedStringArray());
		}

		[Test]
		public void CheckStringArrayOptimizedNoMultiNulls()
		{
			var value = new[] {null, "hewitt", null};
			Writer.WriteOptimized(value);

			CheckValue(5, 6, value, Reader.ReadOptimizedStringArray());
		}

		[Test]
		public void CheckInt16ArrayNull()
		{
			Int16[] value = null;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadInt16Array());
		}

		[Test]
		public void CheckInt16ArrayEmpty()
		{
			var value = new Int16[0];
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadInt16Array());
		}

		[Test]
		public void CheckInt16ArrayOne()
		{
			var value = new Int16[1];
			Writer.Write(value);
			CheckValue(2 + (1 * 2), 2 + (1 * 2), value, Reader.ReadInt16Array());
		}

		[Test]
		public void CheckInt16ArrayTwo()
		{
			var value = new Int16[2];
			Writer.Write(value);
			CheckValue(2 + (2 * 2), 3 + (2 * 1), value, Reader.ReadInt16Array());
		}

		[Test]
		public void CheckInt16ArrayMany()
		{
			var value = new Int16[100];
			Writer.Write(value);
			CheckValue(2 + (100 * 2), 3 + (100 * 1), value, Reader.ReadInt16Array());
		}

		[Test]
		public void CheckInt16ArrayOptimizedNull()
		{
			Int16[] value = null;
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedInt16Array());
		}

		[Test]
		public void CheckInt16ArrayOptimizedEmpty()
		{
			var value = new Int16[0];
			Writer.WriteOptimized(value);
			CheckValue(1, 2, value, Reader.ReadOptimizedInt16Array());
		}

		[Test]
		public void CheckInt16ArrayOptimizedOneOptimizable()
		{
			var value = new Int16[1];
			Writer.WriteOptimized(value);
			CheckValue(2 + (1 * 1), 3 + (1 * 1), value, Reader.ReadOptimizedInt16Array());
		}

		[Test]
		public void CheckInt16ArrayOptimizedOneNotOptimizable()
		{
			var value = new Int16[] {-1};
			Writer.WriteOptimized(value);
			CheckValue(2 + (1 * 2), 3 + (1 * 2), value, Reader.ReadOptimizedInt16Array());
		}

		[Test]
		public void CheckInt16ArrayOptimizedTwoOptimizable()
		{
			var value = new Int16[2];
			Writer.WriteOptimized(value);
			CheckValue(2 + (2 * 1), 3 + (2 * 1), value, Reader.ReadOptimizedInt16Array());
		}

		[Test]
		public void CheckInt16ArrayOptimizedTwoNotOptimizable()
		{
			var value = new Int16[] {-1, -1};
			Writer.WriteOptimized(value);
			CheckValue(2 + (2 * 2), 3 + (2 * 2), value, Reader.ReadOptimizedInt16Array());
		}

		[Test]
		public void CheckInt16ArrayOptimizedTwoPartOptimizable()
		{
			var value = new Int16[] {1, -1};
			Writer.WriteOptimized(value);
			var expected = 2 + (1 * 1 + 1 * 2);
			expected += 1 + 1; // For BitArray
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedInt16Array());
		}

		[Test]
		public void CheckInt16ArrayOptimizedManyNoneOptimizable()
		{
			var value = new Int16[100];
			for(var i = 0; i < value.Length; i++) value[i] = -1;
			Writer.WriteOptimized(value);
			var expected = 2 + (100 * 2);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedInt16Array());
		}

		[Test]
		public void CheckInt16ArrayOptimizedManyAllOptimizable()
		{
			var value = new Int16[100];
			for(var i = 0; i < value.Length; i++) value[i] = 1;
			Writer.WriteOptimized(value);
			var expected = 2 + (100 * 1);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedInt16Array());
		}

		[Test]
		public void CheckInt16ArrayOptimizedManyPartiallyOptimizableAtLimit()
		{
			var value = new Int16[100];
			for(var i = 0; i < value.Length; i++) value[i] = (short) ((i < 80) ? -1 : 1);
			Writer.WriteOptimized(value);
			var expected = 2 + (20 * 1) + (80 * 2);
			expected += 1 + 1 + (100 / 8); // For BitArray 
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedInt16Array());
		}

		[Test]
		public void CheckInt16ArrayOptimizedManyPartiallyOptimizableAboveLimit()
		{
			var value = new Int16[100];
			for(var i = 0; i < value.Length; i++) value[i] = (short) ((i < 81) ? -1 : 1);
			Writer.WriteOptimized(value);
			var expected = 1 + 1 + (100 * 2);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedInt16Array());
		}

		[Test]
		public void CheckUInt16ArrayNull()
		{
			UInt16[] value = null;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadUInt16Array());
		}

		[Test]
		public void CheckUInt16ArrayEmpty()
		{
			var value = new UInt16[0];
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadUInt16Array());
		}

		[Test]
		public void CheckUInt16ArrayOne()
		{
			var value = new UInt16[1];
			Writer.Write(value);
			CheckValue(2 + (1 * 2), 3 + (1 * 1), value, Reader.ReadUInt16Array());
		}

		[Test]
		public void CheckUInt16ArrayTwo()
		{
			var value = new UInt16[2];
			Writer.Write(value);
			CheckValue(2 + (2 * 2), 3 + (2 * 1), value, Reader.ReadUInt16Array());
		}

		[Test]
		public void CheckUInt16ArrayMany()
		{
			var value = new UInt16[100];
			Writer.Write(value);
			CheckValue(2 + (100 * 2), 3 + (100 * 1), value, Reader.ReadUInt16Array());
		}

		[Test]
		public void CheckUInt16ArrayOptimizedNull()
		{
			UInt16[] value = null;
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedUInt16Array());
		}

		[Test]
		public void CheckUInt16ArrayOptimizedEmpty()
		{
			var value = new UInt16[0];
			Writer.WriteOptimized(value);
			CheckValue(1, 2, value, Reader.ReadOptimizedUInt16Array());
		}

		[Test]
		public void CheckUInt16ArrayOptimizedOneOptimizable()
		{
			var value = new UInt16[1];
			Writer.WriteOptimized(value);
			CheckValue(2 + (1 * 1), 3 + (1 * 1), value, Reader.ReadOptimizedUInt16Array());
		}

		[Test]
		public void CheckUInt16ArrayOptimizedOneNotOptimizable()
		{
			var value = new[] {UInt16.MaxValue};
			Writer.WriteOptimized(value);
			CheckValue(2 + (1 * 2), 3 + (1 * 2), value, Reader.ReadOptimizedUInt16Array());
		}

		[Test]
		public void CheckUInt16ArrayOptimizedTwoOptimizable()
		{
			var value = new UInt16[2];
			Writer.WriteOptimized(value);
			CheckValue(2 + (2 * 1), 3 + (2 * 1), value, Reader.ReadOptimizedUInt16Array());
		}

		[Test]
		public void CheckUInt16ArrayOptimizedTwoNotOptimizable()
		{
			var value = new[] {UInt16.MaxValue, UInt16.MaxValue};
			Writer.WriteOptimized(value);
			CheckValue(2 + (2 * 2), 3 + (2 * 2), value, Reader.ReadOptimizedUInt16Array());
		}

		[Test]
		public void CheckUInt16ArrayOptimizedTwoPartOptimizable()
		{
			var value = new UInt16[] {1, UInt16.MaxValue};
			Writer.WriteOptimized(value);
			var expected = 2 + (1 * 1 + 1 * 2);
			expected += 1 + 1; // For BitArray
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedUInt16Array());
		}

		[Test]
		public void CheckUInt16ArrayOptimizedManyNoneOptimizable()
		{
			var value = new UInt16[100];
			for(var i = 0; i < value.Length; i++) value[i] = UInt16.MaxValue;
			Writer.WriteOptimized(value);
			var expected = 2 + (100 * 2);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedUInt16Array());
		}

		[Test]
		public void CheckUInt16ArrayOptimizedManyAllOptimizable()
		{
			var value = new UInt16[100];
			for(var i = 0; i < value.Length; i++) value[i] = 1;
			Writer.WriteOptimized(value);
			var expected = 2 + (100 * 1);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedUInt16Array());
		}

		[Test]
		public void CheckUInt16ArrayOptimizedManyPartiallyOptimizableAtLimit()
		{
			var value = new UInt16[100];
			for(var i = 0; i < value.Length; i++) value[i] = (i < 80) ? UInt16.MaxValue : (UInt16) 1;
			Writer.WriteOptimized(value);
			var expected = 2 + (20 * 1) + (80 * 2);
			expected += 1 + 1 + (100 / 8); // For BitArray 
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedUInt16Array());
		}

		[Test]
		public void CheckUInt16ArrayOptimizedManyPartiallyOptimizableAboveLimit()
		{
			var value = new UInt16[100];
			for(var i = 0; i < value.Length; i++) value[i] = (i < 81) ? UInt16.MaxValue : (UInt16) 1;
			Writer.WriteOptimized(value);
			var expected = 1 + 1 + (100 * 2);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedUInt16Array());
		}

		[Test]
		public void CheckInt32ArrayNull()
		{
			Int32[] value = null;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadInt32Array());
		}

		[Test]
		public void CheckInt32ArrayEmpty()
		{
			var value = new Int32[0];
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadInt32Array());
		}

		[Test]
		public void CheckInt32ArrayOne()
		{
			var value = new Int32[1];
			Writer.Write(value);
			CheckValue(2 + (1 * 4), 3 + (1 * 1), value, Reader.ReadInt32Array());
		}

		[Test]
		public void CheckInt32ArrayTwo()
		{
			var value = new Int32[2];
			Writer.Write(value);
			CheckValue(2 + (2 * 4), 3 + (2 * 1), value, Reader.ReadInt32Array());
		}

		[Test]
		public void CheckInt32ArrayMany()
		{
			var value = new Int32[100];
			Writer.Write(value);
			CheckValue(2 + (100 * 4), 3 + (100 * 1), value, Reader.ReadInt32Array());
		}

		[Test]
		public void CheckInt32ArrayOptimizedNull()
		{
			Int32[] value = null;
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedInt32Array());
		}

		[Test]
		public void CheckInt32ArrayOptimizedEmpty()
		{
			var value = new Int32[0];
			Writer.WriteOptimized(value);
			CheckValue(1, 2, value, Reader.ReadOptimizedInt32Array());
		}

		[Test]
		public void CheckInt32ArrayOptimizedOneOptimizable()
		{
			var value = new Int32[1];
			Writer.WriteOptimized(value);
			CheckValue(2 + (1 * 1), 3 + (1 * 1), value, Reader.ReadOptimizedInt32Array());
		}

		[Test]
		public void CheckInt32ArrayOptimizedOneNotOptimizable()
		{
			var value = new[] {-1};
			Writer.WriteOptimized(value);
			CheckValue(2 + (1 * 4), 3 + (1 * 4), value, Reader.ReadOptimizedInt32Array());
		}

		[Test]
		public void CheckInt32ArrayOptimizedTwoOptimizable()
		{
			var value = new Int32[2];
			Writer.WriteOptimized(value);
			CheckValue(2 + (2 * 1), 3 + (2 * 1), value, Reader.ReadOptimizedInt32Array());
		}

		[Test]
		public void CheckInt32ArrayOptimizedTwoNotOptimizable()
		{
			var value = new[] {-1, -1};
			Writer.WriteOptimized(value);
			CheckValue(2 + (2 * 4), 3 + (2 * 4), value, Reader.ReadOptimizedInt32Array());
		}

		[Test]
		public void CheckInt32ArrayOptimizedTwoPartOptimizable()
		{
			var value = new[] {1, -1};
			Writer.WriteOptimized(value);
			var expected = 2 + (1 * 1 + 1 * 4);
			expected += 1 + 1; // For BitArray
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedInt32Array());
		}

		[Test]
		public void CheckInt32ArrayOptimizedManyNoneOptimizable()
		{
			var value = new Int32[100];
			for(var i = 0; i < value.Length; i++) value[i] = -1;
			Writer.WriteOptimized(value);
			var expected = 2 + (100 * 4);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedInt32Array());
		}

		[Test]
		public void CheckInt32ArrayOptimizedManyAllOptimizable()
		{
			var value = new Int32[100];
			for(var i = 0; i < value.Length; i++) value[i] = 1;
			Writer.WriteOptimized(value);
			var expected = 2 + (100 * 1);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedInt32Array());
		}

		[Test]
		public void CheckInt32ArrayOptimizedManyPartiallyOptimizableAtLimit()
		{
			var value = new Int32[100];
			for(var i = 0; i < value.Length; i++) value[i] = (i < 80) ? -1 : 1;
			Writer.WriteOptimized(value);
			var expected = 2 + (20 * 1) + (80 * 4);
			expected += 1 + 1 + (100 / 8); // For BitArray 
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedInt32Array());
		}

		[Test]
		public void CheckInt32ArrayOptimizedManyPartiallyOptimizableAboveLimit()
		{
			var value = new Int32[100];
			for(var i = 0; i < value.Length; i++) value[i] = (i < 81) ? -1 : 1;
			Writer.WriteOptimized(value);
			var expected = 1 + 1 + (100 * 4);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedInt32Array());
		}

		[Test]
		public void CheckUInt32ArrayNull()
		{
			UInt32[] value = null;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadUInt32Array());
		}

		[Test]
		public void CheckUInt32ArrayEmpty()
		{
			var value = new UInt32[0];
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadUInt32Array());
		}

		[Test]
		public void CheckUInt32ArrayOne()
		{
			var value = new UInt32[1];
			Writer.Write(value);
			CheckValue(2 + (1 * 4), 3 + (1 * 1), value, Reader.ReadUInt32Array());
		}

		[Test]
		public void CheckUInt32ArrayTwo()
		{
			var value = new UInt32[2];
			Writer.Write(value);
			CheckValue(2 + (2 * 4), 3 + (2 * 1), value, Reader.ReadUInt32Array());
		}

		[Test]
		public void CheckUInt32ArrayMany()
		{
			var value = new UInt32[100];
			Writer.Write(value);
			CheckValue(2 + (100 * 4), 3 + (100 * 1), value, Reader.ReadUInt32Array());
		}

		[Test]
		public void CheckUInt32ArrayOptimizedNull()
		{
			UInt32[] value = null;
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedUInt32Array());
		}

		[Test]
		public void CheckUInt32ArrayOptimizedEmpty()
		{
			var value = new UInt32[0];
			Writer.WriteOptimized(value);
			CheckValue(1, 2, value, Reader.ReadOptimizedUInt32Array());
		}

		[Test]
		public void CheckUInt32ArrayOptimizedOneOptimizable()
		{
			var value = new UInt32[1];
			Writer.WriteOptimized(value);
			CheckValue(2 + (1 * 1), 3 + (1 * 1), value, Reader.ReadOptimizedUInt32Array());
		}

		[Test]
		public void CheckUInt32ArrayOptimizedOneNotOptimizable()
		{
			var value = new[] {UInt32.MaxValue};
			Writer.WriteOptimized(value);
			CheckValue(2 + (1 * 4), 3 + (1 * 4), value, Reader.ReadOptimizedUInt32Array());
		}

		[Test]
		public void CheckUInt32ArrayOptimizedTwoOptimizable()
		{
			var value = new UInt32[2];
			Writer.WriteOptimized(value);
			CheckValue(2 + (2 * 1), 3 + (2 * 1), value, Reader.ReadOptimizedUInt32Array());
		}

		[Test]
		public void CheckUInt32ArrayOptimizedTwoNotOptimizable()
		{
			var value = new[] {UInt32.MaxValue, UInt32.MaxValue};
			Writer.WriteOptimized(value);
			CheckValue(2 + (2 * 4), 3 + (2 * 4), value, Reader.ReadOptimizedUInt32Array());
		}

		[Test]
		public void CheckUInt32ArrayOptimizedTwoPartOptimizable()
		{
			var value = new UInt32[] {1, UInt32.MaxValue};
			Writer.WriteOptimized(value);
			var expected = 2 + (1 * 1 + 1 * 4);
			expected += 1 + 1; // For BitArray
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedUInt32Array());
		}

		[Test]
		public void CheckUInt32ArrayOptimizedManyNoneOptimizable()
		{
			var value = new UInt32[100];
			for(var i = 0; i < value.Length; i++) value[i] = UInt32.MaxValue;
			Writer.WriteOptimized(value);
			var expected = 2 + (100 * 4);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedUInt32Array());
		}

		[Test]
		public void CheckUInt32ArrayOptimizedManyAllOptimizable()
		{
			var value = new UInt32[100];
			for(var i = 0; i < value.Length; i++) value[i] = 1;
			Writer.WriteOptimized(value);
			var expected = 2 + (100 * 1);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedUInt32Array());
		}

		[Test]
		public void CheckUInt32ArrayOptimizedManyPartiallyOptimizableAtLimit()
		{
			var value = new UInt32[100];
			for(var i = 0; i < value.Length; i++) value[i] = (i < 80) ? UInt32.MaxValue : 1;
			Writer.WriteOptimized(value);
			var expected = 2 + (20 * 1) + (80 * 4);
			expected += 1 + 1 + (100 / 8); // For BitArray 
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedUInt32Array());
		}

		[Test]
		public void CheckUInt32ArrayOptimizedManyPartiallyOptimizableAboveLimit()
		{
			var value = new UInt32[100];
			for(var i = 0; i < value.Length; i++) value[i] = (i < 81) ? UInt32.MaxValue : 1;
			Writer.WriteOptimized(value);
			var expected = 1 + 1 + (100 * 4);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedUInt32Array());
		}

		[Test]
		public void CheckUInt64ArrayNull()
		{
			UInt64[] value = null;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadUInt64Array());
		}

		[Test]
		public void CheckUInt64ArrayEmpty()
		{
			var value = new UInt64[0];
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadUInt64Array());
		}

		[Test]
		public void CheckUInt64ArrayOne()
		{
			var value = new UInt64[1];
			Writer.Write(value);
			CheckValue(2 + (1 * 8), 3 + (1 * 1), value, Reader.ReadUInt64Array());
		}

		[Test]
		public void CheckUInt64ArrayTwo()
		{
			var value = new UInt64[2];
			Writer.Write(value);
			CheckValue(2 + (2 * 8), 3 + (2 * 1), value, Reader.ReadUInt64Array());
		}

		[Test]
		public void CheckUInt64ArrayMany()
		{
			var value = new UInt64[100];
			Writer.Write(value);
			CheckValue(2 + (100 * 8), 3 + (100 * 1), value, Reader.ReadUInt64Array());
		}

		[Test]
		public void CheckUInt64ArrayOptimizedNull()
		{
			UInt64[] value = null;
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedUInt64Array());
		}

		[Test]
		public void CheckUInt64ArrayOptimizedEmpty()
		{
			var value = new UInt64[0];
			Writer.WriteOptimized(value);
			CheckValue(1, 2, value, Reader.ReadOptimizedUInt64Array());
		}

		[Test]
		public void CheckUInt64ArrayOptimizedOneOptimizable()
		{
			var value = new UInt64[1];
			Writer.WriteOptimized(value);
			CheckValue(2 + (1 * 1), 3 + (1 * 1), value, Reader.ReadOptimizedUInt64Array());
		}

		[Test]
		public void CheckUInt64ArrayOptimizedOneNotOptimizable()
		{
			var value = new[] {UInt64.MaxValue};
			Writer.WriteOptimized(value);
			CheckValue(2 + (1 * 8), 3 + (1 * 8), value, Reader.ReadOptimizedUInt64Array());
		}

		[Test]
		public void CheckUInt64ArrayOptimizedTwoOptimizable()
		{
			var value = new UInt64[2];
			Writer.WriteOptimized(value);
			CheckValue(2 + (2 * 1), 3 + (2 * 1), value, Reader.ReadOptimizedUInt64Array());
		}

		[Test]
		public void CheckUInt64ArrayOptimizedTwoNotOptimizable()
		{
			var value = new[] {UInt64.MaxValue, UInt64.MaxValue};
			Writer.WriteOptimized(value);
			CheckValue(2 + (2 * 8), 3 + (2 * 8), value, Reader.ReadOptimizedUInt64Array());
		}

		[Test]
		public void CheckUInt64ArrayOptimizedTwoPartOptimizable()
		{
			var value = new UInt64[] {1, UInt64.MaxValue};
			Writer.WriteOptimized(value);
			var expected = 2 + (1 * 1 + 1 * 8);
			expected += 1 + 1; // For BitArray
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedUInt64Array());
		}

		[Test]
		public void CheckUInt64ArrayOptimizedManyNoneOptimizable()
		{
			var value = new UInt64[100];
			for(var i = 0; i < value.Length; i++) value[i] = UInt64.MaxValue;
			Writer.WriteOptimized(value);
			var expected = 2 + (100 * 8);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedUInt64Array());
		}

		[Test]
		public void CheckUInt64ArrayOptimizedManyAllOptimizable()
		{
			var value = new UInt64[100];
			for(var i = 0; i < value.Length; i++) value[i] = 1;
			Writer.WriteOptimized(value);
			var expected = 2 + (100 * 1);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedUInt64Array());
		}

		[Test]
		public void CheckUInt64ArrayOptimizedManyPartiallyOptimizableAtLimit()
		{
			var value = new UInt64[100];
			for(var i = 0; i < value.Length; i++) value[i] = (i < 80) ? UInt64.MaxValue : 1;
			Writer.WriteOptimized(value);
			var expected = 2 + (20 * 1) + (80 * 8);
			expected += 1 + 1 + (100 / 8); // For BitArray 
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedUInt64Array());
		}

		[Test]
		public void CheckUInt64ArrayOptimizedManyPartiallyOptimizableAboveLimit()
		{
			var value = new UInt64[100];
			for(var i = 0; i < value.Length; i++) value[i] = (i < 81) ? UInt64.MaxValue : 1;
			Writer.WriteOptimized(value);
			var expected = 1 + 1 + (100 * 8);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedUInt64Array());
		}

		[Test]
		public void CheckInt64ArrayNull()
		{
			Int64[] value = null;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadInt64Array());
		}

		[Test]
		public void CheckInt64ArrayEmpty()
		{
			var value = new Int64[0];
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadInt64Array());
		}

		[Test]
		public void CheckInt64ArrayOne()
		{
			var value = new Int64[1];
			Writer.Write(value);
			CheckValue(2 + (1 * 8), 3 + (1 * 1), value, Reader.ReadInt64Array());
		}

		[Test]
		public void CheckInt64ArrayTwo()
		{
			var value = new Int64[2];
			Writer.Write(value);
			CheckValue(2 + (2 * 8), 3 + (2 * 1), value, Reader.ReadInt64Array());
		}

		[Test]
		public void CheckInt64ArrayMany()
		{
			var value = new Int64[100];
			Writer.Write(value);
			CheckValue(2 + (100 * 8), 3 + (100 * 1), value, Reader.ReadInt64Array());
		}

		[Test]
		public void CheckInt64ArrayOptimizedNull()
		{
			Int64[] value = null;
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedInt64Array());
		}

		[Test]
		public void CheckInt64ArrayOptimizedEmpty()
		{
			var value = new Int64[0];
			Writer.WriteOptimized(value);
			CheckValue(1, 2, value, Reader.ReadOptimizedInt64Array());
		}

		[Test]
		public void CheckInt64ArrayOptimizedOneOptimizable()
		{
			var value = new Int64[1];
			Writer.WriteOptimized(value);
			CheckValue(2 + (1 * 1), 3 + (1 * 1), value, Reader.ReadOptimizedInt64Array());
		}

		[Test]
		public void CheckInt64ArrayOptimizedOneNotOptimizable()
		{
			var value = new Int64[] {-1};
			Writer.WriteOptimized(value);
			CheckValue(2 + (1 * 8), 3 + (1 * 8), value, Reader.ReadOptimizedInt64Array());
		}

		[Test]
		public void CheckInt64ArrayOptimizedTwoOptimizable()
		{
			var value = new Int64[2];
			Writer.WriteOptimized(value);
			CheckValue(2 + (2 * 1), 3 + (2 * 1), value, Reader.ReadOptimizedInt64Array());
		}

		[Test]
		public void CheckInt64ArrayOptimizedTwoNotOptimizable()
		{
			var value = new Int64[] {-1, -1};
			Writer.WriteOptimized(value);
			CheckValue(2 + (2 * 8), 3 + (2 * 8), value, Reader.ReadOptimizedInt64Array());
		}

		[Test]
		public void CheckInt64ArrayOptimizedTwoPartOptimizable()
		{
			var value = new Int64[] {1, -1};
			Writer.WriteOptimized(value);
			var expected = 2 + (1 * 1 + 1 * 8);
			expected += 1 + 1; // For BitArray
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedInt64Array());
		}

		[Test]
		public void CheckInt64ArrayOptimizedManyNoneOptimizable()
		{
			var value = new Int64[100];
			for(var i = 0; i < value.Length; i++) value[i] = -1;
			Writer.WriteOptimized(value);
			var expected = 2 + (100 * 8);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedInt64Array());
		}

		[Test]
		public void CheckInt64ArrayOptimizedManyAllOptimizable()
		{
			var value = new Int64[100];
			for(var i = 0; i < value.Length; i++) value[i] = 1;
			Writer.WriteOptimized(value);
			var expected = 2 + (100 * 1);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedInt64Array());
		}

		[Test]
		public void CheckInt64ArrayOptimizedManyPartiallyOptimizableAtLimit()
		{
			var value = new Int64[100];
			for(var i = 0; i < value.Length; i++) value[i] = (i < 80) ? -1 : 1;
			Writer.WriteOptimized(value);
			var expected = 2 + (20 * 1) + (80 * 8);
			expected += 1 + 1 + (100 / 8); // For BitArray 
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedInt64Array());
		}

		[Test]
		public void CheckInt64ArrayOptimizedManyPartiallyOptimizableAboveLimit()
		{
			var value = new Int64[100];
			for(var i = 0; i < value.Length; i++) value[i] = (i < 81) ? -1 : 1;
			Writer.WriteOptimized(value);
			var expected = 1 + 1 + (100 * 8);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedInt64Array());
		}

		[Test]
		public void CheckTimeSpanArrayNull()
		{
			TimeSpan[] value = null;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadTimeSpanArray());
		}

		[Test]
		public void CheckTimeSpanArrayEmpty()
		{
			var value = new TimeSpan[0];
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadTimeSpanArray());
		}

		[Test]
		public void CheckTimeSpanArrayOne()
		{
			var value = new TimeSpan[1];
			Writer.Write(value);
			CheckValue(2 + (1 * 8), 3 + (1 * 2), value, Reader.ReadTimeSpanArray());
		}

		[Test]
		public void CheckTimeSpanArrayTwo()
		{
			var value = new TimeSpan[2];
			Writer.Write(value);
			CheckValue(2 + (2 * 8), 3 + (2 * 2), value, Reader.ReadTimeSpanArray());
		}

		[Test]
		public void CheckTimeSpanArrayMany()
		{
			var value = new TimeSpan[100];
			Writer.Write(value);
			CheckValue(2 + (100 * 8), 3 + (100 * 2), value, Reader.ReadTimeSpanArray());
		}

		[Test]
		public void CheckTimeSpanArrayOptimizedNull()
		{
			TimeSpan[] value = null;
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedTimeSpanArray());
		}

		[Test]
		public void CheckTimeSpanArrayOptimizedEmpty()
		{
			var value = new TimeSpan[0];
			Writer.WriteOptimized(value);
			CheckValue(1, 2, value, Reader.ReadOptimizedTimeSpanArray());
		}

		[Test]
		public void CheckTimeSpanArrayOptimizedOneOptimizable()
		{
			var value = new TimeSpan[1];
			Writer.WriteOptimized(value);
			CheckValue(2 + (1 * 2), 3 + (1 * 2), value, Reader.ReadOptimizedTimeSpanArray());
		}

		[Test]
		public void CheckTimeSpanArrayOptimizedOneNotOptimizable()
		{
			var value = new[] {NotOptimizableTimeSpan};
			Writer.WriteOptimized(value);
			CheckValue(2 + (1 * 8), 3 + (1 * 8), value, Reader.ReadOptimizedTimeSpanArray());
		}

		[Test]
		public void CheckTimeSpanArrayOptimizedTwoOptimizable()
		{
			var value = new TimeSpan[2];
			Writer.WriteOptimized(value);
			CheckValue(2 + (2 * 2), 3 + (2 * 2), value, Reader.ReadOptimizedTimeSpanArray());
		}

		[Test]
		public void CheckTimeSpanArrayOptimizedTwoNotOptimizable()
		{
			var value = new[] {NotOptimizableTimeSpan, NotOptimizableTimeSpan};
			Writer.WriteOptimized(value);
			CheckValue(2 + (2 * 8), 3 + (2 * 8), value, Reader.ReadOptimizedTimeSpanArray());
		}

		[Test]
		public void CheckTimeSpanArrayOptimizedTwoPartOptimizable()
		{
			var value = new[] {TimeSpan.Zero, NotOptimizableTimeSpan};
			Writer.WriteOptimized(value);
			var expected = 2 + (1 * 2 + 1 * 8);
			expected += 1 + 1; // For BitArray
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedTimeSpanArray());
		}

		[Test]
		public void CheckTimeSpanArrayOptimizedManyNoneOptimizable()
		{
			var value = new TimeSpan[100];
			for(var i = 0; i < value.Length; i++) value[i] = NotOptimizableTimeSpan;
			Writer.WriteOptimized(value);
			var expected = 2 + (100 * 8);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedTimeSpanArray());
		}

		[Test]
		public void CheckTimeSpanArrayOptimizedManyAllOptimizable()
		{
			var value = new TimeSpan[100];
			for(var i = 0; i < value.Length; i++) value[i] = TimeSpan.Zero;
			Writer.WriteOptimized(value);
			var expected = 2 + (100 * 2);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedTimeSpanArray());
		}

		[Test]
		public void CheckTimeSpanArrayOptimizedManyPartiallyOptimizableAtLimit()
		{
			var value = new TimeSpan[100];
			for(var i = 0; i < value.Length; i++) value[i] = (i < 80) ? NotOptimizableTimeSpan : TimeSpan.Zero;
			Writer.WriteOptimized(value);
			var expected = 2 + (20 * 2) + (80 * 8);
			expected += 1 + 1 + (100 / 8); // For BitArray 
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedTimeSpanArray());
		}

		[Test]
		public void CheckTimeSpanArrayOptimizedManyPartiallyOptimizableAboveLimit()
		{
			var value = new TimeSpan[100];
			for(var i = 0; i < value.Length; i++) value[i] = (i < 81) ? NotOptimizableTimeSpan : TimeSpan.Zero;
			Writer.WriteOptimized(value);
			var expected = 1 + 1 + (100 * 8);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedTimeSpanArray());
		}

		[Test]
		public void CheckDateTimeArrayNull()
		{
			DateTime[] value = null;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadDateTimeArray());
		}

		[Test]
		public void CheckDateTimeArrayEmpty()
		{
			var value = new DateTime[0];
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadDateTimeArray());
		}

		[Test]
		public void CheckDateTimeArrayOne()
		{
			var value = new DateTime[1];
			Writer.Write(value);
			CheckValue(2 + (1 * 8), 3 + (1 * 3), value, Reader.ReadDateTimeArray());
		}

		[Test]
		public void CheckDateTimeArrayTwo()
		{
			var value = new DateTime[2];
			Writer.Write(value);
			CheckValue(2 + (2 * 8), 3 + (2 * 3), value, Reader.ReadDateTimeArray());
		}

		[Test]
		public void CheckDateTimeArrayMany()
		{
			var value = new DateTime[100];
			Writer.Write(value);
			CheckValue(2 + (100 * 8), 3 + (100 * 3), value, Reader.ReadDateTimeArray());
		}

		[Test]
		public void CheckDateTimeArrayOptimizedNull()
		{
			DateTime[] value = null;
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedDateTimeArray());
		}

		[Test]
		public void CheckDateTimeArrayOptimizedEmpty()
		{
			var value = new DateTime[0];
			Writer.WriteOptimized(value);
			CheckValue(1, 2, value, Reader.ReadOptimizedDateTimeArray());
		}

		[Test]
		public void CheckDateTimeArrayOptimizedOneOptimizable()
		{
			var value = new DateTime[1];
			Writer.WriteOptimized(value);
			CheckValue(2 + (1 * 3), 3 + (1 * 3), value, Reader.ReadOptimizedDateTimeArray());
		}

		[Test]
		public void CheckDateTimeArrayOptimizedOneNotOptimizable()
		{
			var value = new[] {NotOptimizableDateTime};
			Writer.WriteOptimized(value);
			CheckValue(2 + (1 * 8), 3 + (1 * 8), value, Reader.ReadOptimizedDateTimeArray());
		}

		[Test]
		public void CheckDateTimeArrayOptimizedTwoOptimizable()
		{
			var value = new DateTime[2];
			Writer.WriteOptimized(value);
			CheckValue(2 + (2 * 3), 3 + (2 * 3), value, Reader.ReadOptimizedDateTimeArray());
		}

		[Test]
		public void CheckDateTimeArrayOptimizedTwoNotOptimizable()
		{
			var value = new[] {NotOptimizableDateTime, NotOptimizableDateTime};
			Writer.WriteOptimized(value);
			CheckValue(2 + (2 * 8), 3 + (2 * 8), value, Reader.ReadOptimizedDateTimeArray());
		}

		[Test]
		public void CheckDateTimeArrayOptimizedTwoPartOptimizable()
		{
			var value = new[] {OptimizableDateTime, NotOptimizableDateTime};
			Writer.WriteOptimized(value);
			var expected = 2 + (1 * 3 + 1 * 8);
			expected += 1 + 1; // For BitArray
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedDateTimeArray());
		}

		[Test]
		public void CheckDateTimeArrayOptimizedManyNoneOptimizable()
		{
			var value = new DateTime[100];
			for(var i = 0; i < value.Length; i++) value[i] = NotOptimizableDateTime;
			Writer.WriteOptimized(value);
			var expected = 2 + (100 * 8);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedDateTimeArray());
		}

		[Test]
		public void CheckDateTimeArrayOptimizedManyAllOptimizable()
		{
			var value = new DateTime[100];
			for(var i = 0; i < value.Length; i++) value[i] = OptimizableDateTime;
			Writer.WriteOptimized(value);
			var expected = 2 + (100 * 3);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedDateTimeArray());
		}

		[Test]
		public void CheckDateTimeArrayOptimizedManyPartiallyOptimizableAtLimit()
		{
			var value = new DateTime[100];
			for(var i = 0; i < value.Length; i++) value[i] = (i < 80) ? NotOptimizableDateTime : OptimizableDateTime;
			Writer.WriteOptimized(value);
			var expected = 2 + (20 * 3) + (80 * 8);
			expected += 1 + 1 + (100 / 8); // For BitArray 
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedDateTimeArray());
		}

		[Test]
		public void CheckDateTimeArrayOptimizedManyPartiallyOptimizableAboveLimit()
		{
			var value = new DateTime[100];
			for(var i = 0; i < value.Length; i++) value[i] = (i < 81) ? NotOptimizableDateTime : OptimizableDateTime;
			Writer.WriteOptimized(value);
			var expected = 1 + 1 + (100 * 8);
			CheckValue(expected, 1 + expected, value, Reader.ReadOptimizedDateTimeArray());
		}

		[Test]
		public void CheckDecimalArrayNull()
		{
			Decimal[] value = null;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadDecimalArray());
		}

		[Test]
		public void CheckDecimalArrayEmpty()
		{
			var value = new Decimal[0];
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadDecimalArray());
		}

		[Test]
		public void CheckDecimalArrayOne()
		{
			var value = new Decimal[1];
			Writer.Write(value);
			CheckValue(2 + (1 * 1), 2 + (1 * 1), value, Reader.ReadDecimalArray());
		}

		[Test]
		public void CheckDecimalArrayTwo()
		{
			var value = new Decimal[2];
			Writer.Write(value);
			CheckValue(2 + (2 * 1), 2 + (2 * 1), value, Reader.ReadDecimalArray());
		}

		[Test]
		public void CheckDecimalArrayMany()
		{
			var value = new Decimal[100];
			Writer.Write(value);
			CheckValue(2 + (100 * 1), 2 + (100 * 1), value, Reader.ReadDecimalArray());
		}

		[Test]
		public void CheckDecimalArrayOptimizedNull()
		{
			Decimal[] value = null;
			Writer.WriteOptimized(value);
			CheckValue(1, 1, value, Reader.ReadOptimizedDecimalArray());
		}

		[Test]
		public void CheckDecimalArrayOptimizedEmpty()
		{
			var value = new Decimal[0];
			Writer.WriteOptimized(value);
			CheckValue(1, 2, value, Reader.ReadOptimizedDecimalArray());
		}

		[Test]
		public void CheckDecimalArrayOptimizedOneOptimizable()
		{
			var value = new Decimal[1];
			Writer.WriteOptimized(value);
			CheckValue(2 + (1 * 1), 2 + (1 * 1), value, Reader.ReadOptimizedDecimalArray());
		}

		[Test]
		public void CheckDecimalArrayOptimizedOneNotOptimizable()
		{
			var value = new Decimal[] {-1};
			Writer.WriteOptimized(value);
			CheckValue(2 + (1 * 2), 2 + (1 * 2), value, Reader.ReadOptimizedDecimalArray());
		}

		[Test]
		public void CheckDecimalArrayOptimizedTwoOptimizable()
		{
			var value = new Decimal[2];
			Writer.WriteOptimized(value);
			CheckValue(2 + (2 * 1), 2 + (2 * 1), value, Reader.ReadOptimizedDecimalArray());
		}

		[Test]
		public void CheckDecimalArrayOptimizedTwoNotOptimizable()
		{
			var value = new Decimal[] {-1, -1};
			Writer.WriteOptimized(value);
			CheckValue(2 + (2 * 2), 2 + (2 * 2), value, Reader.ReadOptimizedDecimalArray());
		}

		[Test]
		public void CheckDecimalArrayOptimizedTwoPartOptimizable()
		{
			var value = new Decimal[] {1, -1};
			Writer.WriteOptimized(value);
			var expected = 2 + (1 * 1 + 1 * 1);
			expected += 1 + 1; // For BitArray
			CheckValue(expected, expected, value, Reader.ReadOptimizedDecimalArray());
		}

		[Test]
		public void CheckDecimalArrayOptimizedManyNoneOptimizable()
		{
			var value = new Decimal[100];
			for(var i = 0; i < value.Length; i++) value[i] = -1;
			Writer.WriteOptimized(value);
			var expected = 2 + (100 * 2);
			CheckValue(expected, expected, value, Reader.ReadOptimizedDecimalArray());
		}

		[Test]
		public void CheckDecimalArrayOptimizedManyAllOptimizable()
		{
			var value = new Decimal[100];
			for(var i = 0; i < value.Length; i++) value[i] = 0;
			Writer.WriteOptimized(value);
			var expected = 2 + (100 * 1);
			CheckValue(expected, expected, value, Reader.ReadOptimizedDecimalArray());
		}

		[Test]
		public void CheckSByteArrayNull()
		{
			SByte[] value = null;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadSByteArray());
		}

		[Test]
		public void CheckSByteArrayEmpty()
		{
			var value = new SByte[0];
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadSByteArray());
		}

		[Test]
		public void CheckSByteArrayOne()
		{
			var value = new SByte[1];
			Writer.Write(value);
			CheckValue(2 + (1 * 1), 2 + (1 * 1), value, Reader.ReadSByteArray());
		}

		[Test]
		public void CheckSByteArrayTwo()
		{
			var value = new SByte[2];
			Writer.Write(value);
			CheckValue(2 + (2 * 1), 2 + (2 * 1), value, Reader.ReadSByteArray());
		}

		[Test]
		public void CheckSByteArrayMany()
		{
			var value = new SByte[100];
			Writer.Write(value);
			CheckValue(2 + (100 * 1), 2 + (100 * 1), value, Reader.ReadSByteArray());
		}

		[Test]
		public void CheckSingleArrayNull()
		{
			Single[] value = null;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadSingleArray());
		}

		[Test]
		public void CheckSingleArrayEmpty()
		{
			var value = new Single[0];
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadSingleArray());
		}

		[Test]
		public void CheckSingleArrayOne()
		{
			var value = new Single[1];
			Writer.Write(value);
			CheckValue(2 + (1 * 4), 2 + (1 * 4), value, Reader.ReadSingleArray());
		}

		[Test]
		public void CheckSingleArrayTwo()
		{
			var value = new Single[2];
			Writer.Write(value);
			CheckValue(2 + (2 * 4), 2 + (2 * 4), value, Reader.ReadSingleArray());
		}

		[Test]
		public void CheckSingleArrayMany()
		{
			var value = new Single[100];
			Writer.Write(value);
			CheckValue(2 + (100 * 4), 2 + (100 * 4), value, Reader.ReadSingleArray());
		}

		[Test]
		public void CheckDoubleArrayNull()
		{
			Double[] value = null;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadDoubleArray());
		}

		[Test]
		public void CheckDoubleArrayEmpty()
		{
			var value = new Double[0];
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadDoubleArray());
		}

		[Test]
		public void CheckDoubleArrayOne()
		{
			var value = new Double[1];
			Writer.Write(value);
			CheckValue(2 + (1 * 8), 2 + (1 * 8), value, Reader.ReadDoubleArray());
		}

		[Test]
		public void CheckDoubleArrayTwo()
		{
			var value = new Double[2];
			Writer.Write(value);
			CheckValue(2 + (2 * 8), 2 + (2 * 8), value, Reader.ReadDoubleArray());
		}

		[Test]
		public void CheckDoubleArrayMany()
		{
			var value = new Double[100];
			Writer.Write(value);
			CheckValue(2 + (100 * 8), 2 + (100 * 8), value, Reader.ReadDoubleArray());
		}

		[Test]
		public void CheckGuidArrayNull()
		{
			Guid[] value = null;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadGuidArray());
		}

		[Test]
		public void CheckGuidArrayEmpty()
		{
			var value = new Guid[0];
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadGuidArray());
		}

		[Test]
		public void CheckGuidArrayOne()
		{
			var value = new Guid[1];
			Writer.Write(value);
			CheckValue(2 + (1 * 16), 2 + (1 * 16), value, Reader.ReadGuidArray());
		}

		[Test]
		public void CheckGuidArrayTwo()
		{
			var value = new Guid[2];
			Writer.Write(value);
			CheckValue(2 + (2 * 16), 2 + (2 * 16), value, Reader.ReadGuidArray());
		}

		[Test]
		public void CheckGuidArrayMany()
		{
			var value = new Guid[100];
			Writer.Write(value);
			CheckValue(2 + (100 * 16), 2 + (100 * 16), value, Reader.ReadGuidArray());
		}

		[Test]
		public void CheckCharArrayNull()
		{
			Char[] value = null;
// ReSharper disable AssignNullToNotNullAttribute
			Writer.Write(value);
// ReSharper restore AssignNullToNotNullAttribute
			CheckValue(1, 1, value, Reader.ReadCharArray());
		}

		[Test]
		public void CheckCharArrayEmpty()
		{
			var value = new char[0];
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadCharArray());
		}

		[Test]
		public void CheckCharArrayOne()
		{
			var value = new[] {'a'};
			Writer.Write(value);
			CheckValue(2 + 1, 2 + 1, value, Reader.ReadCharArray());
		}

		[Test]
		public void CheckCharArrayTwo()
		{
			var value = new[] {'a', 'b'};
			Writer.Write(value);
			CheckValue(2 + 2, 2 + 2, value, Reader.ReadCharArray());
		}

		[Test]
		public void CheckCharArrayMany()
		{
			var value = new char[100];
			Writer.Write(value);
			CheckValue(2 + 100, 2 + 100, value, Reader.ReadCharArray());
		}

		[Test]
		public void CheckByteArrayNull()
		{
			Byte[] value = null;
// ReSharper disable AssignNullToNotNullAttribute
			Writer.Write(value);
// ReSharper restore AssignNullToNotNullAttribute
			CheckValue(1, 1, value, Reader.ReadByteArray());
		}

		[Test]
		public void CheckByteArrayEmpty()
		{
			var value = new Byte[0];
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadByteArray());
		}

		[Test]
		public void CheckByteArrayOne()
		{
			var value = new Byte[] {10};
			Writer.Write(value);
			CheckValue(2 + 1, 2 + 1, value, Reader.ReadByteArray());
		}

		[Test]
		public void CheckByteArrayTwo()
		{
			var value = new Byte[] {10, 11};
			Writer.Write(value);
			CheckValue(2 + 2, 2 + 2, value, Reader.ReadByteArray());
		}

		[Test]
		public void CheckByteArrayMany()
		{
			var value = new Byte[100];
			Writer.Write(value);
			CheckValue(2 + 100, 2 + 100, value, Reader.ReadByteArray());
		}

		[Test]
		public void CheckBooleanArrayNull()
		{
			Boolean[] value = null;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadBooleanArray());
		}

		[Test]
		public void CheckBooleanArrayEmpty()
		{
			var value = new Boolean[0];
			Writer.Write(value);
			CheckValue(1, 2, value, Reader.ReadBooleanArray());
		}

		[Test]
		public void CheckBooleanArrayOne()
		{
			var value = new Boolean[1];
			Writer.Write(value);
			CheckValue(3, 3, value, Reader.ReadBooleanArray());
		}

		[Test]
		public void CheckBooleanArrayTwo()
		{
			var value = new Boolean[2];
			Writer.Write(value);
			CheckValue(3, 3, value, Reader.ReadBooleanArray());
		}

		[Test]
		public void CheckBooleanArrayOneHundred()
		{
			var value = new Boolean[100];
			Writer.Write(value);
			CheckValue(15, 15, value, Reader.ReadBooleanArray());
		}

		[Test]
		public void CheckBitArrayNull()
		{
			BitArray value = null;
			Writer.Write(value);
			CheckValue(1, 1, value, Reader.ReadBitArray());
		}

		[Test]
		public void CheckBitArrayEmpty()
		{
			var value = new BitArray(0);
			Writer.Write(value);
			CheckValue(2, 2, value, Reader.ReadBitArray(), new BitArrayComparer());
		}

		[Test]
		public void CheckBitArrayOne()
		{
			var value = new BitArray(1, true);
			Writer.Write(value);
			CheckValue(3, 3, value, Reader.ReadBitArray(), new BitArrayComparer());
		}

		[Test]
		public void CheckBitArrayNine()
		{
			var value = new BitArray(9, true);
			Writer.Write(value);
			CheckValue(4, 4, value, Reader.ReadBitArray(), new BitArrayComparer());
		}

		[Test]
		public void CheckBitArrayOneHundred()
		{
			var value = new BitArray(100, true);
			Writer.Write(value);
			CheckValue(15, 15, value, Reader.ReadBitArray(), new BitArrayComparer());
		}

		[Test]
		public void CheckBitArrayOptimizedEmpty()
		{
			var value = new BitArray(0);
			Writer.WriteOptimized(value);
			CheckValue(1, 2, value, Reader.ReadOptimizedBitArray(), new BitArrayComparer());
		}

		[Test]
		public void CheckBitArrayOptimizedOne()
		{
			var value = new BitArray(1);
			Writer.WriteOptimized(value);
			CheckValue(2, 3, value, Reader.ReadOptimizedBitArray(), new BitArrayComparer());
		}

		[Test]
		public void CheckBitArrayOptimizedNine()
		{
			var value = new BitArray(9);
			Writer.WriteOptimized(value);
			CheckValue(3, 4, value, Reader.ReadOptimizedBitArray(), new BitArrayComparer());
		}

		[Test]
		public void CheckBitArrayOptimizedOneHundred()
		{
			var value = new BitArray(100);
			Writer.WriteOptimized(value);
			CheckValue(14, 15, value, Reader.ReadOptimizedBitArray(), new BitArrayComparer());
		}

		static Dictionary<string, int> CreateSimpleStringIntDictionary()
		{
			var value = new Dictionary<string, int>
			            	{
			            		{ "a", 1 },
			            		{ "b", 2 },
			            		{ "c", 3 },
			            		{ "d", 4 },
			            		{ "e", 5 }
			            	};
			return value;
		}

		static Dictionary<int, string> CreateSimpleIntStringDictionary()
		{
			var value = new Dictionary<int, string>
			                                	{
			                                		{ 1, "a" },
			                                		{ 2, "b" },
			                                		{ 3, "c" },
			                                		{ 4, "d" },
			                                		{ 5, "e" }
			                                	};
			return value;
		}

		static Dictionary<int, int> CreateSimpleIntIntDictionary()
		{
			var value = new Dictionary<int, int> { { 1, 1 }, { 2, 4 }, { 3, 9 }, { 4, 16 }, { 5, 25 } };
			return value;
		}

		static Dictionary<int, object> CreateSimpleIntObjectDictionary()
		{
			var value = new Dictionary<int, object>
			                                	{
			                                		{ 1, "a" },
			                                		{ 2, "b" },
			                                		{ 3, "c" },
			                                		{ 4, "d" },
			                                		{ 5, "e" }
			                                	};
			return value;
		}

		static Dictionary<int, CustomClass> CreateSimpleIntCustomClassDictionary()
		{
			var value = new Dictionary<int, CustomClass>
			                                     	{
			                                     		{ 1, new CustomClass(1, "a") },
			                                     		{ 2, new CustomClass(2, "b") },
			                                     		{ 3, new CustomClass(3, "b") },
			                                     		{ 4, new CustomClass(4, "b") },
			                                     		{ 5, new CustomClass(5, "b") }
			                                     	};
			return value;
		}

		static Dictionary<int, IntelligentCustomClass> CreateSimpleIntIntelligentCustomClassDictionary()
		{
			var value = new Dictionary<int, IntelligentCustomClass>
			                                                	{
			                                                		{ 1, new IntelligentCustomClass(1, "a") },
			                                                		{ 2, new IntelligentCustomClass(2, "b") },
			                                                		{ 3, new IntelligentCustomClass(3, "b") },
			                                                		{ 4, new IntelligentCustomClass(4, "b") },
			                                                		{ 5, new IntelligentCustomClass(5, "b") }
			                                                	};
			return value;
		}

		static Dictionary<string, string> CreateSimpleStringStringDictionary()
		{
			var value = new Dictionary<string, string>
			                                   	{
			                                   		{ "a", "Alpha" },
			                                   		{ "b", "Bravo" },
			                                   		{ "c", "Charlie" },
			                                   		{ "d", "Delta" },
			                                   		{ "e", "Echo" }
			                                   	};
			return value;
		}

		[Test]
		public void CheckDictionaryStringAndInt()
		{
			var value = CreateSimpleStringIntDictionary();
			Writer.Write(value);
			CheckValue(20, 1441, value, Reader.ReadDictionary<string, int>(), new DictionaryComparer<string, int>());
		}

		[Test]
		public void CheckDictionaryStringAndIntPreCreate()
		{
			var value = CreateSimpleStringIntDictionary();
			Writer.Write(value);
			var result = new Dictionary<string, int>();
			Reader.ReadDictionary(result);
			CheckValue(20, 1441, value, result, new DictionaryComparer<string, int>());
		}

		[Test]
		public void CheckDictionaryIntAndString()
		{
			var value = CreateSimpleIntStringDictionary();
			Writer.Write(value);
			CheckValue(20, 1439, value, Reader.ReadDictionary<int, string>(), new DictionaryComparer<int, string>());
		}

		[Test]
		public void CheckDictionaryIntAndInt()
		{
			var value = CreateSimpleIntIntDictionary();
			Writer.Write(value);
			CheckValue(16, 1421, value, Reader.ReadDictionary<int, int>(), new DictionaryComparer<int, int>());
		}

		[Test]
		public void CheckDictionaryIntAndObject()
		{
			var value = CreateSimpleIntObjectDictionary();
			Writer.Write(value);
			CheckValue(20, 1439, value, Reader.ReadDictionary<int, object>(), new DictionaryComparer<int, object>());
		}

		[Test]
		public void CheckDictionaryIntAndCustomClass()
		{
			var value = CreateSimpleIntCustomClassDictionary();
			Writer.Write(value);
			CheckValue(CustomClass.FullTypeSize * value.Count + 370, CustomClass.FullTypeSize * value.Count + 1236, value, Reader.ReadDictionary<int, CustomClass>(), new DictionaryComparer<int, CustomClass>());
		}

		[Test]
		public void CheckDictionaryIntAndIntelligentCustomClass()
		{
			var value = CreateSimpleIntIntelligentCustomClassDictionary();
			Writer.Write(value);
			CheckValue(25, IntelligentCustomClass.FullTypeSize * value.Count + 1247, value, Reader.ReadDictionary<int, IntelligentCustomClass>(), new DictionaryComparer<int, IntelligentCustomClass>());
		}

		[Test]
		public void CheckDictionaryStringAndString()
		{
			var value = CreateSimpleStringStringDictionary();
			Writer.Write(value);
			CheckValue(24, 1480, value, Reader.ReadDictionary<string, string>(), new DictionaryComparer<string, string>());
		}
		
		[Test]
		public void CheckDictionaryIntAndCustomStruct()
		{
			var value = new Dictionary<int, CustomStruct>();
			CustomStruct s;
			s = new CustomStruct { IntValue = 1, StringValue = "A" }; value.Add(1, s);
			s = new CustomStruct { IntValue = 2, StringValue = "B" }; value.Add(2, s);
			s = new CustomStruct { IntValue = 3, StringValue = "C" }; value.Add(3, s);
			Writer.Write(value);
			CheckValue(CustomStruct.FullTypeSize * value.Count + 224, -1, value, Reader.ReadDictionary<int, CustomStruct>(), new DictionaryComparer<int, CustomStruct>());
		}
		
		[Test]
		public void CheckListOfInt()
		{
			var value = new List<int> { 1, 2, 3, 4, 5 };
			Writer.Write(value);
			CheckValue(8, 238, value, Reader.ReadList<int>(), new ListComparer<int>());
		}

		[Test]
		public void CheckListOfString()
		{
			var value = new List<string> { "a", "b", "c", "d", "e" };
			Writer.Write(value);
			CheckValue(12, 242, value, Reader.ReadList<string>(), new ListComparer<string>());
		}

		[Test]
		public void CheckListOfObject()
		{
			var value = new List<object> { new object(), new object(), new object(), new object(), new object() };
			Writer.Write(value);
			Reader.ReadList<object>();
			CheckValue(212, -1, null, null);
		}

		[Test]
		public void CheckListOfCustomStruct()
		{
			var value = new List<CustomStruct>
			            	{
			            		new CustomStruct { IntValue = 1, StringValue = "A" },
			            		new CustomStruct { IntValue = 2, StringValue = "B" },
			            		new CustomStruct { IntValue = 3, StringValue = "C" }
			            	};

			Writer.Write(value);
			CheckValue((72 + CustomStruct.FullTypeSize) * value.Count + 2, CustomStruct.FullTypeSize * 2 + 4 * value.Count + 301 , value, Reader.ReadList<CustomStruct>(), new ListComparer<CustomStruct>());
		}

		[Test]
		public void CheckListOfIntelligentCustomStruct()
		{
			var value = new List<IntelligentCustomStruct>
			            	{
			            		new IntelligentCustomStruct { IntValue = 1, StringValue = "A" },
			            		new IntelligentCustomStruct { IntValue = 2, StringValue = "B" },
			            		new IntelligentCustomStruct { IntValue = 3, StringValue = "C" }
			            	};

			Writer.Write(value);
			CheckValue(11, IntelligentCustomStruct.FullTypeSize * 2 + 4 * value.Count + 323, value, Reader.ReadList<IntelligentCustomStruct>(), new ListComparer<IntelligentCustomStruct>());
		}

		[Test]
		public void CheckListOfCustomClass()
		{
			var value = new List<CustomClass>();
			CustomClass s;
			s = new CustomClass { IntValue = 1, StringValue = "A" }; value.Add(s);
			s = new CustomClass { IntValue = 2, StringValue = "B" }; value.Add(s);
			s = new CustomClass { IntValue = 3, StringValue = "C" }; value.Add(s);
			Writer.Write(value);
			CheckValue(CustomClass.FullTypeSize * 3 + 218, CustomStruct.FullTypeSize * 2 + 4 * value.Count + 299, value, Reader.ReadList<CustomClass>(), new ListComparer<CustomClass>());
		}

		[Test]
		public void CheckListOfSemiIntelligentCustomClass()
		{
			var value = new List<SemiIntelligentCustomClass>();
			SemiIntelligentCustomClass s;
			s = new SemiIntelligentCustomClass { IntValue = 1, StringValue = "A" }; value.Add(s);
			s = new SemiIntelligentCustomClass { IntValue = 2, StringValue = "B" }; value.Add(s);
			s = new SemiIntelligentCustomClass { IntValue = 3, StringValue = "C" }; value.Add(s);
			Writer.Write(value);
			CheckValue(SemiIntelligentCustomClass.FullTypeSize * value.Count + 218, SemiIntelligentCustomClass.FullTypeSize * 2 + 4 * value.Count + 331, value, Reader.ReadList<SemiIntelligentCustomClass>(), new ListComparer<SemiIntelligentCustomClass>());
		}

		[Test]
		public void CheckListOfIntelligentCustomClass()
		{
			var value = new List<IntelligentCustomClass>();
			IntelligentCustomClass s;
			s = new IntelligentCustomClass { IntValue = 1, StringValue = "A" }; value.Add(s);
			s = new IntelligentCustomClass { IntValue = 2, StringValue = "B" }; value.Add(s);
			s = new IntelligentCustomClass { IntValue = 3, StringValue = "C" }; value.Add(s);
			Writer.Write(value);
			CheckValue(11, IntelligentCustomStruct.FullTypeSize * 2 + 4 * value.Count + 321, value, Reader.ReadList<IntelligentCustomClass>(), new ListComparer<IntelligentCustomClass>());
		}

		[Test]
		public void CheckListOfMixedCustomClass()
		{
			var value = new List<CustomClass>();
			var baseClass = new CustomClass { IntValue = 1, StringValue = "A" };
			value.Add(baseClass);
			var inheritedClass = new InheritedCustomClass { IntValue = 1, StringValue = "A" };
			value.Add(inheritedClass);
			Writer.Write(value);
			CheckValue(CustomClass.FullTypeSize + InheritedCustomClass.FullTypeSize + 163, CustomClass.FullTypeSize + InheritedCustomClass.FullTypeSize + 4 * value.Count + 360, value, Reader.ReadList<CustomClass>(), new ListComparer<CustomClass>());
		}

		[Test]
		public void CheckListOfMixedSemiIntelligentCustomClass()
		{
			var value = new List<SemiIntelligentCustomClass>();
			var baseClass = new SemiIntelligentCustomClass { IntValue = 1, StringValue = "A" };
			value.Add(baseClass);
			var inheritedClass = new InheritedSemiIntelligentCustomClass
			                                                     	{
			                                                     		IntValue = 1,
			                                                     		StringValue = "A"
			                                                     	};
			value.Add(inheritedClass);
			Writer.Write(value);
			CheckValue(SemiIntelligentCustomClass.FullTypeSize + InheritedSemiIntelligentCustomClass.FullTypeSize + 163,
								 SemiIntelligentCustomClass.FullTypeSize + InheritedSemiIntelligentCustomClass.FullTypeSize + 413,
			           value, Reader.ReadList<SemiIntelligentCustomClass>(), new ListComparer<SemiIntelligentCustomClass>());
		}

		[Test]
		public void CheckListOfMixedIntelligentCustomClass()
		{
			var value = new List<IntelligentCustomClass>();
			var baseClass = new IntelligentCustomClass { IntValue = 1, StringValue = "A" };
			value.Add(baseClass);
			var inheritedClass = new InheritedIntelligentCustomClass
			                                                 	{
			                                                 		IntValue = 1,
			                                                 		StringValue = "A"
			                                                 	};
			value.Add(inheritedClass);
			Writer.Write(value);
			CheckValue(18, IntelligentCustomClass.FullTypeSize + InheritedIntelligentCustomClass.FullTypeSize + 401, value, Reader.ReadList<IntelligentCustomClass>(), new ListComparer<IntelligentCustomClass>());
		}

		[Test]
		public void CheckNullableInt16Null()
		{
			Int16? value = null;
			Writer.WriteNullable(value);
			CheckValue(1, 1, value, Reader.ReadNullableInt16());
		}

		[Test]
		public void CheckInt16AsNullable()
		{
			Int16 value = 33;
			Writer.WriteNullable(value);
			CheckValue(2, 2, value, Reader.ReadNullableInt16());
		}

		[Test]
		public void CheckNullableInt16()
		{
			Int16? value = 33;
			Writer.WriteNullable(value);
			CheckValue(2, 2, value, Reader.ReadNullableInt16());
		}

		[Test]
		public void CheckNullableByteNull()
		{
			Byte? value = null;
			Writer.WriteNullable(value);
			CheckValue(1, 1, value, Reader.ReadNullableByte());
		}

		[Test]
		public void CheckNullableByte()
		{
			Byte? value = 33;
			Writer.WriteNullable(value);
			CheckValue(2, 2, value, Reader.ReadNullableByte());
		}

		[Test]
		public void CheckNullableGuidNull()
		{
			Guid? value = null;
			Writer.WriteNullable(value);
			CheckValue(1, 1, value, Reader.ReadNullableGuid());
		}

		[Test]
		public void CheckNullableGuid()
		{
			Guid? value = Guid.NewGuid();
			Writer.WriteNullable(value);
			CheckValue(17, 17, value, Reader.ReadNullableGuid());
		}

		[Test]
		public void CheckNullableCharNull()
		{
			Char? value = null;
			Writer.WriteNullable(value);
			CheckValue(1, 1, value, Reader.ReadNullableChar());
		}

		[Test]
		public void CheckNullableChar()
		{
			Char? value = 'A';
			Writer.WriteNullable(value);
			CheckValue(2, 2, value, Reader.ReadNullableChar());
		}

		[Test]
		public void CheckNullableBooleanNull()
		{
			Boolean? value = null;
			Writer.WriteNullable(value);
			CheckValue(1, 1, value, Reader.ReadNullableBoolean());
		}

		[Test]
		public void CheckNullableBooleanTrue()
		{
			Boolean? value = true;
			Writer.WriteNullable(value);
			CheckValue(1, 1, value, Reader.ReadNullableBoolean());
		}

		[Test]
		public void CheckNullableBooleanFalse()
		{
			Boolean? value = true;
			Writer.WriteNullable(value);
			CheckValue(1, 1, value, Reader.ReadNullableBoolean());
		}

		[Test]
		public void CheckNullableSingleNull()
		{
			Single? value = null;
			Writer.WriteNullable(value);
			CheckValue(1, 1, value, Reader.ReadNullableSingle());
		}

		[Test]
		public void CheckNullableSingle()
		{
			Single? value = 33;
			Writer.WriteNullable(value);
			CheckValue(5, 5, value, Reader.ReadNullableSingle());
		}

		[Test]
		public void CheckNullableDoubleNull()
		{
			Double? value = null;
			Writer.WriteNullable(value);
			CheckValue(1, 1, value, Reader.ReadNullableDouble());
		}

		[Test]
		public void CheckNullableDouble()
		{
			Double? value = 33;
			Writer.WriteNullable(value);
			CheckValue(9, 9, value, Reader.ReadNullableDouble());
		}

		[Test]
		public void CheckNullableSByteNull()
		{
			SByte? value = null;
			Writer.WriteNullable(value);
			CheckValue(1, 1, value, Reader.ReadNullableSByte());
		}

		[Test]
		public void CheckNullableSByte()
		{
			SByte? value = 33;
			Writer.WriteNullable(value);
			CheckValue(2, 2, value, Reader.ReadNullableSByte());
		}

		[Test]
		public void CheckNullableUInt16Null()
		{
			UInt16? value = null;
			Writer.WriteNullable(value);
			CheckValue(1, 1, value, Reader.ReadNullableUInt16());
		}

		[Test]
		public void CheckNullableUInt16()
		{
			UInt16? value = 33;
			Writer.WriteNullable(value);
			CheckValue(2, 2, value, Reader.ReadNullableUInt16());
		}

		[Test]
		public void CheckNullableInt32Null()
		{
			Int32? value = null;
			Writer.WriteNullable(value);
			CheckValue(1, 1, value, Reader.ReadNullableInt32());
		}

		[Test]
		public void CheckNullableInt32()
		{
			Int32? value = 33;
			Writer.WriteNullable(value);
			CheckValue(2, 2, value, Reader.ReadNullableInt32());
		}

		[Test]
		public void CheckNullableDecimalNull()
		{
			Decimal? value = null;
			Writer.WriteNullable(value);
			CheckValue(1, 1, value, Reader.ReadNullableDecimal());
		}

		[Test]
		public void CheckNullableDecimal()
		{
			Decimal? value = 33;
			Writer.WriteNullable(value);
			CheckValue(3, 3, value, Reader.ReadNullableDecimal());
		}

		[Test]
		public void CheckNullableDateTimeNull()
		{
			DateTime? value = null;
			Writer.WriteNullable(value);
			CheckValue(1, 1, value, Reader.ReadNullableDateTime());
		}

		[Test]
		public void CheckNullableDateTime()
		{
			DateTime? value = new DateTime(2006, 9, 17);
			Writer.WriteNullable(value);
			CheckValue(4, 4, value, Reader.ReadNullableDateTime());
		}

		[Test]
		public void CheckNullableTimeSpanNull()
		{
			TimeSpan? value = null;
			Writer.WriteNullable(value);
			CheckValue(1, 1, value, Reader.ReadNullableTimeSpan());
		}

		[Test]
		public void CheckNullableTimeSpan()
		{
			TimeSpan? value = TimeSpan.FromDays(1);
			Writer.WriteNullable(value);
			CheckValue(4, 4, value, Reader.ReadNullableTimeSpan());
		}

		[Test]
		public void CheckNullableUInt32Null()
		{
			UInt32? value = null;
			Writer.WriteNullable(value);
			CheckValue(1, 1, value, Reader.ReadNullableUInt32());
		}

		[Test]
		public void CheckNullableUInt32()
		{
			UInt32? value = 33;
			Writer.WriteNullable(value);
			CheckValue(2, 2, value, Reader.ReadNullableUInt32());
		}

		[Test]
		public void CheckNullableUInt64Null()
		{
			UInt64? value = null;
			Writer.WriteNullable(value);
			CheckValue(1, 1, value, Reader.ReadNullableUInt64());
		}

		[Test]
		public void CheckNullableUInt64()
		{
			UInt64? value = 33;
			Writer.WriteNullable(value);
			CheckValue(2, 2, value, Reader.ReadNullableUInt64());
		}

		[Test]
		public void CheckNullableInt64Null()
		{
			Int64? value = null;
			Writer.WriteNullable(value);
			CheckValue(1, 1, value, Reader.ReadNullableInt64());
		}

		[Test]
		public void CheckNullableInt64()
		{
			Int64? value = 33;
			Writer.WriteNullable(value);
			CheckValue(2, 2, value, Reader.ReadNullableInt64());
		}
		
		[Test]
		public void CheckNullableNonPrimitiveStructNull()
		{
			CustomStruct? value = null;
			Writer.WriteNullable(value);
			var result = (CustomStruct?) Reader.ReadNullable();
			CheckValue(1, 1, value, result);
			Assert.IsFalse(result.HasValue);
		}

		[Test]
		public void CheckNullableNonPrimitiveStruct()
		{
			var structValue = new CustomStruct { IntValue = 21, StringValue = "Simon" };
			CustomStruct? value = structValue;
			Writer.WriteNullable(value);
			var result = (CustomStruct?) Reader.ReadNullable();
			CheckValue(-1, -1, value, result);
			Assert.IsTrue(result.HasValue == value.HasValue);
			Assert.IsTrue(result.Value.IntValue == value.Value.IntValue);
			Assert.IsTrue(result.Value.StringValue == value.Value.StringValue);
		}
		
		[Test]
		public void CheckNullableArrayNull()
		{
			var value = new int?[10];
			Writer.WriteTypedArray(value);
			var result = (int?[]) Reader.ReadTypedArray();
			Assert.AreEqual(value.Length, result.Length);
			for(var i = 0; i < value.Length; i++)
			{
				Assert.IsNull(result[i]);
			}
		}

		[Test]
		public void CheckNullableArray()
		{
			var value = new int?[10];
			for (var i = 0; i < value.Length; i++) value[i] = i;
			Writer.WriteTypedArray(value);
			var result = (int?[]) Reader.ReadTypedArray();
			Assert.AreEqual(value.Length, result.Length);
			for(var i = 0; i < value.Length; i++)
			{
				Assert.AreEqual(value[i], result[i]);
			}
		}

		[Test]
		public void CheckDateTimeNet20Unspecified()
		{
			var value = new DateTime(2006, 11, 05, 15, 08, 20, DateTimeKind.Unspecified);
			value = value.AddTicks(2);
			Writer.Write(value);
			var result = Reader.ReadDateTime();
			CheckValue(8, 9, value, result);
			Assert.AreEqual(value.Kind, result.Kind);
		}

		[Test]
		public void CheckDateTimeNet20Utc()
		{
			var value = new DateTime(2006, 11, 05, 15, 08, 20, DateTimeKind.Utc);
			value = value.AddTicks(2);
			Writer.Write(value);
			var result = Reader.ReadDateTime();
			CheckValue(8, 9, value, result);
			Assert.AreEqual(value.Kind, result.Kind);
		}

		[Test]
		public void CheckDateTimeNet20Local()
		{
			var value = new DateTime(2006, 11, 05, 15, 08, 20, DateTimeKind.Local);
			value = value.AddTicks(2);
			Writer.Write(value);
			var result = Reader.ReadDateTime();
			CheckValue(8, 9, value, result);
			Assert.AreEqual(value.Kind, result.Kind);
		}

		[Test]
		public void CheckDateTimeOptimizedUtcWithDateOnly()
		{
			var value = DateTime.SpecifyKind(new DateTime(2006, 9, 17), DateTimeKind.Utc);
			Writer.WriteOptimized(value);
			CheckValue(5, 6, value, Reader.ReadOptimizedDateTime());
		}

		[Test]
		public void CheckDateTimeOptimizedLocalWithDateOnly()
		{
			var value = DateTime.SpecifyKind(new DateTime(2006, 9, 17), DateTimeKind.Local);
			Writer.WriteOptimized(value);
			CheckValue(5, 6, value, Reader.ReadOptimizedDateTime());
		}

		[Test]
		public void CheckDateTimeOptimizedUtcWithDateHoursAndMinutes()
		{
			var value = new DateTime(2006, 9, 17, 12, 20, 0, DateTimeKind.Utc);
			Writer.WriteOptimized(value);
			var result = Reader.ReadOptimizedDateTime();
			CheckValue(5, 6, value, result);
			Assert.AreEqual(value.Kind, result.Kind);
		}

		[Test]
		public void CheckDateTimeOptimizedLocalWithDateHoursAndMinutes()
		{
			var value = new DateTime(2006, 9, 17, 12, 20, 0, DateTimeKind.Local);
			Writer.WriteOptimized(value);
			var result = Reader.ReadOptimizedDateTime();
			CheckValue(5, 6, value, result);
			Assert.AreEqual(value.Kind, result.Kind);
		}

		[Test]
		public void CheckDateTimeOptimizedUtcWithDateHoursAndMinutesAndSeconds()
		{
			var value = new DateTime(2006, 9, 17, 12, 20, 22, DateTimeKind.Utc);
			Writer.WriteOptimized(value);
			var result = Reader.ReadOptimizedDateTime();
			CheckValue(6, 7, value, result);
			Assert.AreEqual(value.Kind, result.Kind);
		}

		[Test]
		public void CheckDateTimeOptimizedLocalWithDateHoursAndMinutesAndSeconds()
		{
			var value = new DateTime(2006, 9, 17, 12, 20, 22, DateTimeKind.Local);
			Writer.WriteOptimized(value);
			var result = Reader.ReadOptimizedDateTime();
			CheckValue(6, 7, value, result);
			Assert.AreEqual(value.Kind, result.Kind);
		}

		[Test]
		public void CheckCustomStructTypedArrayNull()
		{
			CustomStruct[] value = null;
			Writer.WriteTypedArray(value);
			var result = (CustomStruct[]) Reader.ReadTypedArray();

			CheckValue(1, 1, value, result, new ListComparer());
		}

		[Test] //!!!
		public void CheckCustomStructTypedArrayEmpty()
		{
			var value = new CustomStruct[0];
			Writer.WriteTypedArray(value);
			var result = (CustomStruct[]) Reader.ReadTypedArray();

			CheckValue(3, 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckCustomStructTypedArrayOne()
		{
			var value = new[] { new CustomStruct() };
			value[0].IntValue = 3;
			value[0].StringValue = "a";
			Writer.WriteTypedArray(value);
			var result = (CustomStruct[]) Reader.ReadTypedArray();

			CheckValue(CustomStruct.FullTypeSize + 76, CustomStruct.FullTypeSize + 76, value, result, new ListComparer());
		}

		[Test]
		public void CheckCustomStructTypedArrayMulti()
		{
			var value = new[] { new CustomStruct(), new CustomStruct() };
			value[0].IntValue = 3;
			value[0].StringValue = "a";
			value[1].IntValue = 4;
			value[1].StringValue = "b";
			Writer.WriteTypedArray(value);
			var result = (CustomStruct[]) Reader.ReadTypedArray();

			CheckValue(CustomStruct.FullTypeSize * 2 + 148, CustomStruct.FullTypeSize * 2 + 148, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomStructTypedArrayNull()
		{
			IntelligentCustomStruct[] value = null;
			Writer.WriteTypedArray(value);
			var result = (IntelligentCustomStruct[]) Reader.ReadTypedArray();

			CheckValue(1, 1, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomStructTypedArrayEmpty()
		{
			var value = new IntelligentCustomStruct[0];
			Writer.WriteTypedArray(value);
			var result = (IntelligentCustomStruct[]) Reader.ReadTypedArray();

			CheckValue(3, 3, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomStructTypedArrayOne()
		{
			var value = new[] { new IntelligentCustomStruct() };
			value[0].IntValue = 3;
			value[0].StringValue = "a";
			Writer.WriteTypedArray(value);
			var result = (IntelligentCustomStruct[]) Reader.ReadTypedArray();

			CheckValue(7, 7, value, result, new ListComparer());
		}

		[Test]
		public void CheckIntelligentCustomStructTypedArrayMulti()
		{
			var value = new[] { new IntelligentCustomStruct(), new IntelligentCustomStruct() };
			value[0].IntValue = 3;
			value[0].StringValue = "a";
			value[1].IntValue = 4;
			value[1].StringValue = "b";
			Writer.WriteTypedArray(value);
			var result = (IntelligentCustomStruct[]) Reader.ReadTypedArray();

			CheckValue(10, 10, value, result, new ListComparer());
		}
		
		static CustomClass CreateCustomClass()
		{
			var result = new CustomClass { IntValue = 10, StringValue = "abc" };
			return result;
		}
		
		static SemiIntelligentCustomClass CreateSemiIntelligentCustomClass()
		{
			var result = new SemiIntelligentCustomClass { IntValue = 10, StringValue = "abc" };
			return result;
		}
		
		static IntelligentCustomClass CreateIntelligentCustomClass()
		{
			var result = new IntelligentCustomClass { IntValue = 10, StringValue = "abc" };
			return result;
		}
		
		[Test]
		public void CheckCustomClass()
		{
			var value = CreateCustomClass();
			Writer.WriteObject(value);
			CheckValue(CustomClass.FullTypeSize + 74, -1, value, Reader.ReadObject(), Comparer.DefaultInvariant);
		}
		
		[Test]
		public void CheckCustomClassNull()
		{
			CustomClass value = null;
			Writer.WriteObject(value);
			CheckValue(1, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckSemiIntelligentCustomClass()
		{
			var value = CreateSemiIntelligentCustomClass();
			Writer.WriteObject(value);
			CheckValue(SemiIntelligentCustomClass.FullTypeSize + 74, -1, value, Reader.ReadObject(), Comparer.DefaultInvariant);
		}
		
		[Test]
		public void CheckSemiIntelligentCustomClassNull()
		{
			SemiIntelligentCustomClass value = null;
			Writer.WriteObject(value);
			CheckValue(1, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckIntelligentCustomClass()
		{
			var value = CreateIntelligentCustomClass();
			Writer.WriteObject(value);
			CheckValue(6, -1, value, Reader.ReadObject(), Comparer.DefaultInvariant);
		}
		
		[Test]
		public void CheckIntelligentCustomClassNull()
		{
			IntelligentCustomClass value = null;
			Writer.WriteObject(value);
			CheckValue(1, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckDecimalOptimizedBug()
		{
			var value = new Decimal(0, -1, -1, false, 0);
			Writer.WriteOptimized(value);
			CheckValue(9, 10, value, Reader.ReadOptimizedDecimal());
		}

		[Test]
		public void CheckInt32EnumObjectMin()
		{
			object value = Int32Enum.Min;
			Writer.WriteObject(value);
			CheckValue(7, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckInt32EnumObjectLargeNegative()
		{
			object value = Int32Enum.LargeNegative;
			Writer.WriteObject(value);
			CheckValue(7, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckInt32EnumObjectSmallNegative()
		{
			object value = Int32Enum.SmallNegative;
			Writer.WriteObject(value);
			CheckValue(7, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckInt32EnumObjectZero()
		{
			object value = Int32Enum.Zero;
			Writer.WriteObject(value);
			CheckValue(4, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckInt32EnumObjectSmallPositive()
		{
			object value = Int32Enum.SmallPositive;
			Writer.WriteObject(value);
			CheckValue(4, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckInt32EnumObjectMediumPositive()
		{
			object value = Int32Enum.MediumPositive;
			Writer.WriteObject(value);
			CheckValue(5, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckInt32EnumObjectLargePositive()
		{
			object value = Int32Enum.LargePositive;
			Writer.WriteObject(value);
			CheckValue(7, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckInt32EnumObjectMax()
		{
			object value = Int32Enum.Max;
			Writer.WriteObject(value);
			CheckValue(7, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckInt16EnumObjectMin()
		{
			object value = Int16Enum.Min;
			Writer.WriteObject(value);
			CheckValue(5, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckInt16EnumObjectLargeNegative()
		{
			object value = Int16Enum.LargeNegative;
			Writer.WriteObject(value);
			CheckValue(5, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckInt16EnumObjectSmallNegative()
		{
			object value = Int16Enum.SmallNegative;
			Writer.WriteObject(value);
			CheckValue(5, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckInt16EnumObjectZero()
		{
			object value = Int16Enum.Zero;
			Writer.WriteObject(value);
			CheckValue(5, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckInt16EnumObjectSmallPositive()
		{
			object value = Int16Enum.SmallPositive;
			Writer.WriteObject(value);
			CheckValue(5, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckInt16EnumObjectMediumPositive()
		{
			object value = Int16Enum.MediumPositive;
			Writer.WriteObject(value);
			CheckValue(5, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckInt16EnumObjectLargePositive()
		{
			object value = Int16Enum.LargePositive;
			Writer.WriteObject(value);
			CheckValue(5, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckInt16EnumObjectMax()
		{
			object value = Int16Enum.Max;
			Writer.WriteObject(value);
			CheckValue(5, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckInt64EnumObjectMin()
		{
			object value = Int64Enum.Min;
			Writer.WriteObject(value);
			CheckValue(11, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckInt64EnumObjectLargeNegative()
		{
			object value = Int64Enum.LargeNegative;
			Writer.WriteObject(value);
			CheckValue(11, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckInt64EnumObjectSmallNegative()
		{
			object value = Int64Enum.SmallNegative;
			Writer.WriteObject(value);
			CheckValue(11, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckInt64EnumObjectZero()
		{
			object value = Int64Enum.Zero;
			Writer.WriteObject(value);
			CheckValue(4, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckInt64EnumObjectSmallPositive()
		{
			object value = Int64Enum.SmallPositive;
			Writer.WriteObject(value);
			CheckValue(4, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckInt64EnumObjectMediumPositive()
		{
			object value = Int64Enum.MediumPositive;
			Writer.WriteObject(value);
			CheckValue(5, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckInt64EnumObjectLargePositive()
		{
			object value = Int64Enum.LargePositive;
			Writer.WriteObject(value);
			CheckValue(11, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckInt64EnumObjectMax()
		{
			object value = Int64Enum.Max;
			Writer.WriteObject(value);
			CheckValue(11, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckSByteEnumObjectMin()
		{
			object value = SByteEnum.Min;
			Writer.WriteObject(value);
			CheckValue(4, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckSByteEnumObjectLargeNegative()
		{
			object value = SByteEnum.LargeNegative;
			Writer.WriteObject(value);
			CheckValue(4, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckSByteEnumObjectSmallNegative()
		{
			object value = SByteEnum.SmallNegative;
			Writer.WriteObject(value);
			CheckValue(4, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckSByteEnumObjectZero()
		{
			object value = SByteEnum.Zero;
			Writer.WriteObject(value);
			CheckValue(4, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckSByteEnumObjectSmallPositive()
		{
			object value = SByteEnum.SmallPositive;
			Writer.WriteObject(value);
			CheckValue(4, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckSByteEnumObjectMediumPositive()
		{
			object value = SByteEnum.MediumPositive;
			Writer.WriteObject(value);
			CheckValue(4, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckSByteEnumObjectLargePositive()
		{
			object value = SByteEnum.LargePositive;
			Writer.WriteObject(value);
			CheckValue(4, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckSByteEnumObjectMax()
		{
			object value = SByteEnum.Max;
			Writer.WriteObject(value);
			CheckValue(4, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckByteEnumObjectZero()
		{
			object value = ByteEnum.Zero;
			Writer.WriteObject(value);
			CheckValue(4, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckByteEnumObjectSmallPositive()
		{
			object value = ByteEnum.SmallPositive;
			Writer.WriteObject(value);
			CheckValue(4, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckByteEnumObjectMediumPositive()
		{
			object value = ByteEnum.MediumPositive;
			Writer.WriteObject(value);
			CheckValue(4, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckByteEnumObjectLargePositive()
		{
			object value = ByteEnum.LargePositive;
			Writer.WriteObject(value);
			CheckValue(4, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckByteEnumObjectMax()
		{
			object value = ByteEnum.Max;
			Writer.WriteObject(value);
			CheckValue(4, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckUInt16EnumObjectZero()
		{
			object value = UInt16Enum.Zero;
			Writer.WriteObject(value);
			CheckValue(5, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckUInt16EnumObjectSmallPositive()
		{
			object value = UInt16Enum.SmallPositive;
			Writer.WriteObject(value);
			CheckValue(5, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckUInt16EnumObjectMediumPositive()
		{
			object value = UInt16Enum.MediumPositive;
			Writer.WriteObject(value);
			CheckValue(5, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckUInt16EnumObjectLargePositive()
		{
			object value = UInt16Enum.LargePositive;
			Writer.WriteObject(value);
			CheckValue(5, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckUInt16EnumObjectMax()
		{
			object value = UInt16Enum.Max;
			Writer.WriteObject(value);
			CheckValue(5, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckUInt32EnumObjectZero()
		{
			object value = UInt32Enum.Zero;
			Writer.WriteObject(value);
			CheckValue(4, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckUInt32EnumObjectSmallPositive()
		{
			object value = UInt32Enum.SmallPositive;
			Writer.WriteObject(value);
			CheckValue(4, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckUInt32EnumObjectMediumPositive()
		{
			object value = UInt32Enum.MediumPositive;
			Writer.WriteObject(value);
			CheckValue(5, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckUInt32EnumObjectLargePositive()
		{
			object value = UInt32Enum.LargePositive;
			Writer.WriteObject(value);
			CheckValue(7, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckUInt32EnumObjectMax()
		{
			object value = UInt32Enum.Max;
			Writer.WriteObject(value);
			CheckValue(7, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckUInt64EnumObjectZero()
		{
			object value = UInt64Enum.Zero;
			Writer.WriteObject(value);
			CheckValue(4, -1, value, Reader.ReadObject());
		}
		
		[Test]
		public void CheckUInt64EnumObjectSmallPositive()
		{
			object value = UInt64Enum.SmallPositive;
			Writer.WriteObject(value);
			CheckValue(4, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckUInt64EnumObjectMediumPositive()
		{
			object value = UInt64Enum.MediumPositive;
			Writer.WriteObject(value);
			CheckValue(5, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckUInt64EnumObjectLargePositive()
		{
			object value = UInt64Enum.LargePositive;
			Writer.WriteObject(value);
			CheckValue(11, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckUInt64EnumObjectMax()
		{
			object value = UInt64Enum.Max;
			Writer.WriteObject(value);
			CheckValue(11, -1, value, Reader.ReadObject());
		}

		[Test]
		public void CheckNonZeroStartOffset()
		{
			const long ArbitraryStreamLength = 1000;
			const long ArbitraryStartPosition = 10;
			const string SampleString = "Hello";

			var stream = new MemoryStream { Position = ArbitraryStartPosition };
			stream.SetLength(ArbitraryStreamLength);

			var directWriter = new SerializationWriter(stream);
			Assert.AreEqual(ArbitraryStartPosition + FullHeaderSize, stream.Position);
			directWriter.Write(SampleString);
			Assert.AreEqual(ArbitraryStartPosition + FullHeaderSize + 1 + 1 + SampleString.Length + 1, stream.Position);
			directWriter.WriteOptimized(123);
			Assert.AreEqual(ArbitraryStartPosition + FullHeaderSize + 1 + 1 + SampleString.Length + 1 + 1, stream.Position);
			Assert.AreEqual(ArbitraryStreamLength, stream.Length);

			var totalLength = directWriter.UpdateHeader();
			Assert.AreEqual(ArbitraryStartPosition + FullHeaderSize + 1 + 1 + SampleString.Length + 1 + 1, stream.Position);
			Assert.AreEqual(stream.Position - ArbitraryStartPosition, totalLength);

			stream.Position = ArbitraryStartPosition;

			var directReader = new SerializationReader(stream);
			Assert.AreEqual(1, GetStringTableSize(directReader));
			Assert.AreEqual(0, GetObjectTableSize(directReader));
			Assert.AreEqual(SampleString, directReader.ReadString());
			Assert.AreEqual(123, directReader.ReadOptimizedInt32());
			Assert.AreEqual(ArbitraryStartPosition + FullHeaderSize + 1 + 1 + SampleString.Length + 1 + 1, stream.Position);
			Assert.AreEqual(ArbitraryStreamLength, stream.Length);
			Assert.AreEqual(0, directReader.BytesRemaining);
		}

		[Test]
		public void CheckEmptyWriterReturnsJustHeader()
		{
			Assert.AreEqual(FullHeaderSize, new SerializationWriter().UpdateHeader());
			Assert.AreEqual(FullHeaderSize, new SerializationWriter().ToArray().Length);

			Assert.AreEqual(FullHeaderSize, new SerializationWriter(1000).UpdateHeader());
			Assert.AreEqual(FullHeaderSize, new SerializationWriter(1000).ToArray().Length);

			Assert.AreEqual(FullHeaderSize, new SerializationWriter(new MemoryStream()).UpdateHeader());
			Assert.AreEqual(FullHeaderSize, new SerializationWriter(new MemoryStream()).ToArray().Length);

			Assert.AreEqual(MinHeaderSize, new SerializationWriter(new MemoryStream(), false).UpdateHeader());
			Assert.AreEqual(MinHeaderSize, new SerializationWriter(new MemoryStream(), false).ToArray().Length);
		}

		[Test]
		public void CheckToArrayWithNonZeroStartPosition()
		{
			Assert.AreEqual(FullHeaderSize, new SerializationWriter(new MemoryStream(1000) { Position = 50 }).ToArray().Length);
		}

		[Test]
		public void CheckToArrayWithNonSeekableStream()
		{
			Assert.AreEqual(0, new SerializationWriter(new NonSeekableMemoryStream(), true).UpdateHeader());
			Assert.AreEqual(MinHeaderSize, new SerializationWriter(new NonSeekableMemoryStream(), true).ToArray().Length);
		}

		[Test]
		public void CheckSpecificPresizingOverridesHeader()
		{
			writer.UpdateHeader();
			writer.BaseStream.Position = 0;

			var directReader = new SerializationReader(writer.BaseStream, 10, 5);
			Assert.AreEqual(10, GetStringTableSize(directReader));
			Assert.AreEqual(5, GetObjectTableSize(directReader));
		}

		[Test]
		public void CheckSerializationWriterCanUseNonSeekableStream()
		{
			var directWriter = new SerializationWriter(new NonSeekableMemoryStream());
			directWriter.Write("Hello");
		}

		[Test]
		public void CheckManualUpdateHeaderAndPassToSerializationReader()
		{
			const string SampleString = "Hello";

			var stream = new NonSeekableMemoryStream();

			var directWriter = new SerializationWriter(stream, true);
			directWriter.Write(SampleString);
			directWriter.WriteOptimized(123);

			var totalLength = directWriter.UpdateHeader();
			Assert.AreEqual(0, totalLength);

			var stringTableSize = directWriter.StringTokenTableSize;
			Assert.AreEqual(1, stringTableSize);
			var objectTableSize = directWriter.ObjectTokenTableSize;
			Assert.AreEqual(0, objectTableSize);

			stream.SetPosition(0);

			var directReader = new SerializationReader(stream, stringTableSize, objectTableSize);
			Assert.AreEqual(stringTableSize, GetStringTableSize(directReader));
			Assert.AreEqual(objectTableSize, GetObjectTableSize(directReader));
			Assert.AreEqual(SampleString, directReader.ReadString());
			Assert.AreEqual(123, directReader.ReadOptimizedInt32());
			Assert.AreEqual(-1, directReader.BytesRemaining);
		}

		static int GetStringTableSize(SerializationReader reader)
		{
			var list = (List<string>) typeof(SerializationReader).GetField("stringTokenList", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(reader);
			var items = (string[]) typeof(List<string>).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(list);

			return items.Length;
		}

		static int GetObjectTableSize(SerializationReader reader)
		{
			var list = (List<object>) typeof(SerializationReader).GetField("objectTokenList", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(reader);
			var items = (object[]) typeof(List<object>).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(list);

			return items.Length;
		}
	}

	public class NonSeekableMemoryStream: MemoryStream
	{
		public override bool CanSeek
		{
			get { return false; }
		}

		public override long Position
		{
			get { throw new InvalidOperationException("Cannot get Position on a non-seekable stream"); }
			set { throw new InvalidOperationException("Cannot set Position on a non-seekable stream"); }
		}

		public void SetPosition(long position)
		{
			base.Position = position;
		}
	}

	public enum ByteEnum: byte
	{
		Zero = 0,
		SmallPositive = 1,
		MediumPositive = 200,
		LargePositive = byte.MaxValue - 1,
		Max = byte.MaxValue
	}

	[CLSCompliant(false)]
	public enum SByteEnum: sbyte
	{
		Min = sbyte.MinValue,
		LargeNegative = sbyte.MinValue + 1,
		SmallNegative = -1,
		Zero = 0,
		SmallPositive = 1,
		MediumPositive = 67,
		LargePositive = sbyte.MaxValue - 1,
		Max = sbyte.MaxValue
	}

	public enum Int16Enum: short
	{
		Min = short.MinValue,
		LargeNegative = short.MinValue + 1,
		SmallNegative = -1,
		Zero = 0,
		SmallPositive = 1,
		MediumPositive = 500,
		LargePositive = short.MaxValue - 1,
		Max = short.MaxValue
	}

	[CLSCompliant(false)]
	public enum UInt16Enum: ushort
	{
		Zero = 0,
		SmallPositive = 1,
		MediumPositive = 500,
		LargePositive = ushort.MaxValue - 1,
		Max = ushort.MaxValue
	}

	public enum Int32Enum
	{
		Min = int.MinValue,
		LargeNegative = int.MinValue + 1,
		SmallNegative = -1,
		Zero = 0,
		SmallPositive = 1,
		MediumPositive = 500,
		LargePositive = int.MaxValue - 1,
		Max = int.MaxValue
	}

	[CLSCompliant(false)]
	public enum UInt32Enum: uint
	{
		Zero = 0,
		SmallPositive = 1,
		MediumPositive = 500,
		LargePositive = uint.MaxValue - 1,
		Max = uint.MaxValue
	}

	public enum Int64Enum: long
	{
		Min = long.MinValue,
		LargeNegative = long.MinValue + 1,
		SmallNegative = -1,
		Zero = 0,
		SmallPositive = 1,
		MediumPositive = 500,
		LargePositive = long.MaxValue - 1,
		Max = long.MaxValue
	}

	[CLSCompliant(false)]
	public enum UInt64Enum: ulong
	{
		Zero = 0,
		SmallPositive = 1,
		MediumPositive = 500,
		LargePositive = ulong.MaxValue - 1,
		Max = ulong.MaxValue
	}


	public class ListComparer<T>: IComparer {
		public int Compare(object x, object y)
		{
			var list1 = (List<T>) x;
			var list2 = (List<T>) y;
			
			var result = list1.Count.CompareTo(list2.Count);
			if (result == 0)
			{
				IComparer<T> comparer = Comparer<T>.Default;
				for (var i = 0; i < list1.Count && result == 0; i++)
				{
					result = comparer.Compare(list1[i], list2[i]);
				}
			}
			return result;
		}
	}

	public class DictionaryComparer<K, V>: IComparer
	{

		public int Compare(object x, object y)
	  {
	    var dictionary1 = (Dictionary<K, V>) x;
	    var dictionary2 = (Dictionary<K, V>) y;
	    var result = dictionary1.Count.CompareTo(dictionary2.Count);
	    if (result == 0)
	    {
				var keys1 = new List<K>(dictionary1.Keys);
				var keys2 = new List<K>(dictionary2.Keys);
				result = new ListComparer<K>().Compare(keys1, keys2);

				if (result == 0)
				{
					var values1 = new List<V>(dictionary1.Values);
					var values2 = new List<V>(dictionary2.Values);
					result = new ListComparer<V>().Compare(values1, values2);
				}
	    }
	    return result;
	  }
	}
	
	public class BitArrayComparer: IComparer
	{
		public int Compare(object x, object y)
		{
			var bitArray1 = (BitArray) x;
			var bitArray2 = (BitArray) y;
			var result = bitArray1.Length.CompareTo(bitArray2.Length);
			if (result == 0)
			{
				for(var i = 0; i < bitArray1.Length; i++)
				{
					result = bitArray1[i].CompareTo(bitArray2[i]);
					if (result != 0) break;
				}
			}
			return result;
		}
	}

	public class ListComparer: IComparer
	{
		public int Compare(object x, object y)
		{
			if (x == null) return y == null ? 0 : -1;

			var list1 = (IList) x;
			var list2 = (IList) y;
			if (x.GetType() != y.GetType()) throw new InvalidOperationException(string.Format("Not the same types: x={0}, y={1}", x.GetType(), y.GetType()));

			var result = list1.Count.CompareTo(list2.Count);
			if (result == 0)
			{
				for(var i = 0; i < list1.Count; i++)
				{
					if (list1[i] == null) return list2[i] == null ? 0 : -1;

					result = list1[i].GetType().ToString().CompareTo(list2[i].GetType().ToString());
					if (result != 0) break;

					var left = list1[i] as IComparable;
					if (left != null)
					{
						result = left.CompareTo(list2[i]);
					}
					else
					{
						result = list1[i] == list2[i] ? 0 : -1;
					}
					if (result != 0) break;
				}
			}
			return result;
		}
	}

	#region Sample Classes
	[Serializable]
	public class CustomClass: IComparable
	{
		public static readonly int FullTypeSize = typeof(CustomClass).AssemblyQualifiedName.Length;
		
		public int IntValue = 55;
		public string StringValue = "fred";
		
		public CustomClass() {}
		public CustomClass(int intValue, string stringValue)
		{
			IntValue = intValue;
			StringValue = stringValue;
		}

		#region IComparable Members
		public virtual int CompareTo(object obj)
		{
			var compare = obj as CustomClass;
			if (compare == null) throw new ArgumentException();
			var result = IntValue.CompareTo(compare.IntValue);
			if (result == 0) result = StringValue.CompareTo(compare.StringValue);
			return result;
		}
		#endregion
	}

	[Serializable]
	public class InheritedCustomClass: CustomClass
	{
		public static new readonly int FullTypeSize = typeof(InheritedCustomClass).AssemblyQualifiedName.Length;

		public float FloatValue = 1023.232f;

		#region IComparable Members
		public override int CompareTo(object obj)
		{
			var compare = obj as InheritedCustomClass;
			if (compare == null) throw new ArgumentException();
			var result = IntValue.CompareTo(compare.IntValue);
			if (result == 0) result = StringValue.CompareTo(compare.StringValue);
			if (result == 0) result = FloatValue.CompareTo(compare.FloatValue);
			return result;
		}
		#endregion
	}

	[Serializable]
	public class SemiIntelligentCustomClass : IComparable, IOwnedDataSerializable
	{
		public static readonly int FullTypeSize = typeof(SemiIntelligentCustomClass).AssemblyQualifiedName.Length;

		public int IntValue = 55;
		public string StringValue = "fred";

		#region IComparable Members
		public virtual int CompareTo(object obj)
		{
			var compare = obj as SemiIntelligentCustomClass;
			if (compare == null) throw new ArgumentException();
			var result = IntValue.CompareTo(compare.IntValue);
			if (result == 0) result = StringValue.CompareTo(compare.StringValue);
			return result;
		}
		#endregion

		#region IOwnedDataSerializable Members
		public virtual void SerializeOwnedData(SerializationWriter writer, object context)
		{
			writer.WriteOptimized(IntValue);
			writer.WriteOptimized(StringValue);
		}

		public virtual void DeserializeOwnedData(SerializationReader reader, object context)
		{
			IntValue = reader.ReadOptimizedInt32();
			StringValue = reader.ReadOptimizedString();
		}
		#endregion
	}

	[Serializable]
	public class InheritedSemiIntelligentCustomClass : SemiIntelligentCustomClass
	{
		public static new readonly int FullTypeSize = typeof(InheritedSemiIntelligentCustomClass).AssemblyQualifiedName.Length;

		public float FloatValue = 1023.232f;

		#region IComparable Members
		public override int CompareTo(object obj)
		{
			var compare = obj as InheritedSemiIntelligentCustomClass;
			if (compare == null) throw new ArgumentException();
			var result = base.CompareTo(obj);
			if (result == 0) result = FloatValue.CompareTo(compare.FloatValue);
			return result;
		}
		#endregion

		#region IOwnedDataSerializable Members
		public override void SerializeOwnedData(SerializationWriter writer, object context)
		{
			base.SerializeOwnedData(writer, context);
			writer.Write(FloatValue);
		}

		public override void DeserializeOwnedData(SerializationReader reader, object context)
		{
			base.DeserializeOwnedData(reader, context);
			FloatValue = reader.ReadSingle();
		}
		#endregion

	}
	
	
	[Serializable]
	public class IntelligentCustomClass: IComparable, IOwnedDataSerializableAndRecreatable
	{
		public static readonly int FullTypeSize = typeof(IntelligentCustomClass).AssemblyQualifiedName.Length;

		public int IntValue = 55;
		public string StringValue = "fred";
		
		public IntelligentCustomClass() {}
		public IntelligentCustomClass(int intValue, string stringValue)
		{
			IntValue = intValue;
			StringValue = stringValue;
		}

		#region IComparable Members
		public virtual int CompareTo(object obj)
		{
			var compare = obj as IntelligentCustomClass;
			if (compare == null) throw new ArgumentException();
			var result = IntValue.CompareTo(compare.IntValue);
			if (result == 0) result = StringValue.CompareTo(compare.StringValue);
			return result;
		}
		#endregion

		#region IOwnedDataSerializable Members
		public virtual void SerializeOwnedData(SerializationWriter writer, object context)
		{
			writer.WriteOptimized(IntValue);
			writer.WriteOptimized(StringValue);
		}

		public virtual void DeserializeOwnedData(SerializationReader reader, object context)
		{
			IntValue = reader.ReadOptimizedInt32();
			StringValue = reader.ReadOptimizedString();
		}
		#endregion
	}

	[Serializable]
	public class InheritedIntelligentCustomClass : IntelligentCustomClass
	{
		public static new readonly int FullTypeSize = typeof(InheritedIntelligentCustomClass).AssemblyQualifiedName.Length;

		public float FloatValue = 1023.232f;

		#region IComparable Members
		public override int CompareTo(object obj)
		{
			var compare = obj as InheritedIntelligentCustomClass;
			if (compare == null) throw new ArgumentException();
			var result = base.CompareTo(obj);
			if (result == 0) result = FloatValue.CompareTo(compare.FloatValue);
			return result;
		}
		#endregion

		#region IOwnedDataSerializable Members
		public override void SerializeOwnedData(SerializationWriter writer, object context)
		{
			base.SerializeOwnedData(writer, context);
			writer.Write(FloatValue);
		}

		public override void DeserializeOwnedData(SerializationReader reader, object context)
		{
			base.DeserializeOwnedData(reader, context);
			FloatValue = reader.ReadSingle();
		}
		#endregion

	}
	
	[Serializable]
	public struct CustomStruct: IComparable
	{
		public static readonly int FullTypeSize = typeof(CustomStruct).AssemblyQualifiedName.Length;

		public int IntValue;
		public string StringValue;

		public CustomStruct(int intValue, string stringValue)
		{
			IntValue = intValue;
			StringValue = stringValue;
		}
		
		#region IComparable Members
		int IComparable.CompareTo(object obj)
		{
			var compare = (CustomStruct)obj;
			var result = IntValue.CompareTo(compare.IntValue);
			if (result == 0) result = StringValue.CompareTo(compare.StringValue);
			return result;
		}
		#endregion
	}

	[Serializable]
	public struct IntelligentCustomStruct: IOwnedDataSerializable, IComparable
	{
		public static readonly int FullTypeSize = typeof(IntelligentCustomStruct).AssemblyQualifiedName.Length;

		public int IntValue;
		public string StringValue;
		
		public IntelligentCustomStruct(int intValue, string stringValue)
		{
			IntValue = intValue;
			StringValue = stringValue;
		}

		#region IOwnedDataSerializable Members
		void IOwnedDataSerializable.SerializeOwnedData(SerializationWriter writer, object context)
		{
			writer.WriteOptimized(IntValue);
			writer.WriteOptimized(StringValue);
		}

		void IOwnedDataSerializable.DeserializeOwnedData(SerializationReader reader, object context)
		{
			IntValue = reader.ReadOptimizedInt32();
			StringValue = reader.ReadOptimizedString();
		}
		#endregion

		#region IComparable Members
		int IComparable.CompareTo(object obj)
		{
			var compare = (IntelligentCustomStruct)obj;
			var result = IntValue.CompareTo(compare.IntValue);
			if (result == 0) result = StringValue.CompareTo(compare.StringValue);
			return result;
		}
		#endregion

	}

	[Serializable]
	public class SampleFactoryClass {}
	#endregion Sample Classes
}
#endif