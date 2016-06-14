﻿using NesCore.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesCore.Storage
{
    public class Cartridge
    {
        public Cartridge(BinaryReader romBinaryReader)
        {
            SaveRam = new SaveRam();

            uint magicNumber = romBinaryReader.ReadUInt32();

            if (magicNumber != InesMagicNumber)
                throw new InvalidDataException("INES Magic Number mismatch");

            // read header
            byte programBankCount = romBinaryReader.ReadByte();
            byte characterBankCount = romBinaryReader.ReadByte();
            byte controlBits1 = romBinaryReader.ReadByte();
            byte controlBits2 = romBinaryReader.ReadByte();
            byte programRamSize = romBinaryReader.ReadByte();
            romBinaryReader.ReadBytes(7); // unused 7 bytes

            // determine mapper type from control bits
            int mapperTypeLowerNybble = controlBits1 >> 4;
            int mapperTypeHigherNybble = controlBits2 >> 4;
            MapperType = (byte)((mapperTypeHigherNybble << 4) | mapperTypeLowerNybble);

            // determine mirroring mode
            int mirrorLowBit = controlBits1 & 1;
            int mirrorHighBit = (controlBits1 >> 3) & 1;
            MirrorMode = (MirrorMode)((mirrorHighBit << 1) | mirrorLowBit);

            // battery-backed RAM
            BatteryPresent = (controlBits1 & 0x2) != 0;

            // read trainer if present (unused)
            if ((controlBits1 & 0x04) == 0x04)
            {
                byte[] trainer = romBinaryReader.ReadBytes(512);
            }

            // read prg-rom bank(s)
            byte[] programData = romBinaryReader.ReadBytes(programBankCount * 0x4000);
            ProgramRom = new List<byte>(programData);

            // read chr-rom bank(s)
            CharacterRom = characterBankCount == 0
                ? new byte[0x2000] // at least one default empty bank if there are none
                : romBinaryReader.ReadBytes(characterBankCount * 0x2000);

            // instantiate appropriate mapper
            switch (MapperType)
            {
                case 0: Map = new CartridgeMapNRom(this); break;
                case 1: Map = new CartridgeMapMmc1(this); break;
                case 2: Map = new CartridgeMapUxRom(this); break;
                case 3: Map = new CartridgeMapCnRom(this); break;
                case 4: Map = new CartridgeMapMmc3(this); break;
                case 7: Map = new CartridgeMapAxRom(this); break;
                case 9: Map = new CartridgeMapMmc2(this); break;
                case 10: Map = new CartridgeMapMmc4(this); break;
                case 11: Map = new CartridgeMapColourDreams(this); break;
                case 13: Map = new CartridgeMapCpRom(this); break;
                case 15: Map = new CartridgeMap100In1(this); break;
                case 66: Map = new CartridgeMapGxRom(this); break;
                case 71: Map = new CartridgeMapCamerica71(this); break;
                default: throw new NotSupportedException(
                    "Mapper Type " + Utility.Hex.Format(MapperType) + " not supported");
            }
        }

        public IReadOnlyList<byte> ProgramRom { get; private set; }
        public byte[] CharacterRom { get; private set; }
        public SaveRam SaveRam { get; }
        public byte MapperType { get; private set; }
        public MirrorMode MirrorMode { get; set; }
        public bool BatteryPresent { get; private set; }

        public CartridgeMap Map { get; private set; }

        public Action MirrorModeChanged { get; set; }

        public override string ToString()
        {
            return "PRG: " + Hex.Format((uint)ProgramRom.Count)
                + "b, CHR: " + Hex.Format((uint)CharacterRom.Length)
                + "b, Mapper Type: " + Hex.Format(MapperType)
                + ", Mirror Mode:" + MirrorMode + " (" + (byte)MirrorMode + ")"
                + ", Battery: " + (BatteryPresent ? "Yes" : "No");
        }

        private const uint InesMagicNumber = 0x1a53454e;
    }

   
}
