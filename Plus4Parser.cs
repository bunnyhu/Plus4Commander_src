using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Plus4Commander
{
    public class DiskHeaderInfo
    {
        public string DiskName { get; set; } = string.Empty;
        public byte[] RawDiskNameBytes { get; set; } = Array.Empty<byte>();
        public string DiskId { get; set; } = string.Empty;
        public string DosType { get; set; } = string.Empty;
        public int FreeBlocks { get; set; }
        public int TotalBlocks { get; set; } = 664;
        public bool IsT64 { get; set; } = false;
    }

    public class DiskFileItem
    {
        public string Name { get; set; } = string.Empty;
        public byte[] RawNameBytes { get; set; } = Array.Empty<byte>();
        public string Type { get; set; } = string.Empty;
        public int StartAddress { get; set; }
        public int EndAddress { get; set; }
        public int SizeInBytes { get; set; }
        public int BlockSize { get; set; }
        public int RecordLength { get; set; } = 0;
        public byte[] RawData { get; set; } = Array.Empty<byte>();
        public bool IsVirtual { get; set; } = true;
    }

    public enum Plus4ContentType
    {
        BasicProgram,
        MachineCode,
        ScreenOrBitmap,
        FontCharset,
        SeqDatabaseOrText,
        UnknownBinary
    }

    public enum ImagePaletteMode
    {
        Auto,
        Botticelli,
        MultiB,
        Raw
    }

    public static class FileClassifier
    {
        public static Plus4ContentType Identify(DiskFileItem file)
        {
            if (file.Type.Contains("SEQ") || file.Type.Contains("USR") || file.Type.Contains("REL"))
                return Plus4ContentType.SeqDatabaseOrText;

            // A BASIC-ellenőrzés a betűkészlet elé került: egy 2048-2060 bájtos
            // BASIC program ne legyen betűkészletnek nézve.
            if (LooksLikeBasic(file))
                return Plus4ContentType.BasicProgram;

            if (file.SizeInBytes >= 2048 && file.SizeInBytes <= 2060)
                return Plus4ContentType.FontCharset;

            if (file.SizeInBytes >= 7000 && file.SizeInBytes <= 12500)
                return Plus4ContentType.ScreenOrBitmap;

            return Plus4ContentType.MachineCode;
        }

        private static bool LooksLikeBasic(DiskFileItem file)
        {
            byte[] d = file.RawData;

            if (file.StartAddress == 0x1001 || file.StartAddress == 0x4001)
            {
                // következő sor mutatójának alsó bájtja != 0, felső bájtja < $80
                return d.Length > 4 && d[2] != 0 && d[3] < 0x80;
            }

            // $4000 kezdőcím + egy 0 bájt (ugyanezt kezeli a DetokenizeBasic is)
            if (file.StartAddress == 0x4000 && d.Length > 6 && d[2] == 0x00)
            {
                return (d[3] != 0 || d[4] != 0) && d[4] < 0x80;
            }

            return false;
        }
    }

    public static class Plus4Parser
    {
        private const int DataInterleave = 10;

        private enum AddrMode { IMP, ACC, IMM, ZP, ZPX, ZPY, ABS, ABX, ABY, IND, IZX, IZY, REL }

        private static readonly (string Mnemonic, AddrMode Mode)[] OpcodeTable = new (string, AddrMode)[256]
        {
            ("BRK", AddrMode.IMP), ("ORA", AddrMode.IZX), ("???", AddrMode.IMP), ("???", AddrMode.IMP),
            ("???", AddrMode.IMP), ("ORA", AddrMode.ZP),  ("ASL", AddrMode.ZP),  ("???", AddrMode.IMP),
            ("PHP", AddrMode.IMP), ("ORA", AddrMode.IMM), ("ASL", AddrMode.ACC), ("???", AddrMode.IMP),
            ("???", AddrMode.IMP), ("ORA", AddrMode.ABS), ("ASL", AddrMode.ABS), ("???", AddrMode.IMP),
            ("BPL", AddrMode.REL), ("ORA", AddrMode.IZY), ("???", AddrMode.IMP), ("???", AddrMode.IMP),
            ("???", AddrMode.IMP), ("ORA", AddrMode.ZPX), ("ASL", AddrMode.ZPX), ("???", AddrMode.IMP),
            ("CLC", AddrMode.IMP), ("ORA", AddrMode.ABY), ("???", AddrMode.IMP), ("???", AddrMode.IMP),
            ("???", AddrMode.IMP), ("ORA", AddrMode.ABX), ("ASL", AddrMode.ABX), ("???", AddrMode.IMP),
            ("JSR", AddrMode.ABS), ("AND", AddrMode.IZX), ("???", AddrMode.IMP), ("???", AddrMode.IMP),
            ("BIT", AddrMode.ZP),  ("AND", AddrMode.ZP),  ("ROL", AddrMode.ZP),  ("???", AddrMode.IMP),
            ("PLP", AddrMode.IMP), ("AND", AddrMode.IMM), ("ROL", AddrMode.ACC), ("???", AddrMode.IMP),
            ("BIT", AddrMode.ABS), ("AND", AddrMode.ABS), ("ROL", AddrMode.ABS), ("???", AddrMode.IMP),
            ("BMI", AddrMode.REL), ("AND", AddrMode.IZY), ("???", AddrMode.IMP), ("???", AddrMode.IMP),
            ("???", AddrMode.IMP), ("AND", AddrMode.ZPX), ("ROL", AddrMode.ZPX), ("???", AddrMode.IMP),
            ("SEC", AddrMode.IMP), ("AND", AddrMode.ABY), ("???", AddrMode.IMP), ("???", AddrMode.IMP),
            ("???", AddrMode.IMP), ("AND", AddrMode.ABX), ("ROL", AddrMode.ABX), ("???", AddrMode.IMP),
            ("RTI", AddrMode.IMP), ("EOR", AddrMode.IZX), ("???", AddrMode.IMP), ("???", AddrMode.IMP),
            ("???", AddrMode.IMP), ("EOR", AddrMode.ZP),  ("LSR", AddrMode.ZP),  ("???", AddrMode.IMP),
            ("PHA", AddrMode.IMP), ("EOR", AddrMode.IMM), ("LSR", AddrMode.ACC), ("???", AddrMode.IMP),
            ("JMP", AddrMode.ABS), ("EOR", AddrMode.ABS), ("LSR", AddrMode.ABS), ("???", AddrMode.IMP),
            ("BVC", AddrMode.REL), ("EOR", AddrMode.IZY), ("???", AddrMode.IMP), ("???", AddrMode.IMP),
            ("???", AddrMode.IMP), ("EOR", AddrMode.ZPX), ("LSR", AddrMode.ZPX), ("???", AddrMode.IMP),
            ("CLI", AddrMode.IMP), ("EOR", AddrMode.ABY), ("???", AddrMode.IMP), ("???", AddrMode.IMP),
            ("???", AddrMode.IMP), ("EOR", AddrMode.ABX), ("LSR", AddrMode.ABX), ("???", AddrMode.IMP),
            ("RTS", AddrMode.IMP), ("ADC", AddrMode.IZX), ("???", AddrMode.IMP), ("???", AddrMode.IMP),
            ("???", AddrMode.IMP), ("ADC", AddrMode.ZP),  ("ROR", AddrMode.ZP),  ("???", AddrMode.IMP),
            ("PLA", AddrMode.IMP), ("ADC", AddrMode.IMM), ("ROR", AddrMode.ACC), ("???", AddrMode.IMP),
            ("JMP", AddrMode.IND), ("ADC", AddrMode.ABS), ("ROR", AddrMode.ABS), ("???", AddrMode.IMP),
            ("BVS", AddrMode.REL), ("ADC", AddrMode.IZY), ("???", AddrMode.IMP), ("???", AddrMode.IMP),
            ("???", AddrMode.IMP), ("ADC", AddrMode.ZPX), ("ROR", AddrMode.ZPX), ("???", AddrMode.IMP),
            ("SEI", AddrMode.IMP), ("ADC", AddrMode.ABY), ("???", AddrMode.IMP), ("???", AddrMode.IMP),
            ("???", AddrMode.IMP), ("ADC", AddrMode.ABX), ("ROR", AddrMode.ABX), ("???", AddrMode.IMP),
            ("???", AddrMode.IMP), ("STA", AddrMode.IZX), ("???", AddrMode.IMP), ("???", AddrMode.IMP),
            ("STY", AddrMode.ZP),  ("STA", AddrMode.ZP),  ("STX", AddrMode.ZP),  ("???", AddrMode.IMP),
            ("DEY", AddrMode.IMP), ("???", AddrMode.IMP), ("TXA", AddrMode.IMP), ("???", AddrMode.IMP),
            ("STY", AddrMode.ABS), ("STA", AddrMode.ABS), ("STX", AddrMode.ABS), ("???", AddrMode.IMP),
            ("BCC", AddrMode.REL), ("STA", AddrMode.IZY), ("???", AddrMode.IMP), ("???", AddrMode.IMP),
            ("STY", AddrMode.ZPX), ("STA", AddrMode.ZPX), ("STX", AddrMode.ZPY), ("???", AddrMode.IMP),
            ("TYA", AddrMode.IMP), ("STA", AddrMode.ABY), ("TXS", AddrMode.IMP), ("???", AddrMode.IMP),
            ("???", AddrMode.IMP), ("STA", AddrMode.ABX), ("???", AddrMode.IMP), ("???", AddrMode.IMP),
            ("LDY", AddrMode.IMM), ("LDA", AddrMode.IZX), ("LDX", AddrMode.IMM), ("???", AddrMode.IMP),
            ("LDY", AddrMode.ZP),  ("LDA", AddrMode.ZP),  ("LDX", AddrMode.ZP),  ("???", AddrMode.IMP),
            ("TAY", AddrMode.IMP), ("LDA", AddrMode.IMM), ("TAX", AddrMode.IMP), ("???", AddrMode.IMP),
            ("LDY", AddrMode.ABS), ("LDA", AddrMode.ABS), ("LDX", AddrMode.ABS), ("???", AddrMode.IMP),
            ("BCS", AddrMode.REL), ("LDA", AddrMode.IZY), ("???", AddrMode.IMP), ("???", AddrMode.IMP),
            ("LDY", AddrMode.ZPX), ("LDA", AddrMode.ZPX), ("LDX", AddrMode.ZPY), ("???", AddrMode.IMP),
            ("CLV", AddrMode.IMP), ("LDA", AddrMode.ABY), ("TSX", AddrMode.IMP), ("???", AddrMode.IMP),
            ("LDY", AddrMode.ABX), ("LDA", AddrMode.ABX), ("LDX", AddrMode.ABY), ("???", AddrMode.IMP),
            ("CPY", AddrMode.IMM), ("CMP", AddrMode.IZX), ("???", AddrMode.IMP), ("???", AddrMode.IMP),
            ("CPY", AddrMode.ZP),  ("CMP", AddrMode.ZP),  ("DEC", AddrMode.ZP),  ("???", AddrMode.IMP),
            ("INY", AddrMode.IMP), ("CMP", AddrMode.IMM), ("DEX", AddrMode.IMP), ("???", AddrMode.IMP),
            ("CPY", AddrMode.ABS), ("CMP", AddrMode.ABS), ("DEC", AddrMode.ABS), ("???", AddrMode.IMP),
            ("BNE", AddrMode.REL), ("CMP", AddrMode.IZY), ("???", AddrMode.IMP), ("???", AddrMode.IMP),
            ("???", AddrMode.IMP), ("CMP", AddrMode.ZPX), ("DEC", AddrMode.ZPX), ("???", AddrMode.IMP),
            ("CLD", AddrMode.IMP), ("CMP", AddrMode.ABY), ("???", AddrMode.IMP), ("???", AddrMode.IMP),
            ("???", AddrMode.IMP), ("CMP", AddrMode.ABX), ("DEC", AddrMode.ABX), ("???", AddrMode.IMP),
            ("CPX", AddrMode.IMM), ("SBC", AddrMode.IZX), ("???", AddrMode.IMP), ("???", AddrMode.IMP),
            ("CPX", AddrMode.ZP),  ("SBC", AddrMode.ZP),  ("INC", AddrMode.ZP),  ("???", AddrMode.IMP),
            ("INX", AddrMode.IMP), ("SBC", AddrMode.IMM), ("NOP", AddrMode.IMP), ("???", AddrMode.IMP),
            ("CPX", AddrMode.ABS), ("SBC", AddrMode.ABS), ("INC", AddrMode.ABS), ("???", AddrMode.IMP),
            ("BEQ", AddrMode.REL), ("SBC", AddrMode.IZY), ("???", AddrMode.IMP), ("???", AddrMode.IMP),
            ("???", AddrMode.IMP), ("SBC", AddrMode.ZPX), ("INC", AddrMode.ZPX), ("???", AddrMode.IMP),
            ("SED", AddrMode.IMP), ("SBC", AddrMode.ABY), ("???", AddrMode.IMP), ("???", AddrMode.IMP),
            ("???", AddrMode.IMP), ("SBC", AddrMode.ABX), ("INC", AddrMode.ABX), ("???", AddrMode.IMP)
        };

        private static readonly Dictionary<byte, string> BasicTokens = new Dictionary<byte, string>()
        {
            { 0x80, "END" }, { 0x81, "FOR" }, { 0x82, "NEXT" }, { 0x83, "DATA" },
            { 0x84, "INPUT#" }, { 0x85, "INPUT" }, { 0x86, "DIM" }, { 0x87, "READ" },
            { 0x88, "LET" }, { 0x89, "GOTO" }, { 0x8A, "RUN" }, { 0x8B, "IF" },
            { 0x8C, "RESTORE" }, { 0x8D, "GOSUB" }, { 0x8E, "RETURN" }, { 0x8F, "REM" },
            { 0x90, "STOP" }, { 0x91, "ON" }, { 0x92, "WAIT" }, { 0x93, "LOAD" },
            { 0x94, "SAVE" }, { 0x95, "VERIFY" }, { 0x96, "DEF" }, { 0x97, "POKE" },
            { 0x98, "PRINT#" }, { 0x99, "PRINT" }, { 0x9A, "CONT" }, { 0x9B, "LIST" },
            { 0x9C, "CLR" }, { 0x9D, "CMD" }, { 0x9E, "SYS" }, { 0x9F, "OPEN" },
            { 0xA0, "CLOSE" }, { 0xA1, "GET" }, { 0xA2, "NEW" }, { 0xA3, "TAB(" },
            { 0xA4, "TO" }, { 0xA5, "FN" }, { 0xA6, "SPC(" }, { 0xA7, "THEN" },
            { 0xA8, "NOT" }, { 0xA9, "STEP" }, { 0xAA, "+" }, { 0xAB, "-" },
            { 0xAC, "*" }, { 0xAD, "/" }, { 0xAE, "^" }, { 0xAF, "AND" },
            { 0xB0, "OR" }, { 0xB1, ">" }, { 0xB2, "=" }, { 0xB3, "<" },
            { 0xB4, "SGN" }, { 0xB5, "INT" }, { 0xB6, "ABS" }, { 0xB7, "USR" },
            { 0xB8, "FRE" }, { 0xB9, "POS" }, { 0xBA, "SQR" }, { 0xBB, "RND" },
            { 0xBC, "LOG" }, { 0xBD, "EXP" }, { 0xBE, "COS" }, { 0xBF, "SIN" },
            { 0xC0, "TAN" }, { 0xC1, "ATN" }, { 0xC2, "PEEK" }, { 0xC3, "LEN" },
            { 0xC4, "STR$" }, { 0xC5, "VAL" }, { 0xC6, "ASC" }, { 0xC7, "CHR$" },
            { 0xC8, "LEFT$" }, { 0xC9, "RIGHT$" }, { 0xCA, "MID$" }, { 0xCB, "GO" },
            { 0xCC, "RGR" }, { 0xCD, "RCLR" }, { 0xCE, "RLUM" }, { 0xCF, "JOY" },
            { 0xD0, "RDOT" }, { 0xD1, "DEC" }, { 0xD2, "HEX$" }, { 0xD3, "ERR$" },
            { 0xD4, "INSTR" }, { 0xD5, "ELSE" }, { 0xD6, "RESUME" }, { 0xD7, "TRAP" },
            { 0xD8, "TRON" }, { 0xD9, "TROFF" }, { 0xDA, "SOUND" }, { 0xDB, "VOL" },
            { 0xDC, "AUTO" }, { 0xDD, "PUDEF" }, { 0xDE, "GRAPHIC" }, { 0xDF, "PAINT" },
            { 0xE0, "CHAR" }, { 0xE1, "BOX" }, { 0xE2, "CIRCLE" }, { 0xE3, "GSHAPE" },
            { 0xE4, "SSHAPE" }, { 0xE5, "DRAW" }, { 0xE6, "LOCATE" }, { 0xE7, "COLOR" },
            { 0xE8, "SCNCLR" }, { 0xE9, "SCALE" }, { 0xEA, "HELP" }, { 0xEB, "DO" },
            { 0xEC, "LOOP" }, { 0xED, "EXIT" }, { 0xEE, "DIRECTORY" }, { 0xEF, "DSAVE" },
            { 0xF0, "DLOAD" }, { 0xF1, "HEADER" }, { 0xF2, "SCRATCH" }, { 0xF3, "COLLECT" },
            { 0xF4, "COPY" }, { 0xF5, "RENAME" }, { 0xF6, "BACKUP" }, { 0xF7, "DELETE" },
            { 0xF8, "RENUMBER" }, { 0xF9, "KEY" }, { 0xFA, "MONITOR" },
            { 0xFB, "USING" }, { 0xFC, "UNTIL" }, { 0xFD, "WHILE" },
            { 0xFF, "\u03C0" }
        };

        private static byte[] ReadAllBytesShared(string filePath)
        {
            using (var fs = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete))
            {
                byte[] buffer = new byte[fs.Length];
                int offset = 0;
                while (offset < buffer.Length)
                {
                    int read = fs.Read(buffer, offset, buffer.Length - offset);
                    if (read == 0) break;
                    offset += read;
                }
                return buffer;
            }
        }

        private static int GetSectorsPerTrack(int track)
        {
            if (track <= 17) return 21;
            if (track <= 24) return 19;
            if (track <= 30) return 18;
            return 17;
        }

        private static byte[] EncodeCbmName(string text, int length, byte padByte)
        {
            byte[] result = new byte[length];
            for (int i = 0; i < length; i++) result[i] = padByte;

            string upper = (text ?? string.Empty).ToUpperInvariant();
            int count = Math.Min(length, upper.Length);
            for (int i = 0; i < count; i++)
            {
                char c = upper[i];
                result[i] = (c >= 0x20 && c <= 0x5E) ? (byte)c : (byte)'_';
            }
            return result;
        }

        private static byte GetFileTypeCode(string fileType)
        {
            string clean = (fileType ?? "PRG").Replace("*", "").Trim().ToUpperInvariant();
            return clean switch
            {
                "DEL" => 0,
                "SEQ" => 1,
                "PRG" => 2,
                "USR" => 3,
                "REL" => 4,
                _ => 2
            };
        }

        public static void CreateEmptyD64(string filePath, string diskName, string diskId = "01")
        {
            const int d64Size = 174848;
            byte[] d64 = new byte[d64Size];

            int bamOffset = GetD64Offset(18, 0);
            d64[bamOffset + 0x00] = 18;
            d64[bamOffset + 0x01] = 1;
            d64[bamOffset + 0x02] = 0x41;
            d64[bamOffset + 0x03] = 0x00;

            for (int track = 1; track <= 35; track++)
            {
                int entry = bamOffset + (track * 4);
                int sectors = GetSectorsPerTrack(track);

                if (track == 18)
                {
                    d64[entry + 0] = (byte)(sectors - 2);
                    d64[entry + 1] = 0xFC;
                    d64[entry + 2] = 0xFF;
                    d64[entry + 3] = 0x07;
                }
                else
                {
                    d64[entry + 0] = (byte)sectors;
                    d64[entry + 1] = 0xFF;
                    d64[entry + 2] = 0xFF;
                    d64[entry + 3] = sectors switch
                    {
                        21 => 0x1F,
                        19 => 0x07,
                        18 => 0x03,
                        _ => 0x01
                    };
                }
            }

            byte[] nameBytes = EncodeCbmName(diskName, 16, 0xA0);
            Array.Copy(nameBytes, 0, d64, bamOffset + 0x90, 16);

            d64[bamOffset + 0xA0] = 0xA0;
            d64[bamOffset + 0xA1] = 0xA0;

            byte[] idBytes = EncodeCbmName(string.IsNullOrWhiteSpace(diskId) ? "01" : diskId, 2, 0xA0);
            d64[bamOffset + 0xA2] = idBytes[0];
            d64[bamOffset + 0xA3] = idBytes[1];

            d64[bamOffset + 0xA4] = 0xA0;
            d64[bamOffset + 0xA5] = (byte)'2';
            d64[bamOffset + 0xA6] = (byte)'A';
            d64[bamOffset + 0xA7] = 0xA0;
            d64[bamOffset + 0xA8] = 0xA0;
            d64[bamOffset + 0xA9] = 0xA0;
            d64[bamOffset + 0xAA] = 0xA0;

            int dirOffset = GetD64Offset(18, 1);
            d64[dirOffset + 0x00] = 0x00;
            d64[dirOffset + 0x01] = 0xFF;

            File.WriteAllBytes(filePath, d64);
        }

        public static void CreateEmptyT64(string filePath, string tapeName)
        {
            SaveT64File(filePath, tapeName, new List<DiskFileItem>());
        }

        private static bool IsSectorFree(byte[] d64, int track, int sector)
        {
            if (track < 1 || track > 35 || sector < 0 || sector >= GetSectorsPerTrack(track))
                return false;

            int bamEntry = 91392 + (track * 4);
            byte mask = (byte)(1 << (sector % 8));
            return (d64[bamEntry + 1 + (sector / 8)] & mask) != 0;
        }

        private static void SetSectorAllocated(byte[] d64, int track, int sector)
        {
            if (!IsSectorFree(d64, track, sector)) return;
            int bamEntry = 91392 + (track * 4);
            byte mask = (byte)(1 << (sector % 8));
            d64[bamEntry + 1 + (sector / 8)] &= (byte)~mask;
            if (d64[bamEntry] > 0) d64[bamEntry]--;
        }

        private static void SetSectorFree(byte[] d64, int track, int sector)
        {
            if (track < 1 || track > 35 || sector < 0 || sector >= GetSectorsPerTrack(track))
                return;
            if (IsSectorFree(d64, track, sector)) return;

            int bamEntry = 91392 + (track * 4);
            byte mask = (byte)(1 << (sector % 8));
            d64[bamEntry + 1 + (sector / 8)] |= mask;
            d64[bamEntry]++;
        }

        private static void FreeSectorChain(byte[] d64, byte startTrack, byte startSector)
        {
            var visited = new HashSet<(int, int)>();
            byte track = startTrack;
            byte sector = startSector;

            while (track >= 1 && track <= 35 && visited.Add((track, sector)))
            {
                int offset = GetD64Offset(track, sector);
                if (offset < 0 || offset + 256 > d64.Length) break;

                byte nextTrack = d64[offset];
                byte nextSector = d64[offset + 1];

                SetSectorFree(d64, track, sector);

                track = nextTrack;
                sector = nextSector;
            }
        }

        private static readonly int[] DataTrackOrder = BuildDataTrackOrder();

        private static int[] BuildDataTrackOrder()
        {
            // 1541 DOS: a 17-es sávtól lefelé az 1-esig, majd a 19-estől felfelé a 35-ösig
            var order = new List<int>();
            for (int t = 17; t >= 1; t--) order.Add(t);
            for (int t = 19; t <= 35; t++) order.Add(t);
            return order.ToArray();
        }

        /// <summary>
        /// Adatszektort foglal interleave-vel (alapból 10). Az előző szektor (prevTrack/prevSector)
        /// alapján keresi a következőt; ha prevTrack == 0, a 17-es sáv elejétől indul.
        /// </summary>
        private static bool TryAllocateDataSector(byte[] d64, int prevTrack, int prevSector,
                                                  out byte outTrack, out byte outSector)
        {
            int startIdx = 0;
            bool hasPrev = false;
            if (prevTrack >= 1)
            {
                int idx = Array.IndexOf(DataTrackOrder, prevTrack);
                if (idx >= 0)
                {
                    startIdx = idx;
                    hasPrev = true;
                }
            }

            for (int i = startIdx; i < DataTrackOrder.Length; i++)
            {
                int t = DataTrackOrder[i];
                int count = GetSectorsPerTrack(t);
                int s0 = (i == startIdx && hasPrev) ? (prevSector + DataInterleave) % count : 0;

                for (int k = 0; k < count; k++)
                {
                    int s = (s0 + k) % count;
                    if (IsSectorFree(d64, t, s))
                    {
                        SetSectorAllocated(d64, t, s);
                        outTrack = (byte)t;
                        outSector = (byte)s;
                        return true;
                    }
                }
            }

            // ha a végére értünk, a korábbi sávokat is átnézzük
            for (int i = 0; i < startIdx; i++)
            {
                int t = DataTrackOrder[i];
                int count = GetSectorsPerTrack(t);
                for (int s = 0; s < count; s++)
                {
                    if (IsSectorFree(d64, t, s))
                    {
                        SetSectorAllocated(d64, t, s);
                        outTrack = (byte)t;
                        outSector = (byte)s;
                        return true;
                    }
                }
            }

            outTrack = 0;
            outSector = 0;
            return false;
        }

        private static bool TryAllocateDirectorySector(byte[] d64, out byte outSector)
        {
            int count = GetSectorsPerTrack(18);
            for (int s = 1; s < count; s++)
            {
                if (IsSectorFree(d64, 18, s))
                {
                    SetSectorAllocated(d64, 18, s);
                    outSector = (byte)s;
                    return true;
                }
            }
            outSector = 0;
            return false;
        }

        private static int CountFreeDataSectors(byte[] d64)
        {
            int free = 0;
            for (int t = 1; t <= 35; t++)
            {
                if (t == 18) continue;
                free += d64[91392 + (t * 4)];
            }
            return free;
        }

        public static void DeleteFileFromD64(string filePath, int activeFileIndex)
        {
            byte[] d64 = ReadAllBytesShared(filePath);
            int bamOffset = 91392;
            byte currentTrack = d64[bamOffset];
            byte currentSector = d64[bamOffset + 1];

            int currentActiveIdx = 0;
            var visited = new HashSet<(int, int)>();

            while (currentTrack != 0 && currentTrack <= 35 && visited.Add((currentTrack, currentSector)))
            {
                int sectorOffset = GetD64Offset(currentTrack, currentSector);
                if (sectorOffset < 0 || sectorOffset + 256 > d64.Length) break;

                byte nextTrack = d64[sectorOffset];
                byte nextSector = d64[sectorOffset + 1];

                for (int i = 0; i < 8; i++)
                {
                    int entryOffset = sectorOffset + (i * 32);
                    if (entryOffset + 32 > d64.Length) break;

                    byte fileTypeRaw = d64[entryOffset + 2];
                    if (fileTypeRaw == 0x00) continue;

                    if (currentActiveIdx == activeFileIndex)
                    {
                        byte startTrack = d64[entryOffset + 3];
                        byte startSector = d64[entryOffset + 4];
                        if (startTrack != 0)
                        {
                            FreeSectorChain(d64, startTrack, startSector);
                        }

                        if ((fileTypeRaw & 0x07) == 4)
                        {
                            byte ssTrack = d64[entryOffset + 21];
                            byte ssSector = d64[entryOffset + 22];
                            if (ssTrack != 0)
                            {
                                FreeSectorChain(d64, ssTrack, ssSector);
                            }
                        }

                        d64[entryOffset + 2] = 0x00;
                        File.WriteAllBytes(filePath, d64);
                        return;
                    }

                    currentActiveIdx++;
                }

                currentTrack = nextTrack;
                currentSector = nextSector;
            }

            throw new InvalidOperationException("Selected file entry was not found in the D64 directory.");
        }

        public static void AddFileToD64(
            string filePath,
            string fileName,
            byte[]? rawNameBytes,
            string fileType,
            byte[] rawData,
            int recordLength = 0,
            bool overwriteExisting = false)
        {
            byte[] d64 = ReadAllBytesShared(filePath);
            if (d64.Length < 174848)
            {
                throw new InvalidDataException("Invalid or truncated D64 disk image.");
            }

            string targetName = (fileName ?? "UNNAMED").Trim().ToUpperInvariant();
            if (targetName.Length > 16) targetName = targetName.Substring(0, 16);

            int bamOffset = 91392;
            byte curTrack = d64[bamOffset];
            byte curSector = d64[bamOffset + 1];
            var visited = new HashSet<(int, int)>();

            while (curTrack != 0 && curTrack <= 35 && visited.Add((curTrack, curSector)))
            {
                int sectorOffset = GetD64Offset(curTrack, curSector);
                if (sectorOffset < 0 || sectorOffset + 256 > d64.Length) break;

                for (int i = 0; i < 8; i++)
                {
                    int entryOffset = sectorOffset + (i * 32);
                    byte fileTypeRaw = d64[entryOffset + 2];
                    if (fileTypeRaw == 0x00) continue;

                    byte[] existingNameBytes = new byte[16];
                    Array.Copy(d64, entryOffset + 5, existingNameBytes, 0, 16);
                    string existingName = CbmBytesToString(existingNameBytes);

                    if (existingName.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!overwriteExisting)
                        {
                            throw new IOException($"File '{targetName}' already exists on this disk image.");
                        }

                        byte st = d64[entryOffset + 3];
                        byte ss = d64[entryOffset + 4];
                        if (st != 0) FreeSectorChain(d64, st, ss);

                        if ((fileTypeRaw & 0x07) == 4)
                        {
                            byte sst = d64[entryOffset + 21];
                            byte sss = d64[entryOffset + 22];
                            if (sst != 0) FreeSectorChain(d64, sst, sss);
                        }

                        d64[entryOffset + 2] = 0x00;
                    }
                }

                curTrack = d64[sectorOffset];
                curSector = d64[sectorOffset + 1];
            }

            byte typeCode = GetFileTypeCode(fileType);
            bool isRel = (typeCode == 4);
            int effectiveRecLen = isRel ? Math.Clamp(recordLength > 0 ? recordLength : 32, 1, 254) : 0;

            int dataSectorsNeeded = Math.Max(1, (int)Math.Ceiling(rawData.Length / 254.0));
            int sideSectorsNeeded = isRel ? Math.Max(1, (int)Math.Ceiling(dataSectorsNeeded / 120.0)) : 0;

            if (isRel && sideSectorsNeeded > 6)
            {
                throw new InvalidOperationException("REL file is too large for standard 1541 side-sector limit (max 6 side-sectors).");
            }

            if (CountFreeDataSectors(d64) < (dataSectorsNeeded + sideSectorsNeeded))
            {
                throw new InvalidOperationException("Not enough free blocks on the D64 disk image.");
            }

            int freeEntryOffset = -1;
            int lastDirSectorOffset = -1;
            curTrack = d64[bamOffset];
            curSector = d64[bamOffset + 1];
            visited.Clear();

            while (curTrack != 0 && curTrack <= 35 && visited.Add((curTrack, curSector)))
            {
                int sectorOffset = GetD64Offset(curTrack, curSector);
                if (sectorOffset < 0 || sectorOffset + 256 > d64.Length) break;
                lastDirSectorOffset = sectorOffset;

                for (int i = 0; i < 8; i++)
                {
                    int entryOffset = sectorOffset + (i * 32);
                    if (d64[entryOffset + 2] == 0x00)
                    {
                        freeEntryOffset = entryOffset;
                        break;
                    }
                }

                if (freeEntryOffset >= 0) break;

                curTrack = d64[sectorOffset];
                curSector = d64[sectorOffset + 1];
            }

            if (freeEntryOffset < 0)
            {
                if (lastDirSectorOffset < 0 || !TryAllocateDirectorySector(d64, out byte newDirSector))
                {
                    throw new InvalidOperationException("D64 directory is full (maximum 144 files reached).");
                }

                d64[lastDirSectorOffset + 0] = 18;
                d64[lastDirSectorOffset + 1] = newDirSector;

                int newDirOffset = GetD64Offset(18, newDirSector);
                Array.Clear(d64, newDirOffset, 256);
                d64[newDirOffset + 0] = 0x00;
                d64[newDirOffset + 1] = 0xFF;
                freeEntryOffset = newDirOffset;
            }

            var dataSectors = new List<(byte Track, byte Sector)>(dataSectorsNeeded);
            int prevT = 0, prevS = 0;
            for (int i = 0; i < dataSectorsNeeded; i++)
            {
                if (!TryAllocateDataSector(d64, prevT, prevS, out byte t, out byte s))
                {
                    throw new InvalidOperationException("Failed to allocate data sector on D64 disk.");
                }
                dataSectors.Add((t, s));
                prevT = t;
                prevS = s;
            }

            for (int i = 0; i < dataSectors.Count; i++)
            {
                int secOffset = GetD64Offset(dataSectors[i].Track, dataSectors[i].Sector);
                Array.Clear(d64, secOffset, 256);

                int dataStart = i * 254;
                int bytesToWrite = Math.Min(254, Math.Max(0, rawData.Length - dataStart));

                if (i < dataSectors.Count - 1)
                {
                    d64[secOffset + 0] = dataSectors[i + 1].Track;
                    d64[secOffset + 1] = dataSectors[i + 1].Sector;
                }
                else
                {
                    d64[secOffset + 0] = 0x00;
                    d64[secOffset + 1] = (byte)(bytesToWrite > 0 ? bytesToWrite + 1 : 1);
                }

                if (bytesToWrite > 0)
                {
                    Array.Copy(rawData, dataStart, d64, secOffset + 2, bytesToWrite);
                }
            }

            byte firstSideTrack = 0;
            byte firstSideSector = 0;
            if (isRel)
            {
                var sideSectors = new List<(byte Track, byte Sector)>(sideSectorsNeeded);
                for (int i = 0; i < sideSectorsNeeded; i++)
                {
                    if (!TryAllocateDataSector(d64, 0, 0, out byte t, out byte s))
                    {
                        throw new InvalidOperationException("Failed to allocate side-sector on D64 disk.");
                    }
                    sideSectors.Add((t, s));
                }

                firstSideTrack = sideSectors[0].Track;
                firstSideSector = sideSectors[0].Sector;

                for (int i = 0; i < sideSectors.Count; i++)
                {
                    int ssOffset = GetD64Offset(sideSectors[i].Track, sideSectors[i].Sector);
                    Array.Clear(d64, ssOffset, 256);

                    int chunkStart = i * 120;
                    int chunkCount = Math.Min(120, dataSectors.Count - chunkStart);

                    if (i < sideSectors.Count - 1)
                    {
                        d64[ssOffset + 0] = sideSectors[i + 1].Track;
                        d64[ssOffset + 1] = sideSectors[i + 1].Sector;
                    }
                    else
                    {
                        d64[ssOffset + 0] = 0x00;
                        d64[ssOffset + 1] = (byte)(15 + (chunkCount * 2));
                    }

                    d64[ssOffset + 2] = (byte)i;
                    d64[ssOffset + 3] = (byte)effectiveRecLen;

                    for (int k = 0; k < sideSectors.Count; k++)
                    {
                        d64[ssOffset + 4 + (k * 2)] = sideSectors[k].Track;
                        d64[ssOffset + 5 + (k * 2)] = sideSectors[k].Sector;
                    }

                    for (int k = 0; k < chunkCount; k++)
                    {
                        d64[ssOffset + 16 + (k * 2)] = dataSectors[chunkStart + k].Track;
                        d64[ssOffset + 17 + (k * 2)] = dataSectors[chunkStart + k].Sector;
                    }
                }
            }

            for (int b = 2; b < 32; b++) d64[freeEntryOffset + b] = 0x00;

            d64[freeEntryOffset + 2] = (byte)(0x80 | typeCode);
            d64[freeEntryOffset + 3] = dataSectors[0].Track;
            d64[freeEntryOffset + 4] = dataSectors[0].Sector;

            byte[] entryName = (rawNameBytes != null && rawNameBytes.Length == 16)
                ? rawNameBytes
                : EncodeCbmName(targetName, 16, 0xA0);
            Array.Copy(entryName, 0, d64, freeEntryOffset + 5, 16);

            d64[freeEntryOffset + 21] = firstSideTrack;
            d64[freeEntryOffset + 22] = firstSideSector;
            d64[freeEntryOffset + 23] = (byte)effectiveRecLen;

            int totalBlocks = dataSectorsNeeded + sideSectorsNeeded;
            d64[freeEntryOffset + 30] = (byte)(totalBlocks & 0xFF);
            d64[freeEntryOffset + 31] = (byte)((totalBlocks >> 8) & 0xFF);

            File.WriteAllBytes(filePath, d64);
        }

        public static void DeleteFileFromT64(string filePath, int activeFileIndex)
        {
            DiskHeaderInfo header = ReadT64Header(filePath);
            List<DiskFileItem> files = ReadT64File(filePath);

            if (activeFileIndex < 0 || activeFileIndex >= files.Count)
            {
                throw new InvalidOperationException("Selected file entry was not found in the T64 tape image.");
            }

            files.RemoveAt(activeFileIndex);
            SaveT64File(filePath, header.DiskName, files);
        }

        public static void AddFileToT64(
            string filePath,
            string fileName,
            byte[]? rawNameBytes,
            string fileType,
            byte[] rawData,
            bool overwriteExisting = false)
        {
            DiskHeaderInfo header = ReadT64Header(filePath);
            List<DiskFileItem> files = ReadT64File(filePath);

            string targetName = (fileName ?? "UNNAMED").Trim().ToUpperInvariant();
            if (targetName.Length > 16) targetName = targetName.Substring(0, 16);

            int existingIdx = files.FindIndex(f => f.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
            if (existingIdx >= 0)
            {
                if (!overwriteExisting)
                {
                    throw new IOException($"File '{targetName}' already exists in this T64 image.");
                }
                files.RemoveAt(existingIdx);
            }

            byte[] finalRawName = (rawNameBytes != null && rawNameBytes.Length == 16)
                ? rawNameBytes
                : EncodeCbmName(targetName, 16, 0x20);

            int startAddr = rawData.Length >= 2 ? (rawData[0] | (rawData[1] << 8)) : 0x1001;
            int payloadLen = Math.Max(0, rawData.Length - 2);

            files.Add(new DiskFileItem
            {
                Name = targetName,
                RawNameBytes = finalRawName,
                Type = string.IsNullOrWhiteSpace(fileType) ? "PRG" : fileType.Replace("*", "").Trim().ToUpperInvariant(),
                StartAddress = startAddr,
                EndAddress = startAddr + payloadLen - 1,
                SizeInBytes = rawData.Length,
                BlockSize = Math.Max(1, (int)Math.Ceiling(rawData.Length / 254.0)),
                RawData = rawData,
                IsVirtual = true
            });

            SaveT64File(filePath, header.DiskName, files);
        }

        private static void SaveT64File(string filePath, string tapeName, List<DiskFileItem> files)
        {
            int maxEntries = Math.Max(30, files.Count);
            int usedEntries = files.Count;
            int headerAndDirSize = 64 + (maxEntries * 32);

            using (var ms = new MemoryStream())
            {
                byte[] header = new byte[64];
                byte[] sig = Encoding.ASCII.GetBytes("C64 tape image file");
                Array.Copy(sig, 0, header, 0, sig.Length);

                header[0x20] = 0x00;
                header[0x21] = 0x01;
                header[0x22] = (byte)(maxEntries & 0xFF);
                header[0x23] = (byte)((maxEntries >> 8) & 0xFF);
                header[0x24] = (byte)(usedEntries & 0xFF);
                header[0x25] = (byte)((usedEntries >> 8) & 0xFF);

                byte[] tapeNameBytes = EncodeCbmName(tapeName, 24, 0x20);
                Array.Copy(tapeNameBytes, 0, header, 0x28, 24);
                ms.Write(header, 0, header.Length);

                byte[] dirTable = new byte[maxEntries * 32];
                using (var dataStream = new MemoryStream())
                {
                    int currentDataOffset = headerAndDirSize;

                    for (int i = 0; i < files.Count; i++)
                    {
                        var item = files[i];
                        int entryOffset = i * 32;

                        int startAddr = item.RawData.Length >= 2
                            ? (item.RawData[0] | (item.RawData[1] << 8))
                            : 0x1001;

                        byte[] payload;
                        if (item.RawData.Length >= 2)
                        {
                            payload = new byte[item.RawData.Length - 2];
                            Array.Copy(item.RawData, 2, payload, 0, payload.Length);
                        }
                        else
                        {
                            payload = item.RawData;
                        }

                        int endAddr = Math.Min(0xFFFF, startAddr + payload.Length);
                        byte typeCode = GetFileTypeCode(item.Type);

                        dirTable[entryOffset + 0] = 0x01;
                        dirTable[entryOffset + 1] = (byte)(0x80 | typeCode);
                        dirTable[entryOffset + 2] = (byte)(startAddr & 0xFF);
                        dirTable[entryOffset + 3] = (byte)((startAddr >> 8) & 0xFF);
                        dirTable[entryOffset + 4] = (byte)(endAddr & 0xFF);
                        dirTable[entryOffset + 5] = (byte)((endAddr >> 8) & 0xFF);

                        dirTable[entryOffset + 8] = (byte)(currentDataOffset & 0xFF);
                        dirTable[entryOffset + 9] = (byte)((currentDataOffset >> 8) & 0xFF);
                        dirTable[entryOffset + 10] = (byte)((currentDataOffset >> 16) & 0xFF);
                        dirTable[entryOffset + 11] = (byte)((currentDataOffset >> 24) & 0xFF);

                        byte[] nameBytes = (item.RawNameBytes != null && item.RawNameBytes.Length == 16)
                            ? item.RawNameBytes
                            : EncodeCbmName(item.Name, 16, 0x20);

                        for (int k = 0; k < 16; k++)
                        {
                            dirTable[entryOffset + 16 + k] = (nameBytes[k] == 0xA0 || nameBytes[k] == 0x00)
                                ? (byte)0x20
                                : nameBytes[k];
                        }

                        dataStream.Write(payload, 0, payload.Length);
                        currentDataOffset += payload.Length;
                    }

                    ms.Write(dirTable, 0, dirTable.Length);
                    byte[] payloadAll = dataStream.ToArray();
                    ms.Write(payloadAll, 0, payloadAll.Length);
                }

                File.WriteAllBytes(filePath, ms.ToArray());
            }
        }


        private static readonly double[] YapeLumaVoltages = { 2.00, 2.40, 2.55, 2.70, 2.90, 3.30, 3.60, 4.10, 4.95 };

        private static readonly int[] YapeHueAngles =
        {
            -2, -1,
            103, // 2: Red
            283, // 3: Cyan
            53,  // 4: Magenta / Purple
            241, // 5: Green
            347, // 6: Blue
            167, // 7: Yellow
            123, // 8: Orange
            148, // 9: Brown
            195, // 10: Yellow-Green
            83,  // 11: Pink
            265, // 12: Blue-Green
            323, // 13: Light Blue
            0,   // 14: Dark Blue (YAPE note: 23 original, but wrong -> 0)
            213  // 15: Light Green
        };

        private static readonly Color[,] TedPalette = BuildTedPalette();

        private static Color[,] BuildTedPalette()
        {
            // [hue 0..15, luma 0..7] — a képen LUM 0 az alsó sor, LUM 7 a felső sor
            var table = new Color[16, 8];

            // 0: BLACK
            for (int luma = 0; luma < 8; luma++)
                table[0, luma] = Color.FromArgb(0, 0, 0);

            // 1: WHITE (szürke/fehér oszlop, LUM 0 -> LUM 7) OK
            Color[] whiteCol =
            {
                Color.FromArgb(34, 34, 34),   // LUM 0
                Color.FromArgb(47, 47, 47),   // LUM 1
                Color.FromArgb(60, 60, 60),   // LUM 2
                Color.FromArgb(77, 77, 77),   // LUM 3
                Color.FromArgb(110, 110, 110), // LUM 4
                Color.FromArgb(136, 136, 136), // LUM 5
                Color.FromArgb(178, 178, 178), // LUM 6
                Color.FromArgb(238, 238, 238),  // LUM 7
            };

            // 2: RED OK
            Color[] redCol =
            {
                Color.FromArgb(91, 10, 11),
                Color.FromArgb(103, 22, 23),
                Color.FromArgb(116, 35, 36),
                Color.FromArgb(133, 52, 53),
                Color.FromArgb(167, 86, 87),
                Color.FromArgb(193, 112, 113),
                Color.FromArgb(235, 154, 155),
                Color.FromArgb(255, 214, 215),
            };

            // 3: CYAN OK
            Color[] cyanCol =
            {
                Color.FromArgb(0, 58, 57),
                Color.FromArgb(0, 71, 70),
                Color.FromArgb(3, 84, 83),
                Color.FromArgb(20, 101, 100),
                Color.FromArgb(54, 135, 134),
                Color.FromArgb(79, 160, 159),
                Color.FromArgb(122, 203, 202),
                Color.FromArgb(181, 255, 255)
            };

            // 4: PURPLE OK
            Color[] purpleCol =
            {
                Color.FromArgb(80, 0, 96),
                Color.FromArgb(93, 11, 109),
                Color.FromArgb(106, 24, 122),
                Color.FromArgb(123, 41, 139),
                Color.FromArgb(157, 75, 173),
                Color.FromArgb(182, 100, 198),
                Color.FromArgb(225, 143, 241),
                Color.FromArgb(255, 202, 255)
            };

            // 5: GREEN OK
            Color[] greenCol =
            {
                Color.FromArgb(0, 70, 0),
                Color.FromArgb(0, 82, 0),
                Color.FromArgb(9, 95, 9),
                Color.FromArgb(26, 112, 26),
                Color.FromArgb(60, 146, 60),
                Color.FromArgb(85, 172, 86),
                Color.FromArgb(128, 214, 128),
                Color.FromArgb(187, 255, 188)
            };

            // 6: BLUE OK
            Color[] blueCol =
            {
                Color.FromArgb(21, 21, 135),
                Color.FromArgb(34, 34, 148),
                Color.FromArgb(46, 47, 160),
                Color.FromArgb(63, 64, 177),
                Color.FromArgb(97, 98, 211),
                Color.FromArgb(123, 123, 237),
                Color.FromArgb(165, 166, 255),
                Color.FromArgb(225, 225, 255)
            };

            // 7: YELLOW OK
            Color[] yellowCol =
            {
                Color.FromArgb(47, 47, 0),
                Color.FromArgb(60, 60, 0),
                Color.FromArgb(73, 72, 0),
                Color.FromArgb(90, 89, 0),
                Color.FromArgb(124, 123, 10),
                Color.FromArgb(149, 149, 35),
                Color.FromArgb(192, 191, 78),
                Color.FromArgb(251, 251, 137)
            };

            // 8: ORANGE OK
            Color[] orangeCol =
            {
                Color.FromArgb(79, 24, 0),   // LUM 0
                Color.FromArgb(92, 36, 0),   // LUM 1
                Color.FromArgb(105, 49, 0),   // LUM 2
                Color.FromArgb(122, 66, 11),   // LUM 3
                Color.FromArgb(156, 100, 45),   // LUM 4
                Color.FromArgb(181, 126, 71),   // LUM 5
                Color.FromArgb(224, 168, 113),   // LUM 6
                Color.FromArgb(255, 228, 173),   // LUM 7
            };

            // 9: BROWN OK
            Color[] brownCol =
            {
                Color.FromArgb(65, 35, 0),   // LUM 0
                Color.FromArgb(78, 48, 0),   // LUM 1
                Color.FromArgb(90, 61, 0),   // LUM 2
                Color.FromArgb(107, 78, 0),   // LUM 3
                Color.FromArgb(141, 112, 23),   // LUM 4
                Color.FromArgb(167, 137, 48),   // LUM 5
                Color.FromArgb(209, 180, 91),   // LUM 6
                Color.FromArgb(255, 239, 150),   // LUM 7
            };

            // 10: YELLOW GREEN OK
            Color[] yellowGreenCol =
            {
                Color.FromArgb(19, 61, 0),   // LUM 0
                Color.FromArgb(32, 74, 0),   // LUM 1
                Color.FromArgb(44, 87, 0),   // LUM 2
                Color.FromArgb(61, 104, 0),   // LUM 3
                Color.FromArgb(95, 138, 10),   // LUM 4
                Color.FromArgb(121, 163, 36),   // LUM 5
                Color.FromArgb(163, 206, 78),   // LUM 6
                Color.FromArgb(223, 255, 138),   // LUM 7
            };

            // 11: PINK OK
            Color[] pinkCol =
            {
                Color.FromArgb(92, 2, 47),   // LUM 0
                Color.FromArgb(104, 15, 59),   // LUM 1
                Color.FromArgb(117, 28, 72),   // LUM 2
                Color.FromArgb(134, 45, 89),   // LUM 3
                Color.FromArgb(168, 79, 123),   // LUM 4
                Color.FromArgb(194, 104, 149),   // LUM 5
                Color.FromArgb(236, 147, 191),   // LUM 6
                Color.FromArgb(255, 206, 251),   // LUM 7
            };

            // 12: BLUE GREEN OK
            Color[] blueGreenCol =
            {
                Color.FromArgb(0, 65, 25),   // LUM 0
                Color.FromArgb(0, 78, 38),   // LUM 1
                Color.FromArgb(2, 91, 50),   // LUM 2
                Color.FromArgb(19, 108, 67),   // LUM 3
                Color.FromArgb(53, 142, 101),   // LUM 4
                Color.FromArgb(78, 167, 127),   // LUM 5
                Color.FromArgb(121, 210, 169),   // LUM 6
                Color.FromArgb(180, 255, 229),   // LUM 7
            };

            // 13: LIGHT BLUE OK
            Color[] lightBlueCol =
            {
                Color.FromArgb(0, 36, 117),   // LUM 0
                Color.FromArgb(12, 48, 130),   // LUM 1
                Color.FromArgb(25, 61, 142),   // LUM 2
                Color.FromArgb(42, 78, 159),   // LUM 3
                Color.FromArgb(76, 112, 193),   // LUM 4
                Color.FromArgb(101, 138, 219),   // LUM 5
                Color.FromArgb(144, 180, 255),   // LUM 6
                Color.FromArgb(203, 240, 255),   // LUM 7
            };

            // 14: DK BLUE OK
            Color[] darkBlueCol =
            {
                Color.FromArgb(34, 14, 138),
                Color.FromArgb(47, 27, 150),
                Color.FromArgb(60, 39, 163),
                Color.FromArgb(77, 56, 180),
                Color.FromArgb(110, 90, 214),
                Color.FromArgb(136, 116, 240),
                Color.FromArgb(178, 158, 255),
                Color.FromArgb(238, 218, 255)
            };

            // 15: LT GREEN OK
            Color[] lightGreenCol =
            {
                Color.FromArgb(2, 67, 0),   // LUM 0
                Color.FromArgb(15, 80, 0),   // LUM 1
                Color.FromArgb(28, 93, 0),   // LUM 2
                Color.FromArgb(45, 110, 0),   // LUM 3
                Color.FromArgb(79, 144, 24),   // LUM 4
                Color.FromArgb(104, 169, 49),   // LUM 5
                Color.FromArgb(147, 212, 92),   // LUM 6
                Color.FromArgb(206, 255, 151),   // LUM 7
            };

            Color[][] columns =
            {
                whiteCol, redCol, cyanCol, purpleCol, greenCol, blueCol, yellowCol,
                orangeCol, brownCol, yellowGreenCol, pinkCol, blueGreenCol,
                lightBlueCol, darkBlueCol, lightGreenCol
            };

            for (int hue = 1; hue < 16; hue++)
            {
                for (int luma = 0; luma < 8; luma++)
                {
                    table[hue, luma] = columns[hue - 1][luma];
                }
            }

            return table;
        }

        private static Color GetTedColor(int hue, int luma)
        {
            return TedPalette[hue & 0x0F, luma & 0x07];
        }

        private static Color GetTedColorFromRegisterByte(byte tedByte)
        {
            int hue = tedByte & 0x0F;
            int luma = (tedByte >> 4) & 0x07;

            return GetTedColor(hue, luma);
        }

        private static bool TryGetMultiBColors(byte[] rawData, int headerOffset, out Color bg0, out Color bg1)
        {
            bg0 = Color.Black;
            bg1 = Color.LightGray;

            int[] candidateSigOffsets = { headerOffset + 1018, headerOffset + 1020 };
            foreach (int sigOff in candidateSigOffsets)
            {
                if (sigOff >= 0 && sigOff + 5 < rawData.Length
                    && rawData[sigOff] == (byte)'M'
                    && rawData[sigOff + 1] == (byte)'U'
                    && rawData[sigOff + 2] == (byte)'L'
                    && rawData[sigOff + 3] == (byte)'T')
                {
                    byte rawFf16 = rawData[sigOff + 4];
                    byte rawFf15 = rawData[sigOff + 5];

                    byte ff16 = (byte)(((rawFf16 & 0x0F) << 4) | (rawFf16 >> 4));
                    byte ff15 = (byte)(((rawFf15 & 0x0F) << 4) | (rawFf15 >> 4));

                    bg1 = GetTedColorFromRegisterByte(ff16);
                    bg0 = GetTedColorFromRegisterByte(ff15);
                    return true;
                }
            }

            return false;
        }

        public static Bitmap RenderScreenBitmap(byte[] rawData, bool multicolor, ImagePaletteMode paletteMode = ImagePaletteMode.Auto)
        {
            const int width = 320;
            const int height = 200;
            Bitmap bmp = new Bitmap(width, height);

            try
            {
                int[] pixels = new int[width * height];
                Array.Fill(pixels, unchecked((int)0xFF000000)); // opak fekete alap

                bool hasColorTables = rawData.Length >= 10050;
                int startAddr = rawData.Length >= 2 ? (rawData[0] | (rawData[1] << 8)) : 0x2000;

                int lumaOffset;
                int colorOffset;
                int bitmapOffset;
                int payloadBaseOffset = 2;

                if (startAddr == 0x1800 || startAddr == 0x7800 || rawData.Length >= 10050)
                {
                    lumaOffset = payloadBaseOffset + 0x000;
                    colorOffset = payloadBaseOffset + 0x400;
                    bitmapOffset = payloadBaseOffset + 0x800;
                    hasColorTables = true;
                }
                else
                {
                    lumaOffset = payloadBaseOffset;
                    colorOffset = 1002;
                    bitmapOffset = hasColorTables ? 2050 : (rawData.Length >= 8002 ? 2 : 0);
                }

                Color bg0 = Color.Black;
                Color bg1 = Color.Gray;

                if (hasColorTables && paletteMode != ImagePaletteMode.Raw)
                {
                    bool foundMult = TryGetMultiBColors(rawData, payloadBaseOffset, out Color multBg0, out Color multBg1);

                    bool useMultiB = paletteMode == ImagePaletteMode.MultiB
                                  || (paletteMode == ImagePaletteMode.Auto && foundMult);

                    bool useBotticelli = paletteMode == ImagePaletteMode.Botticelli
                                      || (paletteMode == ImagePaletteMode.Auto && !foundMult && rawData.Length > 1003 && (rawData[1002] != 0 || rawData[1003] != 0));

                    if (useMultiB)
                    {
                        if (foundMult)
                        {
                            bg0 = multBg0;
                            bg1 = multBg1;
                        }
                        else if (rawData.Length > payloadBaseOffset + 1023)
                        {
                            byte rawFf16 = rawData[payloadBaseOffset + 1022];
                            byte rawFf15 = rawData[payloadBaseOffset + 1023];
                            byte ff16 = (byte)(((rawFf16 & 0x0F) << 4) | (rawFf16 >> 4));
                            byte ff15 = (byte)(((rawFf15 & 0x0F) << 4) | (rawFf15 >> 4));
                            bg1 = GetTedColorFromRegisterByte(ff16);
                            bg0 = GetTedColorFromRegisterByte(ff15);
                        }
                    }
                    else if (useBotticelli && rawData.Length > 1003)
                    {
                        if (rawData.Length > 10050)
                        {
                            // $3F40: a Botticelli formátum háttérbájtja
                            bg0 = GetTedColorFromRegisterByte(rawData[10050]);
                            bg1 = bg0;
                        }
                        else
                        {
                            bg0 = GetTedColorFromRegisterByte(rawData[1002]);
                            bg1 = GetTedColorFromRegisterByte(rawData[1003]);
                        }
                    }
                }

                for (int row = 0; row < 25; row++)
                {
                    for (int col = 0; col < 40; col++)
                    {
                        int cellIndex = row * 40 + col;
                        int cellBitmapOffset = bitmapOffset + cellIndex * 8;
                        if (cellBitmapOffset + 8 > rawData.Length) break;

                        Color fgColor = Color.SpringGreen;
                        Color cellBgColor = Color.Black;
                        Color mcColor1 = bg1;
                        Color mcColor2 = Color.Cyan;
                        Color mcColor3 = Color.Yellow;

                        if (hasColorTables && (lumaOffset + cellIndex < rawData.Length) && (colorOffset + cellIndex < rawData.Length))
                        {
                            byte lumaByte = rawData[lumaOffset + cellIndex];
                            byte colorByte = rawData[colorOffset + cellIndex];

                            // A luma bájt alsó nibble-je tartozik a színbájt felső nibble-jéhez (hue),
                            // a felső nibble pedig az alsóhoz. (Ellenőrizve a Dan Dare képpel.)
                            int lumaHigh = lumaByte & 0x07;
                            int lumaLow = (lumaByte >> 4) & 0x07;

                            int hueHigh = (colorByte >> 4) & 0x0F;
                            int hueLow = colorByte & 0x0F;

                            if (!multicolor)
                            {
                                fgColor = GetTedColor(hueHigh, lumaHigh);
                                cellBgColor = GetTedColor(hueLow, lumaLow);
                            }
                            else
                            {
                                mcColor1 = GetTedColor(hueHigh, lumaHigh);
                                mcColor2 = GetTedColor(hueLow, lumaLow);
                                mcColor3 = bg1;
                            }
                        }

                        for (int y = 0; y < 8; y++)
                        {
                            int py = row * 8 + y;
                            if (py >= height) continue;

                            byte b = rawData[cellBitmapOffset + y];
                            int rowBase = py * width + col * 8;

                            if (!multicolor)
                            {
                                int fg = fgColor.ToArgb();
                                int bgc = cellBgColor.ToArgb();
                                for (int x = 0; x < 8; x++)
                                {
                                    bool bitOn = (b & (1 << (7 - x))) != 0;
                                    pixels[rowBase + x] = bitOn ? fg : bgc;
                                }
                            }
                            else
                            {
                                for (int bitPair = 0; bitPair < 4; bitPair++)
                                {
                                    int code = (b >> (6 - bitPair * 2)) & 0x03;
                                    Color c = code switch
                                    {
                                        0 => bg0,
                                        1 => mcColor1,
                                        2 => mcColor2,
                                        3 => mcColor3,
                                        _ => bg0
                                    };
                                    int argb = c.ToArgb();
                                    pixels[rowBase + bitPair * 2] = argb;
                                    pixels[rowBase + bitPair * 2 + 1] = argb;
                                }
                            }
                        }
                    }
                }

                var data = bmp.LockBits(new Rectangle(0, 0, width, height),
                                        ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                try
                {
                    Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
                }
                finally
                {
                    bmp.UnlockBits(data);
                }

                return bmp;
            }
            catch
            {
                bmp.Dispose();
                throw;
            }
        }

        public static string Disassemble6502(byte[] prgData, int maxInstructions = 400)
        {
            if (prgData.Length < 3) return "File is too short to disassemble.";

            int startAddr = prgData[0] | (prgData[1] << 8);
            int index = 2;
            int pc = startAddr;
            int count = 0;

            StringBuilder sb = new StringBuilder();

            while (index < prgData.Length && count < maxInstructions)
            {
                byte op = prgData[index];
                var (mnemonic, mode) = OpcodeTable[op];

                int instrLen = mode switch
                {
                    AddrMode.IMP or AddrMode.ACC => 1,
                    AddrMode.IMM or AddrMode.ZP or AddrMode.ZPX or AddrMode.ZPY or AddrMode.IZX or AddrMode.IZY or AddrMode.REL => 2,
                    AddrMode.ABS or AddrMode.ABX or AddrMode.ABY or AddrMode.IND => 3,
                    _ => 1
                };

                if (index + instrLen > prgData.Length)
                {
                    sb.AppendLine($"${pc:X4}:  {op:X2}        .BYTE ${op:X2}");
                    break;
                }

                string hexBytes = instrLen switch
                {
                    1 => $"{op:X2}      ",
                    2 => $"{op:X2} {prgData[index + 1]:X2}   ",
                    3 => $"{op:X2} {prgData[index + 1]:X2} {prgData[index + 2]:X2}",
                    _ => $"{op:X2}      "
                };

                string operand = string.Empty;
                if (mnemonic == "???")
                {
                    mnemonic = ".BYTE";
                    operand = $"${op:X2}";
                }
                else
                {
                    operand = mode switch
                    {
                        AddrMode.IMP => "",
                        AddrMode.ACC => "A",
                        AddrMode.IMM => $"#${prgData[index + 1]:X2}",
                        AddrMode.ZP  => $"${prgData[index + 1]:X2}",
                        AddrMode.ZPX => $"${prgData[index + 1]:X2},X",
                        AddrMode.ZPY => $"${prgData[index + 1]:X2},Y",
                        AddrMode.IZX => $"(${prgData[index + 1]:X2},X)",
                        AddrMode.IZY => $"(${prgData[index + 1]:X2}),Y",
                        AddrMode.ABS => $"${(prgData[index + 1] | (prgData[index + 2] << 8)):X4}",
                        AddrMode.ABX => $"${(prgData[index + 1] | (prgData[index + 2] << 8)):X4},X",
                        AddrMode.ABY => $"${(prgData[index + 1] | (prgData[index + 2] << 8)):X4},Y",
                        AddrMode.IND => $"(${(prgData[index + 1] | (prgData[index + 2] << 8)):X4})",
                        AddrMode.REL => $"${(pc + 2 + (sbyte)prgData[index + 1]) & 0xFFFF:X4}",
                        _ => ""
                    };
                }

                sb.AppendLine($"${pc:X4}:  {hexBytes}   {mnemonic,-5} {operand}");

                index += instrLen;
                pc = (pc + instrLen) & 0xFFFF;
                count++;
            }

            if (index < prgData.Length)
            {
                sb.AppendLine($"\r\n[... {prgData.Length - index} more bytes hidden (max {maxInstructions} instructions) ...]");
            }

            return sb.ToString();
        }

        public static DiskFileItem LoadStandalonePcFile(string filePath)
        {
            byte[] fileBytes = ReadAllBytesShared(filePath);
            string ext = Path.GetExtension(filePath).ToUpperInvariant();
            string type = "PRG";
            string name = Path.GetFileName(filePath);
            byte[] rawName = Array.Empty<byte>();
            int recordLength = 0;

            // PC64 .P00/.S00/.U00/.R00 konténer: "C64File\0" + 16 bájt név + 0 + rekordhossz = 26 bájt fejléc
            bool isP00 = fileBytes.Length >= 26
                      && fileBytes[0] == (byte)'C' && fileBytes[1] == (byte)'6' && fileBytes[2] == (byte)'4'
                      && fileBytes[3] == (byte)'F' && fileBytes[4] == (byte)'i' && fileBytes[5] == (byte)'l'
                      && fileBytes[6] == (byte)'e' && fileBytes[7] == 0x00;

            if (isP00)
            {
                rawName = new byte[16];
                Array.Copy(fileBytes, 8, rawName, 0, 16);
                for (int i = 0; i < rawName.Length; i++)
                {
                    if (rawName[i] == 0x00) rawName[i] = 0xA0;
                }

                string parsedName = CbmBytesToString(rawName);
                if (!string.IsNullOrEmpty(parsedName)) name = parsedName;

                char kind = ext.Length >= 2 ? ext[1] : 'P';
                type = kind switch
                {
                    'S' => "SEQ",
                    'U' => "USR",
                    'R' => "REL",
                    _ => "PRG"
                };
                if (type == "REL") recordLength = fileBytes[25];

                byte[] stripped = new byte[fileBytes.Length - 26];
                Array.Copy(fileBytes, 26, stripped, 0, stripped.Length);
                fileBytes = stripped;
            }
            else
            {
                if (ext == ".SEQ") type = "SEQ";
                else if (ext == ".USR") type = "USR";
                else if (ext == ".REL") type = "REL";
            }

            int startAddr = 0;
            int endAddr = 0;
            if (fileBytes.Length >= 2)
            {
                startAddr = fileBytes[0] | (fileBytes[1] << 8);
                endAddr = startAddr + fileBytes.Length - 3;
            }

            int blocks = Math.Max(1, (int)Math.Ceiling(fileBytes.Length / 254.0));

            return new DiskFileItem
            {
                Name = name,
                RawNameBytes = rawName,
                Type = type,
                StartAddress = startAddr,
                EndAddress = endAddr,
                SizeInBytes = fileBytes.Length,
                BlockSize = blocks,
                RecordLength = recordLength,
                RawData = fileBytes,
                IsVirtual = false
            };
        }

        public static DiskHeaderInfo ReadD64Header(string filePath)
        {
            byte[] d64Data = ReadAllBytesShared(filePath);
            int bamOffset = 91392;

            var header = new DiskHeaderInfo
            {
                IsT64 = false,
                TotalBlocks = 664
            };

            if (d64Data.Length < bamOffset + 256)
            {
                header.DiskName = Path.GetFileNameWithoutExtension(filePath);
                return header;
            }

            byte[] nameBytes = new byte[16];
            Array.Copy(d64Data, bamOffset + 0x90, nameBytes, 0, 16);
            header.RawDiskNameBytes = nameBytes;
            header.DiskName = CbmBytesToString(nameBytes);

            byte[] idBytes = new byte[2];
            Array.Copy(d64Data, bamOffset + 0xA2, idBytes, 0, 2);
            header.DiskId = CbmBytesToString(idBytes);

            byte[] dosBytes = new byte[2];
            Array.Copy(d64Data, bamOffset + 0xA5, dosBytes, 0, 2);
            header.DosType = CbmBytesToString(dosBytes);

            int freeBlocks = 0;
            for (int track = 1; track <= 35; track++)
            {
                if (track == 18) continue;
                int bamEntryOffset = bamOffset + (track * 4);
                freeBlocks += d64Data[bamEntryOffset];
            }

            header.FreeBlocks = freeBlocks;
            return header;
        }

        public static DiskHeaderInfo ReadT64Header(string filePath)
        {
            byte[] t64Data = ReadAllBytesShared(filePath);
            var header = new DiskHeaderInfo
            {
                IsT64 = true,
                DiskId = "T64",
                DosType = "TAPE"
            };

            if (t64Data.Length >= 64)
            {
                byte[] tapeNameBytes = new byte[24];
                Array.Copy(t64Data, 0x28, tapeNameBytes, 0, 24);
                header.RawDiskNameBytes = tapeNameBytes;
                header.DiskName = CbmBytesToString(tapeNameBytes);
            }

            if (string.IsNullOrEmpty(header.DiskName))
            {
                header.DiskName = Path.GetFileNameWithoutExtension(filePath);
            }

            return header;
        }

        public static List<DiskFileItem> ReadD64File(string filePath)
        {
            var files = new List<DiskFileItem>();
            byte[] d64Data = ReadAllBytesShared(filePath);

            int dirTrack18Offset = 91392;
            if (d64Data.Length < dirTrack18Offset + 256)
            {
                throw new InvalidDataException("Invalid or truncated D64 disk image.");
            }

            // 35 sávos (174848 / 175531 bájt) és 40 sávos (196608 / 197376 bájt) képek
            int maxTrack = d64Data.Length >= 196608 ? 40 : 35;

            byte currentTrack = d64Data[dirTrack18Offset];
            byte currentSector = d64Data[dirTrack18Offset + 1];
            var visited = new HashSet<(int, int)>();

            while (currentTrack != 0 && currentTrack <= 35 && visited.Add((currentTrack, currentSector)))
            {
                int sectorOffset = GetD64Offset(currentTrack, currentSector);
                if (sectorOffset < 0 || sectorOffset + 256 > d64Data.Length) break;

                byte nextTrack = d64Data[sectorOffset];
                byte nextSector = d64Data[sectorOffset + 1];

                for (int i = 0; i < 8; i++)
                {
                    int entryOffset = sectorOffset + (i * 32);
                    if (entryOffset + 32 > d64Data.Length) break;

                    byte fileTypeRaw = d64Data[entryOffset + 2];
                    if (fileTypeRaw == 0x00) continue;

                    string typeStr = GetFileTypeString(fileTypeRaw);
                    byte startTrack = d64Data[entryOffset + 3];
                    byte startSector = d64Data[entryOffset + 4];
                    byte recordLength = typeStr.StartsWith("REL") ? d64Data[entryOffset + 23] : (byte)0;

                    byte[] nameBytes = new byte[16];
                    Array.Copy(d64Data, entryOffset + 5, nameBytes, 0, 16);
                    string fileName = CbmBytesToString(nameBytes);

                    int dirBlockCount = d64Data[entryOffset + 30] | (d64Data[entryOffset + 31] << 8);

                    byte[] fileData = ReadFileChain(d64Data, startTrack, startSector, maxTrack);

                    int startAddr = 0;
                    int endAddr = 0;
                    if (fileData.Length >= 2)
                    {
                        startAddr = fileData[0] | (fileData[1] << 8);
                        endAddr = startAddr + fileData.Length - 3;
                    }

                    int blocks = dirBlockCount > 0
                        ? dirBlockCount
                        : Math.Max(1, (int)Math.Ceiling(fileData.Length / 254.0));

                    files.Add(new DiskFileItem
                    {
                        Name = fileName,
                        RawNameBytes = nameBytes,
                        Type = typeStr,
                        StartAddress = startAddr,
                        EndAddress = endAddr,
                        SizeInBytes = fileData.Length,
                        BlockSize = blocks,
                        RecordLength = recordLength,
                        RawData = fileData,
                        IsVirtual = true
                    });
                }

                currentTrack = nextTrack;
                currentSector = nextSector;
            }

            return files;
        }

        /// <summary>
        /// Azoknak a T64 bejegyzéseknek az offszetjei (a fájlon belül), amelyeket a ReadT64File listába vesz:
        /// nem üres típus és érvényes adat-offszet. A törlés/átnevezés ugyanezt az indexelést használja.
        /// </summary>
        private static List<int> GetValidT64EntryOffsets(byte[] t64Data)
        {
            var result = new List<int>();
            if (t64Data.Length < 64) return result;

            int maxEntries = t64Data[0x22] | (t64Data[0x23] << 8);
            int usedEntries = t64Data[0x24] | (t64Data[0x25] << 8);
            int entryCount = Math.Max(maxEntries, usedEntries);

            for (int i = 0; i < entryCount; i++)
            {
                int entryOffset = 64 + (i * 32);
                if (entryOffset + 32 > t64Data.Length) break;

                if (t64Data[entryOffset] == 0x00) continue;

                int dataOffset = t64Data[entryOffset + 8]
                               | (t64Data[entryOffset + 9] << 8)
                               | (t64Data[entryOffset + 10] << 16)
                               | (t64Data[entryOffset + 11] << 24);
                if (dataOffset <= 0 || dataOffset >= t64Data.Length) continue;

                result.Add(entryOffset);
            }

            return result;
        }

        public static List<DiskFileItem> ReadT64File(string filePath)
        {
            var files = new List<DiskFileItem>();
            byte[] t64Data = ReadAllBytesShared(filePath);

            if (t64Data.Length < 64)
            {
                throw new InvalidDataException("Invalid or truncated T64 tape image.");
            }

            List<int> entryOffsets = GetValidT64EntryOffsets(t64Data);

            // Az összes adat-offszet, hogy hibás végcímnél a következő bejegyzés kezdetéig (vagy a fájl végéig) olvashassunk.
            var allDataOffsets = new List<int>();
            foreach (int eo in entryOffsets)
            {
                allDataOffsets.Add(t64Data[eo + 8]
                                 | (t64Data[eo + 9] << 8)
                                 | (t64Data[eo + 10] << 16)
                                 | (t64Data[eo + 11] << 24));
            }
            allDataOffsets.Sort();

            for (int n = 0; n < entryOffsets.Count; n++)
            {
                int entryOffset = entryOffsets[n];

                byte cbmFileType = t64Data[entryOffset + 1];
                string typeStr = (cbmFileType & 0x07) switch
                {
                    1 => "SEQ",
                    2 => "PRG",
                    3 => "USR",
                    4 => "REL",
                    _ => "PRG"
                };

                int startAddr = t64Data[entryOffset + 2] | (t64Data[entryOffset + 3] << 8);
                int endAddr = t64Data[entryOffset + 4] | (t64Data[entryOffset + 5] << 8);
                int dataOffset = t64Data[entryOffset + 8]
                               | (t64Data[entryOffset + 9] << 8)
                               | (t64Data[entryOffset + 10] << 16)
                               | (t64Data[entryOffset + 11] << 24);

                byte[] nameBytes = new byte[16];
                Array.Copy(t64Data, entryOffset + 16, nameBytes, 0, 16);
                string fileName = CbmBytesToString(nameBytes);
                if (string.IsNullOrEmpty(fileName)) fileName = $"TAPE_FILE_{n + 1}";

                int payloadLength = endAddr - startAddr;
                if (payloadLength <= 0)
                {
                    // hibás/nulla végcím: a következő bejegyzés adatáig vagy a fájl végéig
                    int nextOffset = t64Data.Length;
                    foreach (int o in allDataOffsets)
                    {
                        if (o > dataOffset)
                        {
                            nextOffset = o;
                            break;
                        }
                    }
                    payloadLength = nextOffset - dataOffset;
                }

                if (dataOffset + payloadLength > t64Data.Length)
                {
                    payloadLength = t64Data.Length - dataOffset;
                }
                if (payloadLength < 0) payloadLength = 0;

                byte[] prgData = new byte[payloadLength + 2];
                prgData[0] = (byte)(startAddr & 0xFF);
                prgData[1] = (byte)((startAddr >> 8) & 0xFF);
                Array.Copy(t64Data, dataOffset, prgData, 2, payloadLength);

                int blocks = Math.Max(1, (int)Math.Ceiling(prgData.Length / 254.0));

                files.Add(new DiskFileItem
                {
                    Name = fileName,
                    RawNameBytes = nameBytes,
                    Type = typeStr,
                    StartAddress = startAddr,
                    EndAddress = startAddr + payloadLength - 1,
                    SizeInBytes = prgData.Length,
                    BlockSize = blocks,
                    RawData = prgData,
                    IsVirtual = true
                });
            }

            return files;
        }

        private static int GetD64Offset(int track, int sector)
        {
            int sectorsBefore = 0;
            for (int t = 1; t < track; t++)
            {
                sectorsBefore += GetSectorsPerTrack(t);
            }
            return (sectorsBefore + sector) * 256;
        }

        private static byte[] ReadFileChain(byte[] d64, byte track, byte sector, int maxTrack = 35)
        {
            var buffer = new List<byte>();
            var visited = new HashSet<(int, int)>();

            while (track != 0 && track <= maxTrack && visited.Add((track, sector)))
            {
                int offset = GetD64Offset(track, sector);
                if (offset < 0 || offset + 256 > d64.Length) break;

                byte nextTrack = d64[offset];
                byte nextSector = d64[offset + 1];
                int bytesToRead = (nextTrack == 0) ? nextSector + 1 : 256;

                for (int i = 2; i < bytesToRead && (offset + i) < d64.Length; i++)
                {
                    buffer.Add(d64[offset + i]);
                }

                track = nextTrack;
                sector = nextSector;
            }
            return buffer.ToArray();
        }

        private static string GetFileTypeString(byte b)
        {
            string type = (b & 0x07) switch
            {
                0 => "DEL",
                1 => "SEQ",
                2 => "PRG",
                3 => "USR",
                4 => "REL",
                _ => "INV"
            };
            if ((b & 0x80) == 0) type += "*";
            return type;
        }

        /// <summary>
        /// CBM név bájtokból szöveg. A záró $A0/$00/$20 bájtokat levágja, a név belsejében lévő
        /// $A0 (shiftelt szóköz) és $00 szóközként jelenik meg, a vezető szóközök megmaradnak.
        /// </summary>
        private static string CbmBytesToString(byte[] bytes)
        {
            int last = bytes.Length - 1;
            while (last >= 0 && (bytes[last] == 0xA0 || bytes[last] == 0x00 || bytes[last] == 0x20))
            {
                last--;
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i <= last; i++)
            {
                byte b = bytes[i];
                if (b == 0xA0 || b == 0x00)
                {
                    sb.Append(' ');
                    continue;
                }

                char c = (char)b;
                if (b >= 0xC1 && b <= 0xDA) c = (char)(b - 0x80);
                sb.Append(c);
            }
            return sb.ToString();
        }

        public static string DetokenizeBasic(byte[] prgData, bool useCbmPua = false)
        {
            if (prgData.Length < 4) return "Not a BASIC program (or file too short).";

            StringBuilder sb = new StringBuilder();

            int index = 2;
            if (prgData.Length > 2 && prgData[2] == 0x00 && (prgData[0] == 0x00 && prgData[1] == 0x40))
            {
                index = 3;
            }

            while (index < prgData.Length - 1)
            {
                int nextLinePtr = prgData[index] | (prgData[index + 1] << 8);
                if (nextLinePtr == 0 || index + 4 >= prgData.Length) break;

                int lineNumber = prgData[index + 2] | (prgData[index + 3] << 8);
                index += 4;

                sb.Append(lineNumber).Append(" ");
                bool insideQuotes = false;
                bool insideRem = false;
                bool insideData = false;

                while (index < prgData.Length && prgData[index] != 0)
                {
                    byte b = prgData[index];

                    if (b == 0x22)
                    {
                        insideQuotes = !insideQuotes;
                        sb.Append('"');
                    }
                    else if (!insideQuotes && !insideRem && !insideData
                             && BasicTokens.TryGetValue(b, out string? token))
                    {
                        if (b == 0x8F) insideRem = true;   // REM
                        if (b == 0x83) insideData = true;  // DATA

                        sb.Append(token);
                    }
                    else
                    {
                        // a DATA utasítás a következő kettőspontig (idézőjelen kívül) ér véget
                        if (b == (byte)':' && !insideQuotes) insideData = false;

                        if (useCbmPua)
                        {
                            sb.Append(char.ConvertFromUtf32(0xE000 + b));
                        }
                        else
                        {
                            if (b >= 32 && b <= 126)
                            {
                                sb.Append((char)b);
                            }
                            else
                            {
                                sb.Append($"[{b:X2}]");
                            }
                        }
                    }
                    index++;
                }
                sb.AppendLine();
                index++;
            }

            return sb.ToString();
        }

        public static Bitmap RenderFontBitmap(byte[] rawData, int customByteShift = 0)
        {
            int baseOffset = (rawData.Length >= 2050) ? 2 : 0;
            int dataOffset = Math.Max(0, baseOffset + customByteShift);

            int charCount = 256;
            int cols = 16;
            int rows = 16;
            int scale = 3;

            Bitmap bmp = new Bitmap(cols * 8 * scale, rows * 8 * scale);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(20, 20, 30));

                for (int c = 0; c < charCount; c++)
                {
                    int charDataOffset = dataOffset + (c * 8);
                    if (charDataOffset + 8 > rawData.Length) break;

                    int col = c % cols;
                    int row = c / cols;

                    for (int y = 0; y < 8; y++)
                    {
                        byte b = rawData[charDataOffset + y];
                        for (int x = 0; x < 8; x++)
                        {
                            bool pixelOn = (b & (1 << (7 - x))) != 0;
                            if (pixelOn)
                            {
                                g.FillRectangle(Brushes.SpringGreen,
                                    (col * 8 + x) * scale,
                                    (row * 8 + y) * scale,
                                    scale, scale);
                            }
                        }
                    }
                }
            }
            return bmp;
        }

        public static void RenameFileInD64(string filePath, int activeFileIndex, string newName)
        {
            byte[] d64 = ReadAllBytesShared(filePath);
            int bamOffset = 91392;
            byte currentTrack = d64[bamOffset];
            byte currentSector = d64[bamOffset + 1];

            string cleanNewName = (newName ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(cleanNewName))
                throw new ArgumentException("File name cannot be empty.");
            if (cleanNewName.Length > 16)
                cleanNewName = cleanNewName.Substring(0, 16);

            int currentActiveIdx = 0;
            var visited = new HashSet<(int, int)>();

            while (currentTrack != 0 && currentTrack <= 35 && visited.Add((currentTrack, currentSector)))
            {
                int sectorOffset = GetD64Offset(currentTrack, currentSector);
                if (sectorOffset < 0 || sectorOffset + 256 > d64.Length) break;

                byte nextTrack = d64[sectorOffset];
                byte nextSector = d64[sectorOffset + 1];

                for (int i = 0; i < 8; i++)
                {
                    int entryOffset = sectorOffset + (i * 32);
                    if (entryOffset + 32 > d64.Length) break;

                    byte fileTypeRaw = d64[entryOffset + 2];
                    if (fileTypeRaw == 0x00) continue;

                    if (currentActiveIdx == activeFileIndex)
                    {
                        byte[] encodedName = EncodeCbmName(cleanNewName, 16, 0xA0);
                        Array.Copy(encodedName, 0, d64, entryOffset + 5, 16);
                        File.WriteAllBytes(filePath, d64);
                        return;
                    }

                    currentActiveIdx++;
                }

                currentTrack = nextTrack;
                currentSector = nextSector;
            }

            throw new InvalidOperationException("Selected file entry was not found in the D64 directory.");
        }

        public static void RenameFileInT64(string filePath, int activeFileIndex, string newName)
        {
            byte[] t64 = ReadAllBytesShared(filePath);
            List<int> entryOffsets = GetValidT64EntryOffsets(t64);

            if (activeFileIndex < 0 || activeFileIndex >= entryOffsets.Count)
            {
                throw new InvalidOperationException("Selected file entry was not found in the T64 tape image.");
            }

            string cleanNewName = (newName ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(cleanNewName))
                throw new ArgumentException("File name cannot be empty.");
            if (cleanNewName.Length > 16)
                cleanNewName = cleanNewName.Substring(0, 16);

            // Helyben írjuk át a bejegyzés 16-31. bájtjait, a többi bejegyzés érintetlen marad.
            int entryOffset = entryOffsets[activeFileIndex];
            byte[] encodedName = EncodeCbmName(cleanNewName, 16, 0x20);
            Array.Copy(encodedName, 0, t64, entryOffset + 16, 16);

            File.WriteAllBytes(filePath, t64);
        }

        public static void RenameD64Header(string filePath, string newDiskName, string? newDiskId = null)
        {
            byte[] d64 = ReadAllBytesShared(filePath);
            int bamOffset = 91392;

            if (d64.Length < bamOffset + 256)
            {
                throw new InvalidDataException("Invalid or truncated D64 disk image.");
            }

            byte[] nameBytes = EncodeCbmName(newDiskName ?? string.Empty, 16, 0xA0);
            Array.Copy(nameBytes, 0, d64, bamOffset + 0x90, 16);

            if (newDiskId != null)
            {
                string cleanId = newDiskId.Trim();
                if (!string.IsNullOrEmpty(cleanId))
                {
                    byte[] idBytes = EncodeCbmName(cleanId, 2, 0xA0);
                    d64[bamOffset + 0xA2] = idBytes[0];
                    d64[bamOffset + 0xA3] = idBytes[1];
                }
            }

            File.WriteAllBytes(filePath, d64);
        }

        public static void RenameT64Header(string filePath, string newTapeName)
        {
            byte[] t64 = ReadAllBytesShared(filePath);

            if (t64.Length < 64)
            {
                throw new InvalidDataException("Invalid or truncated T64 tape image.");
            }

            byte[] tapeNameBytes = EncodeCbmName(newTapeName ?? string.Empty, 24, 0x20);
            Array.Copy(tapeNameBytes, 0, t64, 0x28, 24);

            File.WriteAllBytes(filePath, t64);
        }
    }
}
