#if DEBUG
using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;

using NUnit.Framework;

// ReSharper disable ConvertToConstant.Local

namespace Framework.Serialization
{
	
	[TestFixture]
	public class WebFastSerializationHelperTests
	{
		const int HeaderSize = 12;

		SerializationReader reader;
		SerializationWriter writer;
		int writerPosition;
		long writerLength;

		[SetUp]
		public void CreateWriter()
		{
			writer = new SerializationWriter();
			reader = null;
			writerPosition = 0;
			writerLength = -1;
		}

		SerializationWriter Writer
		{
			get { return writer; }
		}

		[Browsable(false)]
		public SerializationReader Reader
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

		void CheckValue(int expectedSize, int expectedSurrogateHandledSize, object value)
		{
			CheckValue(expectedSize, expectedSurrogateHandledSize, value, null);
		}

		void CreateWriterNoSurrogates()
		{
			SerializationWriter.TypeSurrogates.Clear();
			CreateWriter();
		}

		void CreateSurrogateWriter()
		{
			SerializationWriter.TypeSurrogates.Add(new WebFastSerializationHelper());
			CreateWriter();
		}

		void CheckValue(int expectedSize, int expectedSurrogateHandledSize, object value, IComparer comparer)
		{
			try
			{
				var losFormatter = new LosFormatter();

				using(var temp = new MemoryStream())
				{
					losFormatter.Serialize(temp, value);
					Console.WriteLine("MemoryStream size: LosFormatter = " + temp.Position);
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine("Not formattable by LosFormatter: " + ex.Message);
			}
			CreateWriterNoSurrogates();
			if (expectedSize != -2) CheckValue(expectedSize, value, comparer);

			if (expectedSurrogateHandledSize != -1)
			{
				CreateSurrogateWriter();
				CheckValue(expectedSurrogateHandledSize, value, comparer);
			}
		}

		void CheckValue(int expectedSize, object value, IComparer comparer)
		{
			var writerType = SerializationWriter.TypeSurrogates.Count == 0 ? "No surrogate" : "With surrogate";
			Writer.WriteObject(value);
			var newValue = Reader.ReadObject();

			Assert.IsTrue((value == null && newValue == null) || (value != null && newValue != null), "Incorrect null/not null: " + writerType);

			if (expectedSize != -1)
			{
				var writerActualSize = writerPosition - HeaderSize - Writer.TableBytes;
				var readerActualSize = Reader.BaseStream.Position - HeaderSize - Writer.TableBytes;
				Assert.AreEqual(expectedSize, writerActualSize, "Incorrect length on WriteObject/" + writerType);
				Assert.AreEqual(expectedSize, readerActualSize, "Incorrect length on ReadObject/" + writerType);
			}
			if (value != null) Assert.AreSame(value.GetType(), newValue.GetType());

			if (comparer == null)
				Assert.AreEqual(value, newValue, "Comparison failed: " + writerType);
			else
			{
				Assert.AreEqual(0, comparer.Compare(value, newValue), "IComparer comparison failed: " + writerType);
			}

			Console.WriteLine("MemoryStream size: " + writerType + "= " + Writer.BaseStream.Length);
		}

		SerializationReader GetReaderFromWriter(SerializationWriter serializationWriter)
		{
			var data = serializationWriter.ToArray();

			if (writerLength == -1)
			{
				writerLength = data.Length;
			}

			return new SerializationReader(data);
		}

		[Test]
		public void CheckPair()
		{
			var value = new Pair(0, 0);
			CheckValue(161, 5, value, new PairComparer());
		}

		[Test]
		public void CheckPairArray()
		{
			var value = new Pair[100];
			for(var i = 0; i < value.Length; i++)
			{
				value[i] = new Pair(0, 0);
			}
			CheckValue(16104, 504, value, new PairArrayComparer());
		}

		[Test]
		public void CheckPairNull()
		{
			var value = new Pair(null, null);
			CheckValue(151, 5, value, new PairComparer());
		}

		[Test]
		public void CheckPairArrayNull()
		{
			var value = new Pair[100];
			for(var i = 0; i < value.Length; i++)
			{
				value[i] = new Pair(null, null);
			}
			CheckValue(15104, 504, value, new PairArrayComparer());
		}

		[Test]
		public void CheckTriplet()
		{
			var value = new Triplet(0, 0);
			CheckValue(172, 6, value, new TripletComparer());
		}

		[Test]
		public void CheckTripletArray()
		{
			var value = new Triplet[100];
			for(var i = 0; i < value.Length; i++)
			{
				value[i] = new Triplet(0, 0);
			}
			CheckValue(17204, 604, value, new TripletArrayComparer());
		}

		[Test]
		public void CheckTripletNull()
		{
			var value = new Triplet(null, null);
			CheckValue(162, 6, value, new TripletComparer());
		}

		[Test]
		public void CheckTripletArrayNull()
		{
			var value = new Triplet[100];
			for(var i = 0; i < value.Length; i++)
			{
				value[i] = new Triplet(null, null);
			}
			CheckValue(16204, 604, value, new TripletArrayComparer());
		}

		[Test]
		public void CheckStateBag()
		{
			var value = new StateBag
			            	{
			            		{ "First", null },
			            		{ "Second", 87878 },
			            		{ "Third", new Triplet("Fred", 7123m, new DateTime(2007, 02, 04, 9, 54, 0)) }
			            	};
			CheckValue(-2, 28, value, new StateBagComparer());
		}

		[Test]
		public void CheckStateBagLarge()
		{
			var value = new StateBag();
			for(var i = 0; i < 1000; i++)
			{
				value.Add(i.ToString(), new Triplet(i, i.ToString(), i * i));
			}
			CheckValue(-2, 13734, value, new StateBagComparer());
		}

		[Test]
		public void CheckUnitPixelZero()
		{
			var value = Unit.Pixel(0);
			CheckValue(269, 3 + 1, value);
		}

		[Test]
		public void CheckUnitPixelOptimizablePositive()
		{
			var value = Unit.Pixel(1);
			CheckValue(269, 3 + 1 + 1, value);
		}

		[Test]
		public void CheckUnitPixelOptimizableNegative()
		{
			var value = Unit.Pixel(-1);
			CheckValue(269, 3 + 1 + 1, value);
		}

		[Test]
		public void CheckUnitPixelNotOptimizablePositive()
		{
			var value = Unit.Pixel(200);
			CheckValue(269, 3 + 1 + 2, value);
		}

		[Test]
		public void CheckUnitPixelNotOptimizableNegative()
		{
			var value = Unit.Pixel(-200);
			CheckValue(269, 3 + 1 + 2, value);
		}

		[Test]
		public void CheckUnitPixelNotOptimizableMinValue()
		{
			var value = Unit.Pixel(short.MinValue);
			CheckValue(269, 3 + 1 + 2, value);
		}

		[Test]
		public void CheckUnitPixelNotOptimizableMaxValue()
		{
			var value = Unit.Pixel(short.MaxValue);
			CheckValue(269, 3 + 1 + 2, value);
		}

		[Test]
		public void CheckUnitPointZero()
		{
			var value = Unit.Point(0);
			CheckValue(269, 3 + 1, value);
		}

		[Test]
		public void CheckUnitPointOptimizablePositive()
		{
			var value = Unit.Point(1);
			CheckValue(269, 3 + 1 + 1, value);
		}

		[Test]
		public void CheckUnitPointOptimizableNegative()
		{
			var value = Unit.Point(-1);
			CheckValue(269, 3 + 1 + 1, value);
		}

		[Test]
		public void CheckUnitPointNotOptimizablePositive()
		{
			var value = Unit.Point(200);
			CheckValue(269, 3 + 1 + 2, value);
		}

		[Test]
		public void CheckUnitPointNotOptimizableNegative()
		{
			var value = Unit.Point(-200);
			CheckValue(269, 3 + 1 + 2, value);
		}

		[Test]
		public void CheckUnitPointNotOptimizableMinValue()
		{
			var value = Unit.Point(short.MinValue);
			CheckValue(269, 3 + 1 + 2, value);
		}

		[Test]
		public void CheckUnitPointNotOptimizableMaxValue()
		{
			var value = Unit.Point(short.MaxValue);
			CheckValue(269, 3 + 1 + 2, value);
		}

		[Test]
		public void CheckUnitPointPositiveDoubleRoundDownToInteger()
		{
			var value = new Unit(9.0000000000000001, UnitType.Point);
			CheckValue(269, 3 + 1 + 1, value);
		}

		[Test]
		public void CheckUnitPointPositiveDoubleNoRoundDownToInteger()
		{
			var value = new Unit(9.000000000000001, UnitType.Point);
			CheckValue(269, 3 + 1 + 8, value);
		}

		[Test]
		public void CheckUnitPointPositiveDoubleRoundUpToInteger()
		{
			var value = new Unit(9.9999999999999999, UnitType.Point);
			CheckValue(269, 3 + 1 + 1, value);
		}

		[Test]
		public void CheckUnitPointPositiveDoubleNoRoundUpToInteger()
		{
			var value = new Unit(9.999999999999999, UnitType.Point);
			CheckValue(269, 3 + 1 + 8, value);
		}

		[Test]
		public void CheckUnitPointNegativeDoubleRoundDownToInteger()
		{
			var value = new Unit(-9.0000000000000001, UnitType.Point);
			CheckValue(269, 3 + 1 + 1, value);
		}

		[Test]
		public void CheckUnitPointNegativeDoubleNoRoundDownToInteger()
		{
			var value = new Unit(-9.000000000000001, UnitType.Point);
			CheckValue(269, 3 + 1 + 8, value);
		}

		[Test]
		public void CheckUnitPointNegativeDoubleRoundUpToInteger()
		{
			var value = new Unit(-9.9999999999999999, UnitType.Point);
			CheckValue(269, 3 + 1 + 1, value);
		}

		[Test]
		public void CheckUnitPointNegativeDoubleNoRoundUpToInteger()
		{
			var value = new Unit(-9.999999999999999, UnitType.Point);
			CheckValue(269, 3 + 1 + 8, value);
		}


		static bool IsDoubleNearlyInt(double value)
		{
			return Math.Abs(value - Math.Round(value)) < WebFastSerializationHelper.Epsilon;
		}

		[Test]
		public void CheckPositiveRoundUpFalse()
		{
			var value = 9.999999999999999;
			Assert.IsFalse(IsDoubleNearlyInt(value));
		}

		[Test]
		public void CheckPositiveRoundUpTrue()
		{
			var value = 9.9999999999999999;
			Assert.IsTrue(IsDoubleNearlyInt(value));
		}

		[Test]
		public void CheckNegativeRoundUpFalse()
		{
			var value = -9.999999999999999;
			Assert.IsFalse(IsDoubleNearlyInt(value));
		}

		[Test]
		public void CheckNegativeRoundUpTrue()
		{
			var value = -9.9999999999999999;
			Assert.IsTrue(IsDoubleNearlyInt(value));
		}

		[Test]
		public void CheckPositiveRoundDownFalse()
		{
		  var value = 9.000000000000001;
		  Assert.IsFalse(IsDoubleNearlyInt(value));
		}

		[Test]
		public void CheckPositiveRoundDownTrue()
		{
		  var value = 9.0000000000000001;
		  Assert.IsTrue(IsDoubleNearlyInt(value));
		}

		[Test]
		public void CheckNegativeRoundDownFalse()
		{
		  var value = -9.000000000000001;
		  Assert.IsFalse(IsDoubleNearlyInt(value));
		}

		[Test]
		public void CheckNegativeRoundDownTrue()
		{
		  var value = -9.0000000000000001;
		  Assert.IsTrue(IsDoubleNearlyInt(value));
		}

		[Test]
		public void CheckSimpleHashtable()
		{
			var value = new Hashtable();
			value[0] = "Zero";
			value[1] = "One";
			value[2] = "Two";

			CheckValue(287, 15, value, new SimpleHashtableComparer());
		}


	}

