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
            var fs = await fr.ReadVideo("z4gi1dfqgkl00000.vdat");
            fs.Close();
            fs.Dispose();
        }
    }
}
