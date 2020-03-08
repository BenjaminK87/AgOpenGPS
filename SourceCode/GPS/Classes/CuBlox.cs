//Please, if you use this, share the improvements

using System;
using System.Globalization;
using System.Text;

namespace AgOpenGPS
{
    public class CUBlox
    {
        public bool updatedRelPosNED, updatedPVT;

        public byte[] rawBuffer = new byte[0];

        private readonly FormGPS mf;

        struct NAV_RELPOSNED
        {
            public byte ver;
            public ushort refStID;
            public ulong iTOW;
            public long relPosN;
            public long relPosE;
            public long relPosD;
            public long relPosLength;
            public long relPosHeading;
            public sbyte relPosHPN;
            public sbyte relPosHPE;
            public sbyte relPosHPD;
            public sbyte relPosHPLength;
            public ulong accN;
            public ulong accE;
            public ulong accD;
            public ulong accLength;
            public ulong accHeading;
            public ulong flags;
        };
        NAV_RELPOSNED UBXRelPosNED;

        struct NAV_PVT
        {
            public ulong iTow;
            public ushort year;
            public byte month;
            public byte day;
            public byte hour;
            public byte min;
            public byte sec;
            public byte valid;
            public ulong tAcc;
            public long nano;
            public byte fixType;
            public byte flags;
            public byte flags2;
            public byte numSV;
            public long lon;
            public long lat;
            public long height;
            public long hMSL;
            public ulong hAcc;
            public ulong vAcc;
            public long velN;
            public long velE;
            public long velD;
            public long gSpeed;
            public long headMot;
            public ulong sAcc;
            public ulong headAcc;
            public ushort pDOP;
            public byte flags3;
            public long headVeh;
            public short magDec;
            public ushort magAcc;
        }
        NAV_PVT UBXPVT;

        public CUBlox(FormGPS f)
        {
            //constructor, grab the main form reference
            mf = f;
        }

        private int Search(byte[] src, byte[] pattern)
        {
            int c = src.Length - pattern.Length + 1;
            int j;
            for (int i = 0; i < c; i++)
            {
                if (src[i] != pattern[0]) continue;
                for (j = pattern.Length - 1; j >= 1 && src[i + j] == pattern[j]; j--) ;
                if (j == 0) return i;
            }
            return -1;
        }