	internal class SimpleHashtableComparer: IComparer
	{
		public int Compare(object x, object y)
		{
			var left = (Hashtable) x;
			var right = (Hashtable) x;

			var result = left.Count.CompareTo(right.Count);
			if (result != 0) return result;

			foreach(var key in left.Keys)
			{
				var leftValue = left[key];
				var rightValue = right[key];

				result = Comparer.Default.Compare(leftValue.GetType(), rightValue.GetType());
				if (result != 0) return result;

				if (leftValue is IComparable)
				{
					result = Comparer.Default.Compare(leftValue, rightValue);
					if (result != 0) return result;
				}

			}

			return 0;
		}
	}

	public class StateBagComparer: IComparer
	{
		public int Compare(object x, object y)
		{
			var left = (StateBag) x;
			var right = (StateBag) y;

			var result = left.Count.CompareTo(right.Count);
			if (result != 0) return result;

			var ignoreCaseLeft = (bool) WebFastSerializationHelper.StateBagIgnoreCaseField.GetValue(left);
			var ignoreCaseRight = (bool) WebFastSerializationHelper.StateBagIgnoreCaseField.GetValue(right);
			result = ignoreCaseLeft.CompareTo(ignoreCaseRight);
			if (result != 0) return result;

			foreach(string key in left.Keys)
			{
				var leftValue = left[key];
				var rightValue = right[key];

				result = Comparer.Default.Compare(leftValue.GetType(), rightValue.GetType());
				if (result != 0) return result;

				if (leftValue is IComparable)
				{
					result = Comparer.Default.Compare(leftValue, rightValue);
					if (result != 0) return result;
				}

				result = left.IsItemDirty(key).CompareTo(right.IsItemDirty(key));
				if (result != 0) return result;
			}

			return 0;
		}
	}

