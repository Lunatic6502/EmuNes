﻿using NesCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesCore.Storage
{
    class CartridgeMapMmc5 : CartridgeMap
    {
        public CartridgeMapMmc5(Cartridge cartridge)
        {
            Cartridge = cartridge;

            // 64K ram total (switchable 8 banks of 8K)
            programRam = new byte[0x10000];

            // 1K expansion ram
            extendedRam = new byte[0x400];

            programBankMode = 0;

            characterBanks = new ushort[12];
        }

        public Cartridge Cartridge { get; private set; }

        public override string Name { get { return "MMC5"; } }

        public override byte this[ushort address]
        {
            get
            {
                if (address == 0x5205)
                    return productLow;

                if (address == 0x5206)
                    return productHigh;

                if (address >= 0x5C00 && address < 0x6000)
                {
                    // expansion ram - all modes
                    switch (extendedRamMode)
                    {
                        case 0:
                        case 1:
                            // expansion ram mode 0/1 - returns open bus
                            return (byte)(address >> 8);
                        case 2:
                            // expansion ram mode 2 - 1K r/w memory
                        case 3:
                            // expansion ram mode 3 - 1K ROM
                            return extendedRam[address % 0x400];
                        default:
                            throw new Exception("MMC5 Invalid expansion ram mode");
                    }
                }

                if (address >= 0x6000 && address < 0x8000)
                {
                    // all bank modes - 8K switchable RAM bank
                    int offset = address % 0x2000;
                    return programRam[programRamBank * 0x2000 + offset];
                }

                if (address >= 0x8000)
                {
                    // program banks for all modes
                    switch (programBankMode)
                    {
                        case 0:
                            {
                                // PRG mode 0 - single 32k switchable ROM bank
                                int offset = address % 0x8000;
                                return Cartridge.ProgramRom[programBank0 * 0x8000 + offset];
                            }
                        case 1:
                            if (address < 0xC000)
                            {
                                // PRG mode 1 - first 16k switchable ROM/RAM bank
                                int offset = address % 0x4000;
                                return Cartridge.ProgramRom[programBank1 * 0x4000 + offset];
                            }
                            else // if (address >= 0xC000)
                            {
                                // PRG mode 1 - second 16k switchable ROM bank
                                int offset = address % 0x4000;
                                return Cartridge.ProgramRom[programRomBank * 0x4000 + offset];
                            }
                        case 2:
                            if (address < 0xC000)
                            {
                                // PRG mode 2 - 16k switchable ROM/RAM bank
                                int offset = address % 0x4000;
                                return Cartridge.ProgramRom[programBank1 * 0x4000 + offset];
                            }
                            else if (address < 0xE000)
                            {
                                // PRG mode 2 - first 8k switchable ROM/RAM bank
                                int offset = address % 0x2000;
                                return Cartridge.ProgramRom[programBank2 * 0x2000 + offset];
                            }
                            else // if (address >= 0xE000 )
                            {
                                // PRG mode 2 - second 8k switchable ROM bank
                                int offset = address % 0x2000;
                                return Cartridge.ProgramRom[programRomBank * 0x2000 + offset];
                            }
                        case 3:
                            if (address < 0xA000)
                            {
                                // PRG mode 3 - first 8k switchable ROM/RAM bank
                                int offset = address % 0x2000;
                                return Cartridge.ProgramRom[programBank0 * 0x2000 + offset];
                            }
                            else if (address < 0xC000)
                            {
                                // PRG mode 3 - second 8k switchable ROM/RAM bank
                                int offset = address % 0x2000;
                                return Cartridge.ProgramRom[programBank1 * 0x2000 + offset];
                            }
                            else if (address < 0xE000 )
                            {
                                // PRG mode 3 - third 8k switchable ROM/RAM bank
                                int offset = address % 0x2000;
                                return Cartridge.ProgramRom[programBank2 * 0x2000 + offset];
                            }
                            else // if (address >= 0xE000)
                            {
                                // PRG mode 3 - fourth 8k switchable ROM/RAM bank
                                int offset = address % 0x2000;
                                return Cartridge.ProgramRom[programRomBank * 0x2000 + offset];
                            }
                        default:
                            throw new Exception("MMC5 Invalid program bank mode");
                    }
                }

                // invalid / unhandled addresses
                throw new Exception("Unhandled " + Name + " mapper read at address: " + Hex.Format(address));
            }

            set
            {
                // registers
                if (address == 0x5100)
                {
                    programBankMode = (byte)(value & 0x03);
                    return;
                }
                if (address == 0x5101)
                {
                    characterBankMode = (byte)(value & 0x03);

                    // compute character bank count and size depending on mode
                    characterBankSize = (ushort)(2 ^ (3 - characterBankMode));
                    characterBankCount = (ushort)(Cartridge.CharacterRom.Length / characterBankSize);
                    return;
                }
                if (address == 0x5102)
                {
                    programRamProtect1 = value == 2;
                    programRamProtect = programRamProtect1 && programRamProtect2;
                    return;
                }
                if (address == 0x5103)
                {
                    programRamProtect2 = value == 1;
                    programRamProtect = programRamProtect1 && programRamProtect2;
                    return;
                }
                if (address == 0x5104)
                {
                    extendedRamMode = (byte)(value & 0x03);
                    return;
                }
                if (address == 0x5105)
                {
                    nameTableA = (byte)(value & 0x03);
                    nameTableB = (byte)((value >> 2) & 0x03);
                    nameTableC = (byte)((value >> 4) & 0x03);
                    nameTableD = (byte)((value >> 6) & 0x03);
                    return;
                }
                if (address == 0x5106)
                {
                    fillModeTile = value;
                    return;
                }
                if (address == 0x5107)
                {
                    fillModeAttributes = (byte)(value & 0x03);
                    return;
                }
                if (address == 0x5113)
                {
                    //---- -CBB : C: chip, BB: bank within chip (CBB can be treated as 8 banks) 
                    programRamBank = (byte)(value & 0x07);
                    return;
                }
                if (address == 0x5114)
                {
                    //RBBB BBBB : R - ROM mode, BBBBBBB - bank number 
                    romMode0 = (value & 0x80) != 0;
                    programBank0 = (byte)(value & 0x7F);
                    return;
                }
                if (address == 0x5115)
                {
                    //RBBB BBBB : R - ROM mode, BBBBBBB - bank number 
                    romMode1 = (value & 0x80) != 0;
                    programBank1 = (byte)(value & 0x7F);
                    return;
                }
                if (address == 0x5116)
                {
                    //RBBB BBBB : R - ROM mode, BBBBBBB - bank number 
                    romMode2 = (value & 0x80) != 0;
                    programBank2 = (byte)(value & 0x7F);
                    return;
                }
                if (address == 0x5117)
                {
                    //-BBB BBBB : BBBBBBB - bank number 
                    programRomBank = (byte)(value & 0x7F);
                    return;
                }

                if (address >= 0x5120 && address <= 0x512B)
                {
                    ushort characterBank = 0;
                    // merge low bits
                    characterBank |= value;
                    // merge high bits
                    characterBank |= characterBankUpper;
                    // ensure within available banks
                    characterBank %= characterBankCount;
                    // assign to corresponding bank switch
                    characterBanks[address - 0x5120] = characterBank;
                    return;
                }

                if (address == 0x5130)
                {
                    // upper 2 bits (bit 8, 9) for character bank selection (all banks)
                    characterBankUpper = value;
                    characterBankUpper &= 0x03;
                    characterBankUpper <<= 8;
                    return;
                }

                if (address == 0x5205)
                {
                    factor1 = value;
                    EvaluateProduct();
                    return;
                }
                if (address == 0x5206)
                {
                    factor2 = value;
                    EvaluateProduct();
                    return;
                }

                if (address >= 0x5C00 && address < 0x6000)
                {
                    // expansion ram - all modes
                    switch (extendedRamMode)
                    {
                        case 0:
                        case 1:
                            // expansion ram mode 0/1 - writes allowed when ppu rendering, otherwise zero written
                            extendedRam[address % 0x400] = ppuRendering ? value : (byte)0;
                            return;
                        case 2:
                            // expansion ram mode 2 - 1K r/w memory
                            extendedRam[address % 0x400] = value;
                            return;
                        case 3:
                            // expansion ram mode 3 - 1K ROM (read only - do nothing?)
                            return;
                        default:
                            throw new Exception("MMC5 Invalid expansion ram mode");
                    }
                }

                if (address >= 0x8000)
                {
                    // program banks for all modes
                    switch (programBankMode)
                    {
                        case 0:
                            {
                                // PRG mode 0 - single 32k switchable ROM bank
                                throw new Exception("Cannot write to MMC5 ROM range $8000-$FFFF in PRG mode 0");
                            }
                        case 1:
                            if (address < 0xC000)
                            {
                                // PRG mode 1 - first 16k switchable ROM/RAM bank
                                if (romMode1)
                                    throw new Exception("Cannot write to MMC5 range $8000-$BFFF in PRG mode 1 in ROM mode");
                                else
                                {
                                    if (programRamProtect)
                                        throw new Exception("Cannot write to MMC5 RAM range $C000-$FFFF in PRG mode 1 with Write Protect 1/2");
                                    else
                                    {
                                        int offset = address % 0x4000;
                                        programRam[programBank1 * 0x4000 + offset] = value;
                                        return;
                                    }
                                }
                            }
                            else // if (address >= 0xC000)
                            {
                                // PRG mode 1 - second 16k switchable ROM bank
                                throw new Exception("Cannot write to MMC5 ROM range C8000-$FFFF in PRG mode 1");
                            }
                        case 2:
                            if (address < 0xC000)
                            {
                                // PRG mode 2 - 16k switchable ROM/RAM bank
                                if (romMode1)
                                    throw new Exception("Cannot write to MMC5 range $8000-$BFFF in PRG mode 2 in ROM mode");
                                else
                                {
                                    if (programRamProtect)
                                        throw new Exception("Cannot write to MMC5 RAM range $8000-$BFFF in PRG mode 2 with Write Protect 1/2");
                                    else
                                    {
                                        int offset = address % 0x4000;
                                        programRam[programBank1 * 0x4000 + offset] = value;
                                        return;
                                    }
                                }
                            }
                            else if (address < 0xE000)
                            {
                                // PRG mode 2 - first 8k switchable ROM/RAM bank
                                if (romMode2)
                                    throw new Exception("Cannot write to MMC5 range $C000-$DFFF in PRG mode 2 in ROM mode");
                                else
                                {
                                    if (programRamProtect)
                                        throw new Exception("Cannot write to MMC5 RAM range $C000-$DFFF in PRG mode 2 with Write Protect 1/2");
                                    else
                                    {
                                        int offset = address % 0x2000;
                                        programRam[programBank2 * 0x2000 + offset] = value;
                                        return;
                                    }
                                }
                            }
                            else // if (address >= 0xE000 )
                            {
                                // PRG mode 2 - second 8k switchable ROM bank
                                throw new Exception("Cannot write to MMC5 ROM range $E000-$FFFF in PRG mode 2");
                            }
                        case 3:
                            if (address < 0xA000)
                            {
                                // PRG mode 3 - first 8k switchable ROM/RAM bank
                                if (romMode0)
                                    throw new Exception("Cannot write to MMC5 range $8000-$9FFF in PRG mode 3 in ROM mode");
                                else
                                {
                                    if (programRamProtect)
                                        throw new Exception("Cannot write to MMC5 RAM range $8000-$9FFF in PRG mode 3 with Write Protect 1/2");
                                    else
                                    {
                                        int offset = address % 0x2000;
                                        programRam[programBank0 * 0x2000 + offset] = value;
                                        return;
                                    }
                                }
                            }
                            else if (address < 0xC000)
                            {
                                // PRG mode 3 - second 8k switchable ROM/RAM bank
                                if (romMode1)
                                    throw new Exception("Cannot write to MMC5 range $A000-$BFFF in PRG mode 3 in ROM mode");
                                else
                                {
                                    if (programRamProtect)
                                        throw new Exception("Cannot write to MMC5 RAM range $A000-$BFFF in PRG mode 3 with Write Protect 1/2");
                                    else
                                    {
                                        int offset = address % 0x2000;
                                        programRam[programBank1 * 0x2000 + offset] = value;
                                        return;
                                    }
                                }
                            }
                            else if (address < 0xE000)
                            {
                                // PRG mode 3 - third 8k switchable ROM/RAM bank
                                if (romMode2)
                                    throw new Exception("Cannot write to MMC5 range $C000-$DFFF in PRG mode 3 in ROM mode");
                                else
                                {
                                    if (programRamProtect)
                                        throw new Exception("Cannot write to MMC5 RAM range $C000-$DFFF in PRG mode 3 with Write Protect 1/2");
                                    else
                                    {
                                        int offset = address % 0x2000;
                                        programRam[programBank2 * 0x2000 + offset] = value;
                                        return;
                                    }
                                }
                            }
                            else // if (address >= 0xE000)
                            {
                                // PRG mode 3 - fourth 8k switchable ROM bank
                                throw new Exception("Cannot write to MMC5 ROM range $E000-$FFFF in PRG mode 3");
                            }
                        default:
                            throw new Exception("MMC5 Invalid program bank mode");
                    }
                }

                // invalid / unhandled addresses
                throw new Exception("Unhandled " + Name + " mapper write at address: " + Hex.Format(address));

            }
        }

        public override void StepVideo(int scanLine, int cycle, bool showBackground, bool showSprites)
        {
            ppuRendering = scanLine >= 0 && scanLine < 240;
        }

        private void EvaluateProduct()
        {
            int product = factor1 * factor2;
            productLow = (byte)product;
            productHigh = (byte)(product >> 8);
        }

        // program ram
        private byte[] programRam;

        // program bank mode and switching
        private byte programBankMode;

        private byte programRamBank;
        private bool programRamProtect1;
        private bool programRamProtect2;
        private bool programRamProtect;

        private byte programBank0;
        private bool romMode0;

        private byte programBank1;
        private bool romMode1;

        private byte programBank2;
        private bool romMode2;

        private byte programRomBank;

        // extended ram
        private byte[] extendedRam;
        private byte extendedRamMode;
        private bool ppuRendering;

        // character bank mode and switching
        private byte characterBankMode;
        private ushort characterBankSize;
        private ushort characterBankCount;
        private ushort[] characterBanks;
        private ushort characterBankUpper;

        // nametables
        private byte nameTableA;
        private byte nameTableB;
        private byte nameTableC;
        private byte nameTableD;

        // fill mode
        byte fillModeTile;
        byte fillModeAttributes;

        // multiplication
        private byte factor1;
        private byte factor2;
        private byte productLow;
        private byte productHigh;
    }
}