        public void parseUBX()
        {
            if (rawBuffer == null) return;

            //search beginning of ubx frame (Hex B5, Hex 62)
            byte[] pattern = new byte[] {181, 98};
            int ubxStart = Search(rawBuffer, pattern);

            //int ubxStart = rawBuffer.IndexOf("\u00B5\u0062");
            if (ubxStart > -1  & rawBuffer.Length > 8) //minimum Frame Size with only one byte Payload
            {
                //Console.WriteLine("Starting new Parse");
                //Console.Write("UBX in: ");
                //Console.WriteLine(ubxStart);

                //Console.Write("Length: "); Console.WriteLine(rawBuffer.Length);
                //Console.WriteLine("Input Array");
                //for (int i = 0; i < rawBuffer.Length; i++)
                //{
                //    Console.Write(rawBuffer[i]);
                //    Console.Write(" ");
                //}
                //Console.Write("\n");

                System.Buffer.BlockCopy(rawBuffer, ubxStart, rawBuffer, 0, rawBuffer.Length - ubxStart);
                //resize rawBuffer to new size
                Array.Resize<byte>(ref rawBuffer, rawBuffer.Length - ubxStart);

                //Console.Write("Length: "); Console.WriteLine(rawBuffer.Length);
                //Console.WriteLine("After Aligning Start of Sentence Array");
                //for (int i = 0; i < rawBuffer.Length; i++)
                //{
                //    Console.Write(rawBuffer[i]);
                //    Console.Write(" ");
                //}
                //Console.Write("\n");

                int ubxClass = Convert.ToByte(rawBuffer[2]);
                int ubxID = Convert.ToByte(rawBuffer[3]);
                byte[] ubxLengtArray = new byte[2];
                ubxLengtArray[0] = Convert.ToByte(rawBuffer[4]);
                ubxLengtArray[1] = Convert.ToByte(rawBuffer[5]);
                int ubxLength = BitConverter.ToInt16(ubxLengtArray, 0);

                //Console.Write("Class: ");
                //Console.Write(ubxClass);
                //Console.Write(" ID: ");
                //Console.Write(ubxID);
                //Console.Write(" Length: ");
                //Console.Write(ubxLength);
                //Console.WriteLine("");

                
                if (ValidateChecksum(ubxLength))
                {
                    
                    if(ubxClass == 1 & ubxID == 7) //UBX-NAV-PVT
                    {
                        ParsePVT();
                    }
                    else if (ubxClass == 1 & ubxID == 60) //UBX-NAV-RELPOSNED - Hex 3c
                    {
                        ParseRelPosNed();
                    }
                }

                // "Delete processed data from rawBuffer
                int totalFrameLength = 1 + 1 + 1 + 1 + 2 + ubxLength + 2;
                ////rawBuffer = rawBuffer.Substring(1 + 1 + 1 + 1 + 2 + ubxLength + 2); //1xHeader, 1xHeader, 1xClass, 1xID, 2x Length, 2x Checksum
                if (rawBuffer.Length > totalFrameLength)
                {
                    System.Buffer.BlockCopy(rawBuffer, totalFrameLength, rawBuffer, 0, rawBuffer.Length - totalFrameLength);
                }
                //System.Buffer.BlockCopy(test, 0, rawBuffer, 0, 2);
                //resize rawBuffer to new size
                int newArraySize = rawBuffer.Length - totalFrameLength;
                Array.Resize<byte>(ref rawBuffer, newArraySize);

                //Console.Write("Length: "); Console.WriteLine(rawBuffer.Length);
                //Console.WriteLine("Cutting Processed Parts");
                //for (int i = 0; i < rawBuffer.Length; i++)
                //{
                //    Console.Write(rawBuffer[i]);
                //    Console.Write(" ");
                //}
                //Console.Write("\n");

                //Console.WriteLine("End of Parse");
            }
            
        }