	public class PairArrayComparer: IComparer
	{
		public int Compare(object x, object y)
		{
			var left = (Pair[]) x;
			var right = (Pair[]) y;
			var comparer = new PairComparer();
			var result = 0;

			for(var i = 0; i < left.Length; i++)
			{
				result = comparer.Compare(left[i], right[i]);
				if (result != 0) break;
			}
			return result;
		}
	}


	public class PairComparer: IComparer
	{
		public int Compare(object x, object y)
		{
			var left = (Pair) x;
			var right = (Pair) y;
			var result = Comparer.Default.Compare(left.First, right.First);
			if (result == 0) result = Comparer.Default.Compare(left.Second, right.Second);
			return result;
		}
	}

	public class TripletComparer: IComparer
	{
		public int Compare(object x, object y)
		{
			var left = (Triplet) x;
			var right = (Triplet) y;
			var result = Comparer.Default.Compare(left.First, right.First);
			if (result == 0) result = Comparer.Default.Compare(left.Second, right.Second);
			if (result == 0) result = Comparer.Default.Compare(left.Third, right.Third);
			return result;
		}
	}

	public class TripletArrayComparer: IComparer
	{
		public int Compare(object x, object y)
		{
			var left = (Triplet[]) x;
			var right = (Triplet[]) y;
			var comparer = new TripletComparer();
			var result = 0;

			for(var i = 0; i < left.Length; i++)
			{
				result = comparer.Compare(left[i], right[i]);
				if (result != 0) break;
			}
			return result;
		}
	}

}
#endif
