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
            var fr = new AVFileReader(@"C:\Users\Raphael\Source\Repos\WebRTCPOC\WebRTCPOC\bin");
            var fs = await fr.ReadVideo("l3zui0aesk000000.vdat");
            fs.Close();
            fs.Dispose();
        }
    }
}