        private void ParsePVT()
        {
            byte frameOffset = 6;
            UBXPVT.iTow = BitConverter.ToUInt32(rawBuffer, frameOffset + 0);
            UBXPVT.year = BitConverter.ToUInt16(rawBuffer, frameOffset + 4);
            UBXPVT.month = rawBuffer[frameOffset + 6];
            UBXPVT.day = rawBuffer[frameOffset + 7];
            UBXPVT.hour = rawBuffer[frameOffset + 8];
            UBXPVT.min = rawBuffer[frameOffset + 9];
            UBXPVT.sec = rawBuffer[frameOffset + 10];
            UBXPVT.valid = rawBuffer[frameOffset + 11];
            UBXPVT.tAcc = BitConverter.ToUInt32(rawBuffer, frameOffset + 12);
            UBXPVT.nano = BitConverter.ToInt32(rawBuffer, frameOffset + 16);
            UBXPVT.fixType = rawBuffer[frameOffset + 20];
            UBXPVT.flags = rawBuffer[frameOffset + 21];
            UBXPVT.flags2 = rawBuffer[frameOffset + 22];
            UBXPVT.numSV = rawBuffer[frameOffset + 23];
            UBXPVT.lon = BitConverter.ToInt32(rawBuffer, frameOffset + 24);
            UBXPVT.lat = BitConverter.ToInt32(rawBuffer, frameOffset + 28);
            UBXPVT.height = BitConverter.ToInt32(rawBuffer, frameOffset + 32);
            UBXPVT.hMSL = BitConverter.ToInt32(rawBuffer, frameOffset + 36);
            UBXPVT.hAcc = BitConverter.ToUInt32(rawBuffer, frameOffset + 40);
            UBXPVT.vAcc = BitConverter.ToUInt32(rawBuffer, frameOffset + 44);
            UBXPVT.velN = BitConverter.ToInt32(rawBuffer, frameOffset + 48);
            UBXPVT.velE = BitConverter.ToInt32(rawBuffer, frameOffset + 52);
            UBXPVT.velD = BitConverter.ToInt32(rawBuffer, frameOffset + 56);
            UBXPVT.gSpeed = BitConverter.ToInt32(rawBuffer, frameOffset + 60);
            UBXPVT.headMot = BitConverter.ToInt32(rawBuffer, frameOffset + 64);
            UBXPVT.sAcc = BitConverter.ToUInt32(rawBuffer, frameOffset + 68);
            UBXPVT.headAcc = BitConverter.ToUInt32(rawBuffer, frameOffset + 72);
            UBXPVT.pDOP = BitConverter.ToUInt16(rawBuffer, frameOffset + 76);
            UBXPVT.flags3 = rawBuffer[frameOffset + 78];
            UBXPVT.headVeh = BitConverter.ToUInt32(rawBuffer, frameOffset + 84);
            UBXPVT.magDec = BitConverter.ToInt16(rawBuffer, frameOffset + 88);
            UBXPVT.magAcc = BitConverter.ToUInt16(rawBuffer, frameOffset + 90);

            mf.pn.latitude = UBXPVT.lat * 0.0000001;
            mf.pn.longitude = UBXPVT.lon * 0.0000001;
            mf.pn.satellitesTracked = UBXPVT.numSV;
            mf.pn.hdop = UBXPVT.pDOP;
            mf.pn.altitude = UBXPVT.height * 0.0001;
            mf.pn.speed = UBXPVT.gSpeed * 0.0036;
            mf.pn.headingTrue = UBXPVT.headVeh;

            bool gnssFixOK = GetBit(UBXPVT.flags, 7);
            bool diffSoln = GetBit(UBXPVT.flags, 6);
            bool headVehValid = GetBit(UBXPVT.flags, 3);

            if (UBXPVT.fixType == 3 & GetBit(UBXPVT.flags, 2) == false & GetBit(UBXPVT.flags, 1) == false)
            {
                mf.pn.fixQuality = 1;
            }
            else if (GetBit(UBXPVT.flags, 2) & GetBit(UBXPVT.flags, 1) == false)
            {
                mf.pn.fixQuality = 5;
            }
            else if (GetBit(UBXPVT.flags, 2) & GetBit(UBXPVT.flags, 1))
            {
                mf.pn.fixQuality = 4;
            }
            else if (UBXPVT.fixType == 0)
            {
                mf.pn.fixQuality = 0;
            }

            
            //switch (UBXPVT.fixType)
            //{
            //    case 0:
            //        mf.pn.fixQuality = 0;
            //        break;
            //    case 1:
            //        mf.pn.fixQuality = 0;
            //        break;
            //    case 2:
            //        mf.pn.fixQuality = 1;
            //        break;
            //    case 3:
            //        mf.pn.fixQuality = 1;
            //        break;
            //    case 4:
            //        mf.pn.fixQuality = 0;
            //        break;
            //    case 5:
            //        mf.pn.fixQuality = 0;
            //        break;
            //    default:
            //        mf.pn.fixQuality = 0;
            //        break;

            //}
                       
            mf.pn.UpdateNorthingEasting();

            updatedPVT = true;
            mf.recvCounter = 0;

            //average the speed
            mf.avgSpeed[mf.ringCounter] = mf.pn.speed;
            if (mf.ringCounter++ > 8) mf.ringCounter = 0;
        }

        private bool GetBit(byte b, int bitNumber)
        {
            return ((b & (1 << bitNumber - 1)) != 0);
        }

