#if DEBUG
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;

using NUnit.Framework;

namespace Framework.Serialization
{
	
	[TestFixture]
	public class DrawingFastSerializationHelperTests
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
			SerializationWriter.TypeSurrogates.Add(new DrawingFastSerializationHelper());
			CreateWriter();
		}

		void CheckValue(int expectedSize, int expectedSurrogateHandledSize, object value, IComparer comparer)
		{
			CreateWriterNoSurrogates();

			CheckValue(expectedSize, value, comparer);

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

			if (value != null)
			{
				Assert.AreSame(value.GetType(), newValue.GetType());
			}

			if (comparer == null)
			{
				Assert.AreEqual(value, newValue, "Comparison failed: " + writerType);
			}
			else
			{
				Assert.AreEqual(0, comparer.Compare(value, newValue), "IComparer comparison failed: " + writerType);
			}

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
		public void CheckKnownColorRed()
		{
			var value = Color.Red;
			Assert.IsTrue(value.IsKnownColor);
			CheckValue(188, 6, value);
		}

		[Test]
		public void CheckKnownColorGreen()
		{
			var value = Color.Green;
			Assert.IsTrue(value.IsKnownColor);
			CheckValue(188, 5, value);
		}

		[Test]
		public void CheckKnownColorBlue()
		{
			var value = Color.Blue;
			Assert.IsTrue(value.IsKnownColor);
			CheckValue(188, 5, value);
		}

		[Test]
		public void CheckValueColorRed()
		{
			var value = Color.FromArgb(Color.Red.ToArgb());
			Assert.IsFalse(value.IsKnownColor);
			CheckValue(188, 6, value);
		}

		[Test]
		public void CheckValueColorGreen()
		{
			var value = Color.FromArgb(Color.Green.ToArgb());
			Assert.IsFalse(value.IsKnownColor);
			CheckValue(188, 6, value);
		}

		[Test]
		public void CheckValueColorBlue()
		{
			var value = Color.FromArgb(Color.Blue.ToArgb());
			Assert.IsFalse(value.IsKnownColor);
			CheckValue(188, 6, value);
		}

	}
}
#endif