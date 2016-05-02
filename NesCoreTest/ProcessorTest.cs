﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NesCore.Processing;
using NesCore.Utility;

namespace NesCoreTest
{
    [TestClass]
    public class ProcessorTest: SystemBusTest
    {
        public ProcessorTest()
        {
            processor = new Processor(this);
            assembler = new Assembler(processor);
        }

        [TestMethod]
        public void TestStack()
        {
            ResetSystem();
            processor.Push(0x69);
            Assert.IsTrue(processor.State.StackPointer == 0xFC, "SP did not update correctly (8bit)");
            Assert.IsTrue(processor.Pull() == 0x69, "push and pull error (8bit)");

            processor.Push16(0x1234);
            Assert.IsTrue(processor.State.StackPointer == 0xFB, "SP did not update correctly (16bit)");
            Assert.IsTrue(processor.Pull16() == 0x1234, "push and pull error (16bit)");
        }

        [TestMethod]
        public void TestInstructionBrk()
        {
            // assembler test
            ResetSystem();
            assembler.GenerateProgram(0x1000,
                @"BRK");
            Assert.IsTrue(Read(0x1000) == 0x00, "BRK instruction not assembled");

            // execution test
            processor.State.ProgramCounter = 0x1000;
            processor.Write16(Processor.IrqVector, 0x2030);
            processor.State.InterruptDisableFlag = false;
            byte statusFlags = processor.State.Flags;

            processor.ExecuteInstruction();
            Assert.IsTrue(processor.State.ProgramCounter == 0x2030, "PC not set");
            Assert.IsTrue(processor.State.InterruptDisableFlag, "Interrupt Disable flag not set");
            Assert.IsTrue(processor.Pull() == (byte)(statusFlags | 0x10), "status flags not preserved");
            Assert.IsTrue(processor.Pull16() == 0x1001, "PC not pushed on stack prior to BRK");
        }

        [TestMethod]
        public void TestInstructionOraIzx()
        {
            // assembler test
            ResetSystem();
            assembler.GenerateProgram(0x1000,
                @"ORA ($10,X) ; OR accumulator with contents of address $2000 contained at address [$90, $91] computed by $10 offset from X ($80)");
            Assert.IsTrue(Read(0x1000) == 0x01, "ORA/IZX instruction not assembled");
            Assert.IsTrue(Read(0x1001) == 0x10, "Indexed indirect 0x10 not written");

            // execution test
            processor.State.ProgramCounter = 0x1000;
            processor.State.RegisterX = 0x80;
            processor.Write16(0x0090, 0x2000);
            Write(0x2000, 0x0F);
            processor.State.Accumulator = 0xF0;

            processor.ExecuteInstruction();

            Assert.IsTrue(processor.State.Accumulator == 0xFF, "Value $FF expected in Accumulator");
        }

        [TestMethod]
        public void TestInstructionOraZp()
        {
            // assembler test
            ResetSystem();
            assembler.GenerateProgram(0x1000,
                @"ORA $10 ; OR accumulator with contents of address $0010");
            Assert.IsTrue(Read(0x1000) == 0x05, "ORA/ZP instruction not assembled");
            Assert.IsTrue(Read(0x1001) == 0x10, "ZP operand 0x10 not written");

            // execution test
            processor.State.ProgramCounter = 0x1000;
            Write(0x0010, 0x0F);
            processor.State.Accumulator = 0xF0;

            processor.ExecuteInstruction();

            Assert.IsTrue(processor.State.Accumulator == 0xFF, "Value $FF expected in Accumulator");
        }

        [TestMethod]
        public void TestInstructionAslZp()
        {
            // assembler test
            ResetSystem();
            assembler.GenerateProgram(0x1000,
                @";7 shifts of 00000001 -> 10000000, C
                  ASL $10 ; ASL contents of address $0010
                  ASL $10 ; ASL contents of address $0010
                  ASL $10 ; ASL contents of address $0010
                  ASL $10 ; ASL contents of address $0010
                  ASL $10 ; ASL contents of address $0010
                  ASL $10 ; ASL contents of address $0010
                  ASL $10 ; ASL contents of address $0010
                  ASL $10 ; ASL contents of address $0010");
            Assert.IsTrue(Read(0x1000) == 0x06, "ASL/ZP instruction not assembled");
            Assert.IsTrue(Read(0x1001) == 0x10, "ZP operand 0x10 not written");

            // execution test
            processor.State.ProgramCounter = 0x1000;
            Write(0x0010, 0x01);
 
            // execute first 7 ASL
            processor.ExecuteInstructions(7);

            Assert.IsTrue(Read(0x0010) == 0x80, "Value $80 expected at location $0010");

            // execute last ASL
            processor.ExecuteInstruction();
            Assert.IsTrue(Read(0x0010) == 0x00, "Value $00 expected at location $0010");
            Assert.IsTrue(processor.State.CarryFlag, "Carry not set");
        }

        [TestMethod]
        public void TestImpliedInstructions()
        {
            ResetSystem();
            processor.Write16(Processor.IrqVector, 0x2030);
            assembler.GenerateProgram(0x1000,
                @"BRK");

            WipeMemory();
            assembler.GenerateProgram(0x1000,
                @"NOP ;no operation
                  TAX ;transfer A to X");

            Assert.IsTrue(Read(0x1000) == 0xEA);
            Assert.IsTrue(Read(0x1001) == 0xAA);
        }

        [TestMethod]
        public void TestImmediateInstructions()
        {
            ResetSystem();
            assembler.GenerateProgram(0x1000,
                @"LDX #$10 ;load value 16 in x register
                  INX      ;increment x register
                  DEX      ;decrement x register
                  DEX      ;decrement x register");

            Assert.IsTrue(Read(0x1000) == 0xA2); //LDX
            Assert.IsTrue(Read(0x1001) == 0x10); //#$10
            Assert.IsTrue(Read(0x1002) == 0xE8); //INX
            Assert.IsTrue(Read(0x1003) == 0xCA); //DEX
            Assert.IsTrue(Read(0x1004) == 0xCA); //DEX

            processor.State.ProgramCounter = 0x1000;
            processor.ExecuteInstructions(4);

            Assert.IsTrue(processor.State.RegisterX == 0x0F);
        }

        private void ResetSystem()
        {
            WipeMemory();
            processor.Reset();
        }

        private Processor processor;
        private Assembler assembler;
    }
}
