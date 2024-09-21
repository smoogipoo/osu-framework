using System.Threading.Tasks;
using osu.Framework.Development;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Wangs
{
    public partial class Class1 : TestScene
    {
        [Test]
        public async Task MyTest()
        {
            await Assert.That(() => ThreadSafety.IsUpdateThread).IsTrue().AtSomePoint();
        }
    }
}
