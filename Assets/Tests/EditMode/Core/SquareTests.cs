using ModularChess.Core;
using NUnit.Framework;

namespace ModularChess.Core.Tests
{
    public class SquareTests
    {
        [Test]
        public void TryParse_e4_IsFile4Rank3()
        {
            Assert.IsTrue(Square.TryParse("e4", out Square square));
            Assert.AreEqual(4, square.File);
            Assert.AreEqual(3, square.Rank);
            Assert.IsTrue(square.IsOnBoard);
            Assert.AreEqual("e4", square.ToString());
        }

        [Test]
        public void TryParse_a1_IsOrigin()
        {
            Assert.IsTrue(Square.TryParse("a1", out Square square));
            Assert.AreEqual(0, square.File);
            Assert.AreEqual(0, square.Rank);
        }

        [Test]
        public void TryParse_h8_IsFarCorner()
        {
            Assert.IsTrue(Square.TryParse("h8", out Square square));
            Assert.AreEqual(7, square.File);
            Assert.AreEqual(7, square.Rank);
        }

        [Test]
        public void TryParse_RejectsInvalid()
        {
            Assert.IsFalse(Square.TryParse("e9", out _));
            Assert.IsFalse(Square.TryParse("i4", out _));
            Assert.IsFalse(Square.TryParse("", out _));
            Assert.IsFalse(Square.TryParse("e", out _));
        }

        [Test]
        public void OffBoard_IsNotOnBoard()
        {
            Assert.IsFalse(new Square(-1, 0).IsOnBoard);
            Assert.IsFalse(new Square(0, 8).IsOnBoard);
        }
    }
}
