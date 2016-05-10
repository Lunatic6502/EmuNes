﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using NesCore.Memory;
using NesCore.Utility;

namespace NesCoreTest
{
    [TestClass]
    public class MemoryTest
    {
        public MemoryTest()
        {
            memoryMap = new MemoryMap();
        }

        [TestMethod, TestCategory("Memory")]
        public void TestMemoryDirectReadWrite()
        {
            memoryMap.ResetConfiguration();
            memoryMap.Wipe();
            memoryMap[0x1000] = 0x12;
            Assert.IsTrue(memoryMap[0x1000] == 0x12, "Value $12 expected at address $1000");
        }

        [TestMethod]
        public void TestMemoryMirorredReadWrite()
        {
            memoryMap.ResetConfiguration();
            memoryMap.Wipe();
            memoryMap.ConfigureAddressMirroring(0x1000, 0x800, 0x3000);

            memoryMap[0x1100] = 0x12;
            Assert.IsTrue(memoryMap[0x1900] == 0x12, "Value $12 expected at address $1900");
            Assert.IsTrue(memoryMap[0x2100] == 0x12, "Value $12 expected at address $2100");
            Assert.IsTrue(memoryMap[0x2900] == 0x12, "Value $12 expected at address $2900");
        }

        [TestMethod]
        public void TestMemoryRawPerformance()
        {
            memoryMap.ResetConfiguration();
            memoryMap.Wipe();

            Random random = new Random();
            double testDuration = 1.0;
            int writes = 0;
            DateTime dateTimeStart = DateTime.Now;
            while ((DateTime.Now - dateTimeStart).TotalSeconds < testDuration)
            {
                byte value = (byte)random.Next();
                ushort address = (ushort)random.Next();
                memoryMap[address] = value;
                ++writes;
            }
            Console.WriteLine("Writes per second: " + writes / testDuration);
        }

        private MemoryMap memoryMap;
    }
}
