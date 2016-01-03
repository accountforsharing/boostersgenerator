using NUnit.Framework;

namespace BoosterGenerator.Test
{
    [TestFixture]
    public class DocumentTest
    {
        [Test]
        public void TestGeneration()
        {
            new DocumentGenerator().Generate();
        }
    }
}
