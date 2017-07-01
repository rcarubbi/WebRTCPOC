using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace AVRecordManager.Tests
{
    [TestClass]
    public class RecoverTests
    {
        [TestMethod]
        public async Task RecoverVideoFromCaller()
        {
            var fr = new AVFileReader(@"C:\Users\Raphael\Source\Repos\WebRTCPOC\AVRecordManager.Tests\bin\Debug");
            var fs = await fr.ReadVideo("qn9c5bthsbm00000.vdat");
            fs.Close();
            fs.Dispose();
        }
    }
}