        private void ParseRelPosNed()
        {
            byte frameOffset = 6;
            UBXRelPosNED.ver = rawBuffer[frameOffset + 0];
            UBXRelPosNED.refStID = BitConverter.ToUInt16(rawBuffer, frameOffset + 2);
            UBXRelPosNED.iTOW = BitConverter.ToUInt32(rawBuffer, frameOffset + 4);
            UBXRelPosNED.relPosN = BitConverter.ToInt32(rawBuffer, frameOffset + 8);
            UBXRelPosNED.relPosE = BitConverter.ToInt32(rawBuffer, frameOffset + 12);
            UBXRelPosNED.relPosD = BitConverter.ToInt32(rawBuffer, frameOffset + 16);
            UBXRelPosNED.relPosLength = BitConverter.ToInt32(rawBuffer, frameOffset + 20);
            UBXRelPosNED.relPosHeading = BitConverter.ToInt32(rawBuffer, frameOffset + 24);
            UBXRelPosNED.relPosHPN = (sbyte)(rawBuffer[frameOffset + 32]);
            UBXRelPosNED.relPosHPE = (sbyte)(rawBuffer[frameOffset + 33]);
            UBXRelPosNED.relPosHPD = (sbyte)(rawBuffer[frameOffset + 34]);
            UBXRelPosNED.relPosHPLength = (sbyte)(rawBuffer[frameOffset + 35]);
            UBXRelPosNED.accN = BitConverter.ToUInt32(rawBuffer, frameOffset + 36);
            UBXRelPosNED.accE = BitConverter.ToUInt32(rawBuffer, frameOffset + 40);
            UBXRelPosNED.accD = BitConverter.ToUInt32(rawBuffer, frameOffset + 44);
            UBXRelPosNED.accLength = BitConverter.ToUInt32(rawBuffer, frameOffset + 48);
            UBXRelPosNED.accHeading = BitConverter.ToUInt32(rawBuffer, frameOffset + 52);

            mf.lblN.Text = (UBXRelPosNED.relPosN / 100.0).ToString("0.00");
            mf.lblE.Text = (UBXRelPosNED.relPosE / 100.0).ToString("0.00");
            mf.lblD.Text = (UBXRelPosNED.relPosD / 100.0).ToString("0.00");
            mf.lblLength.Text = (UBXRelPosNED.relPosLength / 100.0).ToString("0.00");
            mf.lblHeading.Text = (UBXRelPosNED.relPosHeading / 100000.0).ToString("0.00");
            mf.lblNAcc.Text = (UBXRelPosNED.accN / 10000.0).ToString("0.00");
            mf.lblEAcc.Text = (UBXRelPosNED.accE / 10000.0).ToString("0.00");
            mf.lblDAcc.Text = (UBXRelPosNED.accD / 10000.0).ToString("0.00");
            mf.lblLengthAcc.Text = (UBXRelPosNED.accLength / 10000.0).ToString("0.00");
            mf.lblHeadingAcc.Text = (UBXRelPosNED.accHeading / 100000.0).ToString("0.00");

            updatedRelPosNED = true;
            mf.recvCounter = 0;
        }
            //checks the checksum against the string
            public bool ValidateChecksum(int ubxLength)
        {
            byte checksumA = 0;
            byte checksumB = 0;

            try
            {
                
                for (int i = 2; i < ubxLength + 4 + 2; i++)
                {
                    checksumA += rawBuffer[i];
                    checksumB += checksumA;
                }

                if (checksumA == rawBuffer[6 + ubxLength] & checksumB == rawBuffer[6 + ubxLength + 1])
                {
                    //Console.WriteLine("Checksum correct");
                    return true;
                }
                else
                {
                    Console.WriteLine("Checksum incorrect");
                    Console.Write("Checksums: ");
                    Console.Write(checksumA);
                    Console.Write(" ");
                    Console.WriteLine(checksumB);
                    return false;
                }
            }
            catch (Exception e)
            {
                mf.WriteErrorLog("Validate Checksum" + e);
                return false;
            }
        }
    }
}