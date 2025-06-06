﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TRR_SaveMaster
{
    class TR2Utilities
    {
        // Savegame constants
        private const int SLOT_STATUS_OFFSET = 0x004;
        private const int GAME_MODE_OFFSET = 0x008;
        private const int SAVE_NUMBER_OFFSET = 0x00C;
        private const int LEVEL_INDEX_OFFSET = 0x628;
        private const int BASE_SAVEGAME_OFFSET_TR2 = 0x72000;
        private const int MAX_SAVEGAME_OFFSET_TR2 = 0xE2000;
        private const int SAVEGAME_SIZE = 0x3800;
        private const int MAX_SAVEGAMES = 32;

        // Dynamic offsets
        private int smallMedipackOffset;
        private int largeMedipackOffset;
        private int flaresOffset;
        private int weaponsConfigNumOffset;
        private int automaticPistolsAmmoOffset;
        private int uziAmmoOffset;
        private int shotgunAmmoOffset;
        private int harpoonGunAmmoOffset;
        private int m16AmmoOffset;
        private int grenadeLauncherAmmoOffset;
        private int m16AmmoOffset2;
        private int grenadeLauncherAmmoOffset2;
        private int harpoonGunAmmoOffset2;
        private int shotgunAmmoOffset2;
        private int uziAmmoOffset2;
        private int automaticPistolsAmmoOffset2;

        // Weapon byte flags
        private const byte WEAPON_PISTOLS = 2;
        private const byte WEAPON_AUTOMATIC_PISTOLS = 4;
        private const byte WEAPON_UZIS = 8;
        private const byte WEAPON_SHOTGUN = 16;
        private const byte WEAPON_M16 = 32;
        private const byte WEAPON_GRENADE_LAUNCHER = 64;
        private const byte WEAPON_HARPOON_GUN = 128;

        // Health
        private const UInt16 MAX_HEALTH_VALUE = 1000;
        private const UInt16 MIN_HEALTH_VALUE = 1;
        private int MAX_HEALTH_OFFSET;
        private int MIN_HEALTH_OFFSET;

        // Platform
        private Platform platform;

        // Strings
        private string savegamePath;

        // Misc
        private int savegameOffset;
        private int secondaryAmmoIndex = -1;

        private byte ReadByte(int offset)
        {
            using (FileStream saveFile = new FileStream(savegamePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                saveFile.Seek(offset, SeekOrigin.Begin);
                return (byte)saveFile.ReadByte();
            }
        }

        private void WriteByte(int offset, byte value)
        {
            using (FileStream saveFile = new FileStream(savegamePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                saveFile.Seek(offset, SeekOrigin.Begin);
                byte[] byteData = { value };
                saveFile.Write(byteData, 0, byteData.Length);
            }
        }

        private UInt16 ReadUInt16(int offset)
        {
            byte lowerByte = ReadByte(offset);
            byte upperByte = ReadByte(offset + 1);

            return (UInt16)(lowerByte + (upperByte << 8));
        }

        private void WriteUInt16(int offset, UInt16 value)
        {
            if (value > 255)
            {
                byte upperByte = (byte)(value / 256);
                byte lowerByte = (byte)(value % 256);

                WriteByte(offset + 1, upperByte);
                WriteByte(offset, lowerByte);
            }
            else
            {
                WriteByte(offset + 1, 0);
                WriteByte(offset, (byte)value);
            }
        }

        private Int32 ReadInt32(int offset)
        {
            byte byte1 = ReadByte(offset);
            byte byte2 = ReadByte(offset + 1);
            byte byte3 = ReadByte(offset + 2);
            byte byte4 = ReadByte(offset + 3);

            return (Int32)(byte1 + (byte2 << 8) + (byte3 << 16) + (byte4 << 24));
        }

        private void WriteInt32(int offset, Int32 value)
        {
            WriteByte(offset, (byte)value);
            WriteByte(offset + 1, (byte)(value >> 8));
            WriteByte(offset + 2, (byte)(value >> 16));
            WriteByte(offset + 3, (byte)(value >> 24));
        }

        public bool IsSavegamePresent()
        {
            return ReadByte(savegameOffset + SLOT_STATUS_OFFSET) != 0;
        }

        private GameMode GetGameMode()
        {
            int gameMode = ReadByte(savegameOffset + GAME_MODE_OFFSET);
            return gameMode == 0 ? GameMode.Normal : GameMode.Plus;
        }

        private byte GetNumSmallMedipacks()
        {
            return ReadByte(savegameOffset + smallMedipackOffset);
        }

        private byte GetNumLargeMedipacks()
        {
            return ReadByte(savegameOffset + largeMedipackOffset);
        }

        private byte GetNumFlares()
        {
            return ReadByte(savegameOffset + flaresOffset);
        }

        private Int32 GetSaveNumber()
        {
            return ReadInt32(savegameOffset + SAVE_NUMBER_OFFSET);
        }

        private byte GetLevelIndex()
        {
            return ReadByte(savegameOffset + LEVEL_INDEX_OFFSET);
        }

        private byte GetWeaponsConfigNum()
        {
            return ReadByte(savegameOffset + weaponsConfigNumOffset);
        }

        private UInt16 GetShotgunAmmo()
        {
            return ReadUInt16(savegameOffset + shotgunAmmoOffset);
        }

        private UInt16 GetM16Ammo()
        {
            return ReadUInt16(savegameOffset + m16AmmoOffset);
        }

        private UInt16 GetUziAmmo()
        {
            return ReadUInt16(savegameOffset + uziAmmoOffset);
        }

        private UInt16 GetHarpoonGunAmmo()
        {
            return ReadUInt16(savegameOffset + harpoonGunAmmoOffset);
        }

        private UInt16 GetAutomaticPistolsAmmo()
        {
            return ReadUInt16(savegameOffset + automaticPistolsAmmoOffset);
        }

        private UInt16 GetGrenadeLauncherAmmo()
        {
            return ReadUInt16(savegameOffset + grenadeLauncherAmmoOffset);
        }

        public int GetHealthOffset()
        {
            byte[] savegameData;

            using (FileStream fs = new FileStream(savegamePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                savegameData = new byte[fs.Length];
                fs.Read(savegameData, 0, savegameData.Length);
            }

            for (int offset = MIN_HEALTH_OFFSET; offset <= MAX_HEALTH_OFFSET; offset += 0xC)
            {
                int valueIndex = savegameOffset + offset;

                if (valueIndex + 2 >= savegameData.Length)
                {
                    break;
                }

                UInt16 value = BitConverter.ToUInt16(savegameData, valueIndex);

                if (value >= MIN_HEALTH_VALUE && value <= MAX_HEALTH_VALUE)
                {
                    int flagIndex1 = savegameOffset + offset - 10;
                    int flagIndex2 = savegameOffset + offset - 9;
                    int flagIndex3 = savegameOffset + offset - 8;
                    int flagIndex4 = savegameOffset + offset - 7;

                    if (flagIndex4 >= savegameData.Length)
                    {
                        continue;
                    }

                    byte byteFlag1 = savegameData[flagIndex1];
                    byte byteFlag2 = savegameData[flagIndex2];
                    byte byteFlag3 = savegameData[flagIndex3];
                    byte byteFlag4 = savegameData[flagIndex4];

                    if (IsKnownByteFlagPattern(byteFlag1, byteFlag2, byteFlag3, byteFlag4))
                    {
                        return savegameOffset + offset;
                    }
                }
            }

            return -1;
        }

        private UInt16 GetHealthValue(int healthOffset)
        {
            return ReadUInt16(healthOffset);
        }

        private readonly Dictionary<byte, int[]> ammoIndexDataPC = new Dictionary<byte, int[]>
        {
            {  1, new int[] { 0x19BA, 0x19BB, 0x19BC, 0x19BD } },   // The Great Wall
            {  2, new int[] { 0x1CFC, 0x1CFD, 0x1CFE, 0x1CFF } },   // Venice
            {  3, new int[] { 0x1F10, 0x1F11, 0x1F12, 0x1F13 } },   // Bartoli's Hideout
            {  4, new int[] { 0x2A16, 0x2A17, 0x2A18, 0x2A19 } },   // Opera House
            {  5, new int[] { 0x1AEE, 0x1AEF, 0x1AF0, 0x1AF1 } },   // Offshore Rig
            {  6, new int[] { 0x1EBC, 0x1EBD, 0x1EBE, 0x1EBF } },   // Diving Area
            {  7, new int[] { 0x1410, 0x1411, 0x1412, 0x1413 } },   // 40 Fathoms
            {  8, new int[] { 0x2598, 0x2599, 0x259A, 0x259B } },   // Wreck of the Maria Doria
            {  9, new int[] { 0x17C2, 0x17C3, 0x17C4, 0x17C5 } },   // Living Quarters
            { 10, new int[] { 0x1C0E, 0x1C0F, 0x1C10, 0x1C11 } },   // The Deck
            { 11, new int[] { 0x1F62, 0x1F63, 0x1F64, 0x1F65 } },   // Tibetan Foothills
            { 12, new int[] { 0x2B56, 0x2B57, 0x2B58, 0x2B59 } },   // Barkhang Monastery
            { 13, new int[] { 0x2282, 0x2283, 0x2284, 0x2285 } },   // Catacombs of the Talion
            { 14, new int[] { 0x1DEE, 0x1DEF, 0x1DF0, 0x1DF1 } },   // Ice Palace
            { 15, new int[] { 0x2CB2, 0x2CB3, 0x2CB4, 0x2CB5 } },   // Temple of Xian
            { 16, new int[] { 0x1E42, 0x1E43, 0x1E44, 0x1E45 } },   // Floating Islands
            { 17, new int[] { 0x157C, 0x157D, 0x157E, 0x157F } },   // The Dragon's Lair
            { 18, new int[] { 0x1AB0, 0x1AB1, 0x1AB2, 0x1AB3 } },   // Home Sweet Home
            { 19, new int[] { 0x2CFA, 0x2CFB, 0x2CFC, 0x2CFD } },   // The Cold War
            { 20, new int[] { 0x2CF2, 0x2CF3, 0x2CF4, 0x2CF5 } },   // Fool's Gold
            { 21, new int[] { 0x2AF0, 0x2AF1, 0x2AF2, 0x2AF3 } },   // Furnace of the Gods
            { 22, new int[] { 0x210A, 0x210B, 0x210C, 0x210D } },   // Kingdom
            { 23, new int[] { 0x2354, 0x2355, 0x2356, 0x2357 } },   // Nightmare in Vegas
        };

        private readonly Dictionary<byte, int[]> ammoIndexDataConsole = new Dictionary<byte, int[]>
        {
            {  1, new int[] { 0x19B6, 0x19B7, 0x19B8, 0x19B9 } },   // The Great Wall
            {  2, new int[] { 0x1CF8, 0x1CF9, 0x1CFA, 0x1CFB } },   // Venice
            {  3, new int[] { 0x1F0C, 0x1F0D, 0x1F0E, 0x1F0F } },   // Bartoli's Hideout
            {  4, new int[] { 0x2A12, 0x2A13, 0x2A14, 0x2A15 } },   // Opera House
            {  5, new int[] { 0x1AEA, 0x1AEB, 0x1AEC, 0x1AED } },   // Offshore Rig
            {  6, new int[] { 0x1EB8, 0x1EB9, 0x1EBA, 0x1EBB } },   // Diving Area
            {  7, new int[] { 0x140C, 0x140D, 0x140E, 0x140F } },   // 40 Fathoms
            {  8, new int[] { 0x2594, 0x2595, 0x2596, 0x2597 } },   // Wreck of the Maria Doria
            {  9, new int[] { 0x17BE, 0x17BF, 0x17C0, 0x17C1 } },   // Living Quarters
            { 10, new int[] { 0x1C0A, 0x1C0A, 0x1C0B, 0x1C0E } },   // The Deck
            { 11, new int[] { 0x1F5E, 0x1F5F, 0x1F60, 0x1F61 } },   // Tibetan Foothills
            { 12, new int[] { 0x2B52, 0x2B53, 0x2B54, 0x2B55 } },   // Barkhang Monastery
            { 13, new int[] { 0x227E, 0x227F, 0x2280, 0x2281 } },   // Catacombs of the Talion
            { 14, new int[] { 0x1DEA, 0x1DEB, 0x1DEC, 0x1DED } },   // Ice Palace
            { 15, new int[] { 0x2CAE, 0x2CAB, 0x2CAC, 0x2CAD } },   // Temple of Xian
            { 16, new int[] { 0x1E3E, 0x1E3F, 0x1E40, 0x1E41 } },   // Floating Islands
            { 17, new int[] { 0x1578, 0x1579, 0x157A, 0x157B } },   // The Dragon's Lair
            { 18, new int[] { 0x1AAC, 0x1AAB, 0x1AAC, 0x1AAD } },   // Home Sweet Home
            { 19, new int[] { 0x2CF6, 0x2CF7, 0x2CF8, 0x2CF9 } },   // The Cold War
            { 20, new int[] { 0x2CEE, 0x2CEF, 0x2CF0, 0x2CF1 } },   // Fool's Gold
            { 21, new int[] { 0x2AEC, 0x2AED, 0x2AEE, 0x2AEF } },   // Furnace of the Gods
            { 22, new int[] { 0x2106, 0x2107, 0x2108, 0x2109 } },   // Kingdom
            { 23, new int[] { 0x2350, 0x2351, 0x2352, 0x2353 } },   // Nightmare in Vegas
        };

        private int GetSecondaryAmmoIndex()
        {
            byte levelIndex = GetLevelIndex();

            Dictionary<byte, int[]> ammoIndexData;

            if (platform == Platform.PC)
            {
                ammoIndexData = ammoIndexDataPC;
            }
            else
            {
                ammoIndexData = ammoIndexDataConsole;
            }

            if (ammoIndexData.ContainsKey(levelIndex))
            {
                int[] indexData = ammoIndexData[levelIndex];

                int[] offsets1 = new int[indexData.Length];
                int[] offsets2 = new int[indexData.Length];

                for (int index = 0; index < 25; index++)
                {
                    Array.Copy(indexData, offsets1, indexData.Length);

                    for (int i = 0; i < indexData.Length; i++)
                    {
                        offsets2[i] = offsets1[i] + 0xA;

                        offsets1[i] += savegameOffset + (index * 0xC);
                        offsets2[i] += savegameOffset + (index * 0xC);
                    }

                    if (offsets1.All(offset => ReadByte(offset) == 0xFF))
                    {
                        return index;
                    }

                    if (offsets2.All(offset => ReadByte(offset) == 0xFF))
                    {
                        return index;
                    }
                }
            }

            return -1;
        }

        private int GetSecondaryAmmoOffset(int baseOffset)
        {
            return baseOffset + (secondaryAmmoIndex * 0xC);
        }

        private bool IsKnownByteFlagPattern(byte byteFlag1, byte byteFlag2, byte byteFlag3, byte byteFlag4)
        {
            if (byteFlag1 == 0x02 && byteFlag2 == 0x00 && byteFlag3 == 0x02 && byteFlag4 == 0x00) return true;  // Standing
            if (byteFlag1 == 0x13 && byteFlag2 == 0x00 && byteFlag3 == 0x13 && byteFlag4 == 0x00) return true;  // Climbing
            if (byteFlag1 == 0x21 && byteFlag2 == 0x00 && byteFlag3 == 0x21 && byteFlag4 == 0x00) return true;  // On water
            if (byteFlag1 == 0x0D && byteFlag2 == 0x00 && byteFlag3 == 0x0D && byteFlag4 == 0x00) return true;  // Underwater
            if (byteFlag1 == 0x12 && byteFlag2 == 0x00 && byteFlag3 == 0x12 && byteFlag4 == 0x00) return true;  // Swimming
            if (byteFlag1 == 0x17 && byteFlag2 == 0x00 && byteFlag3 == 0x02 && byteFlag4 == 0x00) return true;  // Rolling
            if (byteFlag1 == 0x41 && byteFlag2 == 0x00 && byteFlag3 == 0x02 && byteFlag4 == 0x00) return true;  // Walking through water
            if (byteFlag1 == 0x22 && byteFlag2 == 0x00 && byteFlag3 == 0x22 && byteFlag4 == 0x00) return true;  // Wading through water
            if (byteFlag1 == 0x01 && byteFlag2 == 0x00 && byteFlag3 == 0x02 && byteFlag4 == 0x00) return true;  // Running forward
            if (byteFlag1 == 0x03 && byteFlag2 == 0x00 && byteFlag3 == 0x03 && byteFlag4 == 0x00) return true;  // Jumping forward
            if (byteFlag1 == 0x20 && byteFlag2 == 0x00 && byteFlag3 == 0x20 && byteFlag4 == 0x00) return true;  // Sliding backward
            if (byteFlag1 == 0x18 && byteFlag2 == 0x00 && byteFlag3 == 0x18 && byteFlag4 == 0x00) return true;  // Sliding downhill
            if (byteFlag1 == 0x2A && byteFlag2 == 0x00 && byteFlag3 == 0x02 && byteFlag4 == 0x00) return true;  // Using puzzle item
            if (byteFlag1 == 0x01 && byteFlag2 == 0x00 && byteFlag3 == 0x01 && byteFlag4 == 0x00) return true;  // Motorboat
            if (byteFlag1 == 0x05 && byteFlag2 == 0x00 && byteFlag3 == 0x05 && byteFlag4 == 0x00) return true;  // Motorboat
            if (byteFlag1 == 0x08 && byteFlag2 == 0x00 && byteFlag3 == 0x08 && byteFlag4 == 0x00) return true;  // Snowmobile
            if (byteFlag1 == 0x04 && byteFlag2 == 0x00 && byteFlag3 == 0x04 && byteFlag4 == 0x00) return true;  // Snowmobile

            return false;
        }

        public bool IsLaraInVehicle(int healthOffset)
        {
            byte byteFlag1 = ReadByte(healthOffset - 10);
            byte byteFlag2 = ReadByte(healthOffset - 9);
            byte byteFlag3 = ReadByte(healthOffset - 8);
            byte byteFlag4 = ReadByte(healthOffset - 7);

            if (byteFlag1 == 0x01 && byteFlag2 == 0x00 && byteFlag3 == 0x01 && byteFlag4 == 0x00) return true;  // Motorboat
            if (byteFlag1 == 0x05 && byteFlag2 == 0x00 && byteFlag3 == 0x05 && byteFlag4 == 0x00) return true;  // Motorboat
            if (byteFlag1 == 0x08 && byteFlag2 == 0x00 && byteFlag3 == 0x08 && byteFlag4 == 0x00) return true;  // Snowmobile
            if (byteFlag1 == 0x04 && byteFlag2 == 0x00 && byteFlag3 == 0x04 && byteFlag4 == 0x00) return true;  // Snowmobile

            return false;
        }

        private readonly Dictionary<byte, string> levelNames = new Dictionary<byte, string>()
        {
            {  1,  "The Great Wall"             },
            {  2,  "Venice"                     },
            {  3,  "Bartoli's Hideout"          },
            {  4,  "Opera House"                },
            {  5,  "Offshore Rig"               },
            {  6,  "Diving Area"                },
            {  7,  "40 Fathoms"                 },
            {  8,  "Wreck of the Maria Doria"   },
            {  9,  "Living Quarters"            },
            { 10,  "The Deck"                   },
            { 11,  "Tibetan Foothills"          },
            { 12,  "Barkhang Monastery"         },
            { 13,  "Catacombs of the Talion"    },
            { 14,  "Ice Palace"                 },
            { 15,  "Temple of Xian"             },
            { 16,  "Floating Islands"           },
            { 17,  "The Dragon's Lair"          },
            { 18,  "Home Sweet Home"            },
            { 19,  "The Cold War"               },
            { 20,  "Fool's Gold"                },
            { 21,  "Furnace of the Gods"        },
            { 22,  "Kingdom"                    },
            { 23,  "Nightmare in Vegas"         },
        };

        private void WriteSaveNumber(Int32 value)
        {
            WriteInt32(savegameOffset + SAVE_NUMBER_OFFSET, value);
        }

        private void WriteNumSmallMedipacks(byte value)
        {
            WriteByte(savegameOffset + smallMedipackOffset, value);
        }

        private void WriteNumLargeMedipacks(byte value)
        {
            WriteByte(savegameOffset + largeMedipackOffset, value);
        }

        private void WriteNumFlares(byte value)
        {
            WriteByte(savegameOffset + flaresOffset, value);
        }

        private void WriteWeaponsConfigNum(byte value)
        {
            WriteByte(savegameOffset + weaponsConfigNumOffset, value);
        }

        private void WriteHealthValue(UInt16 newHealth)
        {
            int healthOffset = GetHealthOffset();

            if (healthOffset != -1)
            {
                WriteUInt16(healthOffset, newHealth);
            }
        }

        private void WriteAutomaticPistolsAmmo(bool isPresent, UInt16 ammo)
        {
            WriteUInt16(savegameOffset + automaticPistolsAmmoOffset, ammo);

            if (isPresent && secondaryAmmoIndex != -1)
            {
                WriteUInt16(savegameOffset + automaticPistolsAmmoOffset2, ammo);
            }
            else if (!isPresent && secondaryAmmoIndex != -1)
            {
                WriteUInt16(savegameOffset + automaticPistolsAmmoOffset2, 0);
            }
        }

        private void WriteShotgunAmmo(bool isPresent, UInt16 ammo)
        {
            WriteUInt16(savegameOffset + shotgunAmmoOffset, ammo);

            if (isPresent && secondaryAmmoIndex != -1)
            {
                WriteUInt16(savegameOffset + shotgunAmmoOffset2, ammo);
            }
            else if (!isPresent && secondaryAmmoIndex != -1)
            {
                WriteUInt16(savegameOffset + shotgunAmmoOffset2, 0);
            }
        }

        private void WriteUziAmmo(bool isPresent, UInt16 ammo)
        {
            WriteUInt16(savegameOffset + uziAmmoOffset, ammo);

            if (isPresent && secondaryAmmoIndex != -1)
            {
                WriteUInt16(savegameOffset + uziAmmoOffset2, ammo);
            }
            else if (!isPresent && secondaryAmmoIndex != -1)
            {
                WriteUInt16(savegameOffset + uziAmmoOffset2, 0);
            }
        }

        private void WriteHarpoonGunAmmo(bool isPresent, UInt16 ammo)
        {
            WriteUInt16(savegameOffset + harpoonGunAmmoOffset, ammo);

            if (isPresent && secondaryAmmoIndex != -1)
            {
                WriteUInt16(savegameOffset + harpoonGunAmmoOffset2, ammo);
            }
            else if (!isPresent && secondaryAmmoIndex != -1)
            {
                WriteUInt16(savegameOffset + harpoonGunAmmoOffset2, 0);
            }
        }

        private void WriteGrenadeLauncherAmmo(bool isPresent, UInt16 ammo)
        {
            WriteUInt16(savegameOffset + grenadeLauncherAmmoOffset, ammo);

            if (isPresent && secondaryAmmoIndex != -1)
            {
                WriteUInt16(savegameOffset + grenadeLauncherAmmoOffset2, ammo);
            }
            else if (!isPresent && secondaryAmmoIndex != -1)
            {
                WriteUInt16(savegameOffset + grenadeLauncherAmmoOffset2, 0);
            }
        }

        private void WriteM16Ammo(bool isPresent, UInt16 ammo)
        {
            WriteUInt16(savegameOffset + m16AmmoOffset, ammo);

            if (isPresent && secondaryAmmoIndex != -1)
            {
                WriteUInt16(savegameOffset + m16AmmoOffset2, ammo);
            }
            else if (!isPresent && secondaryAmmoIndex != -1)
            {
                WriteUInt16(savegameOffset + m16AmmoOffset2, 0);
            }
        }

        private void DetermineOffsets()
        {
            byte levelIndex = GetLevelIndex();

            automaticPistolsAmmoOffset = 0x12 + (levelIndex * 0x30);
            uziAmmoOffset = 0x14 + (levelIndex * 0x30);
            shotgunAmmoOffset = 0x16 + (levelIndex * 0x30);
            m16AmmoOffset = 0x18 + (levelIndex * 0x30);
            grenadeLauncherAmmoOffset = 0x1A + (levelIndex * 0x30);
            harpoonGunAmmoOffset = 0x1C + (levelIndex * 0x30);

            smallMedipackOffset = 0x1E + (levelIndex * 0x30);
            largeMedipackOffset = 0x1F + (levelIndex * 0x30);
            flaresOffset = 0x21 + (levelIndex * 0x30);
            weaponsConfigNumOffset = 0x3C + (levelIndex * 0x30);

            if (levelIndex == 1)        // The Great Wall
            {
                MIN_HEALTH_OFFSET = 0xB64;
                MAX_HEALTH_OFFSET = 0xBAC;
            }
            else if (levelIndex == 2)   // Venice
            {
                MIN_HEALTH_OFFSET = 0x7FA;
                MAX_HEALTH_OFFSET = 0x7FA;
            }
            else if (levelIndex == 3)   // Bartoli's Hideout
            {
                MIN_HEALTH_OFFSET = 0x1734;
                MAX_HEALTH_OFFSET = 0x1764;
            }
            else if (levelIndex == 4)   // Opera House
            {
                MIN_HEALTH_OFFSET = 0x1E20;
                MAX_HEALTH_OFFSET = 0x1E38;
            }
            else if (levelIndex == 5)   // Offshore Rig
            {
                MIN_HEALTH_OFFSET = 0xAC4;
                MAX_HEALTH_OFFSET = 0xADC;
            }
            else if (levelIndex == 6)   // Diving Area
            {
                MIN_HEALTH_OFFSET = 0x12DE;
                MAX_HEALTH_OFFSET = 0x131A;
            }
            else if (levelIndex == 7)   // 40 Fathoms
            {
                MIN_HEALTH_OFFSET = 0x7FC;
                MAX_HEALTH_OFFSET = 0x7FC;
            }
            else if (levelIndex == 8)   // Wreck of the Maria Doria
            {
                MIN_HEALTH_OFFSET = 0x238E;
                MAX_HEALTH_OFFSET = 0x242A;
            }
            else if (levelIndex == 9)   // Living Quarters
            {
                MIN_HEALTH_OFFSET = 0x90A;
                MAX_HEALTH_OFFSET = 0x90A;
            }
            else if (levelIndex == 10)  // The Deck
            {
                MIN_HEALTH_OFFSET = 0xBAC;
                MAX_HEALTH_OFFSET = 0xBF4;
            }
            else if (levelIndex == 11)  // Tibetan Foothills
            {
                MIN_HEALTH_OFFSET = 0x12E4;
                MAX_HEALTH_OFFSET = 0x1314;
            }
            else if (levelIndex == 12)  // Barkhang Monastery
            {
                MIN_HEALTH_OFFSET = 0x2522;
                MAX_HEALTH_OFFSET = 0x25FA;
            }
            else if (levelIndex == 13)  // Catacombs of the Talion
            {
                MIN_HEALTH_OFFSET = 0x7F8;
                MAX_HEALTH_OFFSET = 0x7F8;
            }
            else if (levelIndex == 14)  // Ice Palace
            {
                MIN_HEALTH_OFFSET = 0xE2A;
                MAX_HEALTH_OFFSET = 0xE4E;
            }
            else if (levelIndex == 15)  // Temple of Xian
            {
                MIN_HEALTH_OFFSET = 0x2A7A;
                MAX_HEALTH_OFFSET = 0x2AC2;
            }
            else if (levelIndex == 16)  // Floating Islands
            {
                MIN_HEALTH_OFFSET = 0x9CC;
                MAX_HEALTH_OFFSET = 0x9D8;
            }
            else if (levelIndex == 17)  // The Dragon's Lair
            {
                MIN_HEALTH_OFFSET = 0xF78;
                MAX_HEALTH_OFFSET = 0xFC0;
            }
            else if (levelIndex == 18)  // Home Sweet Home
            {
                MIN_HEALTH_OFFSET = 0xE86;
                MAX_HEALTH_OFFSET = 0xF2E;
            }
            else if (levelIndex == 19)  // The Cold War
            {
                MIN_HEALTH_OFFSET = 0x1626;
                MAX_HEALTH_OFFSET = 0x1656;
            }
            else if (levelIndex == 20)  // Fool's Gold
            {
                MIN_HEALTH_OFFSET = 0x1D80;
                MAX_HEALTH_OFFSET = 0x1DBC;
            }
            else if (levelIndex == 21)  // Furnace of the Gods
            {
                MIN_HEALTH_OFFSET = 0x1FD4;
                MAX_HEALTH_OFFSET = 0x2064;
            }
            else if (levelIndex == 22)  // Kingdom
            {
                MIN_HEALTH_OFFSET = 0x91A;
                MAX_HEALTH_OFFSET = 0x926;
            }
            else if (levelIndex == 23)  // Nightmare in Vegas
            {
                MIN_HEALTH_OFFSET = 0xDDA;
                MAX_HEALTH_OFFSET = 0xDF2;
            }

            if (platform != Platform.PC)
            {
                MIN_HEALTH_OFFSET -= 4;
                MAX_HEALTH_OFFSET -= 4;
            }
        }

        public void SetLevelParams(CheckBox chkPistols, CheckBox chkShotgun, CheckBox chkAutomaticPistols, CheckBox chkUzis,
            CheckBox chkM16, CheckBox chkGrenadeLauncher, CheckBox chkHarpoonGun, NumericUpDown nudShotgunAmmo,
            NumericUpDown nudAutomaticPistolsAmmo, NumericUpDown nudUziAmmo, NumericUpDown nudM16Ammo,
            NumericUpDown nudGrenadeLauncherAmmo, NumericUpDown nudHarpoonGunAmmo, Label lblPistolAmmo)
        {
            byte levelIndex = GetLevelIndex();

            if (levelIndex == 18)       // Home Sweet Home
            {
                chkPistols.Enabled = false;
                chkShotgun.Enabled = true;
                chkAutomaticPistols.Enabled = false;
                chkUzis.Enabled = false;
                chkM16.Enabled = false;
                chkGrenadeLauncher.Enabled = false;
                chkHarpoonGun.Enabled = false;

                nudShotgunAmmo.Enabled = true;
                nudAutomaticPistolsAmmo.Enabled = false;
                nudUziAmmo.Enabled = false;
                nudM16Ammo.Enabled = false;
                nudGrenadeLauncherAmmo.Enabled = false;
                nudHarpoonGunAmmo.Enabled = false;

                lblPistolAmmo.Enabled = false;
            }
            else if (levelIndex == 23)  // Nightmare in Vegas
            {
                chkPistols.Enabled = true;
                chkShotgun.Enabled = true;
                chkAutomaticPistols.Enabled = true;
                chkUzis.Enabled = true;
                chkM16.Enabled = false;
                chkGrenadeLauncher.Enabled = false;
                chkHarpoonGun.Enabled = false;

                nudShotgunAmmo.Enabled = true;
                nudAutomaticPistolsAmmo.Enabled = true;
                nudUziAmmo.Enabled = true;
                nudM16Ammo.Enabled = false;
                nudGrenadeLauncherAmmo.Enabled = false;
                nudHarpoonGunAmmo.Enabled = false;

                lblPistolAmmo.Enabled = true;
            }
            else
            {
                chkPistols.Enabled = true;
                chkShotgun.Enabled = true;
                chkAutomaticPistols.Enabled = true;
                chkUzis.Enabled = true;
                chkM16.Enabled = true;
                chkGrenadeLauncher.Enabled = true;
                chkHarpoonGun.Enabled = true;

                nudShotgunAmmo.Enabled = true;
                nudAutomaticPistolsAmmo.Enabled = true;
                nudUziAmmo.Enabled = true;
                nudM16Ammo.Enabled = true;
                nudGrenadeLauncherAmmo.Enabled = true;
                nudHarpoonGunAmmo.Enabled = true;

                lblPistolAmmo.Enabled = true;
            }
        }

        public void DisplayGameInfo(CheckBox chkPistols, CheckBox chkAutomaticPistols, CheckBox chkUzis,
            CheckBox chkM16, CheckBox chkGrenadeLauncher, CheckBox chkHarpoonGun, NumericUpDown nudSaveNumber,
            NumericUpDown nudAutomaticPistolsAmmo, CheckBox chkShotgun, NumericUpDown nudUziAmmo,
            NumericUpDown nudM16Ammo, NumericUpDown nudGrenadeLauncherAmmo, NumericUpDown nudHarpoonGunAmmo,
            NumericUpDown nudShotgunAmmo, NumericUpDown nudFlares, NumericUpDown nudSmallMedipacks,
            NumericUpDown nudLargeMedipacks, TrackBar trbHealth, Label lblHealth, Label lblHealthError)
        {
            DetermineOffsets();

            nudSaveNumber.Value = GetSaveNumber();
            nudSmallMedipacks.Value = GetNumSmallMedipacks();
            nudLargeMedipacks.Value = GetNumLargeMedipacks();
            nudFlares.Value = GetNumFlares();

            byte levelIndex = GetLevelIndex();

            if (levelIndex == 18)       // Home Sweet Home
            {
                nudAutomaticPistolsAmmo.Value = 0;
                nudUziAmmo.Value = 0;
                nudM16Ammo.Value = 0;
                nudGrenadeLauncherAmmo.Value = 0;
                nudHarpoonGunAmmo.Value = 0;
            }
            else if (levelIndex == 23)  // Nightmare in Vegas
            {
                nudAutomaticPistolsAmmo.Value = GetAutomaticPistolsAmmo();
                nudUziAmmo.Value = GetUziAmmo();
                nudM16Ammo.Value = 0;
                nudGrenadeLauncherAmmo.Value = 0;
                nudHarpoonGunAmmo.Value = 0;
            }
            else
            {
                nudAutomaticPistolsAmmo.Value = GetAutomaticPistolsAmmo();
                nudUziAmmo.Value = GetUziAmmo();
                nudM16Ammo.Value = GetM16Ammo();
                nudGrenadeLauncherAmmo.Value = GetGrenadeLauncherAmmo();
                nudHarpoonGunAmmo.Value = GetHarpoonGunAmmo();
            }

            nudShotgunAmmo.Value = GetShotgunAmmo() / 6;

            byte weaponsConfigNum = GetWeaponsConfigNum();

            if (weaponsConfigNum == 1)
            {
                chkPistols.Checked = false;
                chkAutomaticPistols.Checked = false;
                chkUzis.Checked = false;
                chkShotgun.Checked = false;
                chkM16.Checked = false;
                chkGrenadeLauncher.Checked = false;
                chkHarpoonGun.Checked = false;
            }
            else
            {
                chkPistols.Checked = (weaponsConfigNum & WEAPON_PISTOLS) != 0;
                chkAutomaticPistols.Checked = (weaponsConfigNum & WEAPON_AUTOMATIC_PISTOLS) != 0;
                chkUzis.Checked = (weaponsConfigNum & WEAPON_UZIS) != 0;
                chkShotgun.Checked = (weaponsConfigNum & WEAPON_SHOTGUN) != 0;
                chkM16.Checked = (weaponsConfigNum & WEAPON_M16) != 0;
                chkGrenadeLauncher.Checked = (weaponsConfigNum & WEAPON_GRENADE_LAUNCHER) != 0;
                chkHarpoonGun.Checked = (weaponsConfigNum & WEAPON_HARPOON_GUN) != 0;
            }

            int healthOffset = GetHealthOffset();

            if (healthOffset != -1)
            {
                UInt16 health = GetHealthValue(healthOffset);
                double healthPercentage = ((double)health / MAX_HEALTH_VALUE) * 100;
                trbHealth.Value = health;
                trbHealth.Enabled = true;

                lblHealth.Text = healthPercentage.ToString("0.0") + "%";
                lblHealthError.Visible = false;
                lblHealth.Visible = true;
            }
            else
            {
                trbHealth.Enabled = false;
                trbHealth.Value = trbHealth.Minimum;
                lblHealthError.Visible = true;
                lblHealth.Visible = false;
            }
        }

        public void WriteChanges(CheckBox chkPistols, CheckBox chkAutomaticPistols, CheckBox chkUzis, CheckBox chkShotgun,
            CheckBox chkM16, CheckBox chkGrenadeLauncher, CheckBox chkHarpoonGun, NumericUpDown nudSaveNumber, NumericUpDown nudFlares,
            NumericUpDown nudSmallMedipacks, NumericUpDown nudLargeMedipacks, NumericUpDown nudAutomaticPistolsAmmo,
            NumericUpDown nudUziAmmo, NumericUpDown nudM16Ammo, NumericUpDown nudGrenadeLauncherAmmo, NumericUpDown nudHarpoonGunAmmo,
            NumericUpDown nudShotgunAmmo, TrackBar trbHealth)
        {
            WriteSaveNumber((Int32)nudSaveNumber.Value);
            WriteNumSmallMedipacks((byte)nudSmallMedipacks.Value);
            WriteNumLargeMedipacks((byte)nudLargeMedipacks.Value);
            WriteNumFlares((byte)nudFlares.Value);

            byte newWeaponsConfigNum = 1;

            if (chkPistols.Checked) newWeaponsConfigNum += WEAPON_PISTOLS;
            if (chkAutomaticPistols.Checked) newWeaponsConfigNum += WEAPON_AUTOMATIC_PISTOLS;
            if (chkUzis.Checked) newWeaponsConfigNum += WEAPON_UZIS;
            if (chkShotgun.Checked) newWeaponsConfigNum += WEAPON_SHOTGUN;
            if (chkM16.Checked) newWeaponsConfigNum += WEAPON_M16;
            if (chkGrenadeLauncher.Checked) newWeaponsConfigNum += WEAPON_GRENADE_LAUNCHER;
            if (chkHarpoonGun.Checked) newWeaponsConfigNum += WEAPON_HARPOON_GUN;

            WriteWeaponsConfigNum(newWeaponsConfigNum);

            byte levelIndex = GetLevelIndex();
            secondaryAmmoIndex = GetSecondaryAmmoIndex();

            if (secondaryAmmoIndex != -1)
            {
                Dictionary<byte, int[]> ammoIndexData;

                if (platform == Platform.PC)
                {
                    ammoIndexData = ammoIndexDataPC;
                }
                else
                {
                    ammoIndexData = ammoIndexDataConsole;
                }

                int baseSecondaryAmmoOffset = ammoIndexData[levelIndex][0];

                automaticPistolsAmmoOffset2 = GetSecondaryAmmoOffset(baseSecondaryAmmoOffset - 0xAC);
                uziAmmoOffset2 = GetSecondaryAmmoOffset(baseSecondaryAmmoOffset - 0xA4);
                shotgunAmmoOffset2 = GetSecondaryAmmoOffset(baseSecondaryAmmoOffset - 0x9C);
                harpoonGunAmmoOffset2 = GetSecondaryAmmoOffset(baseSecondaryAmmoOffset - 0x94);
                grenadeLauncherAmmoOffset2 = GetSecondaryAmmoOffset(baseSecondaryAmmoOffset - 0x8C);
                m16AmmoOffset2 = GetSecondaryAmmoOffset(baseSecondaryAmmoOffset - 0x7C);
            }

            if (levelIndex == 18)       // Home Sweet Home
            {
                WriteShotgunAmmo(chkShotgun.Checked, (UInt16)(nudShotgunAmmo.Value * 6));
            }
            else if (levelIndex == 23)  // Nightmare in Vegas
            {
                WriteShotgunAmmo(chkShotgun.Checked, (UInt16)(nudShotgunAmmo.Value * 6));
                WriteAutomaticPistolsAmmo(chkAutomaticPistols.Checked, (UInt16)nudAutomaticPistolsAmmo.Value);
                WriteUziAmmo(chkUzis.Checked, (UInt16)nudUziAmmo.Value);
            }
            else
            {
                WriteShotgunAmmo(chkShotgun.Checked, (UInt16)(nudShotgunAmmo.Value * 6));
                WriteAutomaticPistolsAmmo(chkAutomaticPistols.Checked, (UInt16)nudAutomaticPistolsAmmo.Value);
                WriteUziAmmo(chkUzis.Checked, (UInt16)nudUziAmmo.Value);
                WriteHarpoonGunAmmo(chkHarpoonGun.Checked, (UInt16)nudHarpoonGunAmmo.Value);
                WriteGrenadeLauncherAmmo(chkGrenadeLauncher.Checked, (UInt16)nudGrenadeLauncherAmmo.Value);
                WriteM16Ammo(chkM16.Checked, (UInt16)nudM16Ammo.Value);
            }

            if (trbHealth.Enabled)
            {
                WriteHealthValue((UInt16)trbHealth.Value);
            }
        }

        public void UpdateDisplayName(Savegame savegame)
        {
            bool savegamePresent = ReadByte(savegame.Offset + SLOT_STATUS_OFFSET) != 0;

            if (savegamePresent)
            {
                byte levelIndex = ReadByte(savegame.Offset + LEVEL_INDEX_OFFSET);
                Int32 saveNumber = ReadInt32(savegame.Offset + SAVE_NUMBER_OFFSET);

                if (levelNames.ContainsKey(levelIndex) && saveNumber >= 0)
                {
                    string levelName = levelNames[levelIndex];
                    GameMode gameMode = ReadByte(savegame.Offset + GAME_MODE_OFFSET) == 0 ? GameMode.Normal : GameMode.Plus;

                    savegame.UpdateDisplayName(levelName, saveNumber, gameMode);
                }
            }
        }

        public void SetPlatform(Platform platform)
        {
            this.platform = platform;
        }

        public void SetSavegamePath(string path)
        {
            savegamePath = path;
        }

        public void SetSavegameOffset(int offset)
        {
            savegameOffset = offset;
        }

        public void PopulateEmptySlots(ComboBox cmbSavegames)
        {
            if (cmbSavegames.Items.Count == MAX_SAVEGAMES)
            {
                return;
            }

            for (int i = cmbSavegames.Items.Count; i < MAX_SAVEGAMES; i++)
            {
                int currentSavegameOffset = BASE_SAVEGAME_OFFSET_TR2 + (i * SAVEGAME_SIZE);

                if (currentSavegameOffset < MAX_SAVEGAME_OFFSET_TR2)
                {
                    Int32 saveNumber = ReadInt32(currentSavegameOffset + SAVE_NUMBER_OFFSET);
                    byte levelIndex = ReadByte(currentSavegameOffset + LEVEL_INDEX_OFFSET);
                    bool savegamePresent = ReadByte(currentSavegameOffset + SLOT_STATUS_OFFSET) != 0;

                    if (savegamePresent && levelNames.ContainsKey(levelIndex) && saveNumber >= 0)
                    {
                        int slot = (currentSavegameOffset - BASE_SAVEGAME_OFFSET_TR2) / SAVEGAME_SIZE;

                        bool savegameExists = false;

                        foreach (Savegame existingSavegame in cmbSavegames.Items)
                        {
                            if (existingSavegame.Slot == slot)
                            {
                                savegameExists = true;
                                break;
                            }
                        }

                        if (!savegameExists)
                        {
                            string levelName = levelNames[levelIndex];
                            GameMode gameMode = ReadByte(currentSavegameOffset + GAME_MODE_OFFSET) == 0 ? GameMode.Normal : GameMode.Plus;

                            Savegame savegame = new Savegame(currentSavegameOffset, slot, saveNumber, levelName, gameMode);
                            cmbSavegames.Items.Add(savegame);
                        }
                    }
                }
            }
        }

        public void PopulateSavegames(ComboBox cmbSavegames)
        {
            int numSaves = 0;

            for (int i = 0; i < MAX_SAVEGAMES; i++)
            {
                int currentSavegameOffset = BASE_SAVEGAME_OFFSET_TR2 + (i * SAVEGAME_SIZE);
                SetSavegameOffset(currentSavegameOffset);

                Int32 saveNumber = GetSaveNumber();
                byte levelIndex = GetLevelIndex();
                bool savegamePresent = IsSavegamePresent();

                if (savegamePresent && levelNames.ContainsKey(levelIndex) && saveNumber >= 0)
                {
                    string levelName = levelNames[levelIndex];
                    int slot = (currentSavegameOffset - BASE_SAVEGAME_OFFSET_TR2) / SAVEGAME_SIZE;
                    GameMode gameMode = GetGameMode();

                    Savegame savegame = new Savegame(currentSavegameOffset, slot, saveNumber, levelName, gameMode);
                    cmbSavegames.Items.Add(savegame);

                    numSaves++;
                }
            }

            if (numSaves > 0)
            {
                cmbSavegames.SelectedIndex = 0;
            }
        }
    }
}
