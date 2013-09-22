﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ID3Lite
{
    public enum Revision
    {
        Rev0,
        Rev1
    }

    public enum DataType
    {
        Title,
        Artist,
        Album,
        Year,
        Comment,
        Track,
        Genre,
    }

    public class Version1
    {
        private string filePath;
        public Version1(string FilePath)
        {
            filePath = FilePath;
        }

        public v1Data Read()
        {
            v1Data tagData = new v1Data();
            using (FileStream fs = File.OpenRead(filePath))
            {
                Byte[] versionChecker = new byte[1];

                fs.Seek(-3, SeekOrigin.End);

                fs.Read(versionChecker, 0, 1);
                fs.Seek(-128, SeekOrigin.End);

                dynamic tag;

                if (versionChecker[0] != 0x00)
                {
                    tag = new v1TagData();

                }
                else
                {
                    tag = new v11TagData();
                }

                fs.Read(tag.Header, 0, tag.Header.Length);
                string theTAGID = Encoding.Default.GetString(tag.Header);
                if (theTAGID.Equals("TAG"))
                {
                    fs.Read(tag.Title, 0, tag.Title.Length);
                    fs.Read(tag.Artist, 0, tag.Artist.Length);
                    fs.Read(tag.Album, 0, tag.Album.Length);
                    fs.Read(tag.Year, 0, tag.Year.Length);
                    
                    fs.Read(tag.Comment, 0, tag.Comment.Length);
                    if (tag.GetType() == typeof(v11TagData))
                    {
                        fs.Read(tag.Separator, 0, tag.Separator.Length);
                        fs.Read(tag.Track, 0, tag.Track.Length);
                        if (tag.Track[0] != 0x00)
                        {
                            tagData.Track = tag.Track[0].ToString();
                        }
                    }
                    fs.Read(tag.Genre, 0, tag.Genre.Length);
                    
                    tagData.Title = Encoding.Default.GetString(RemoveNullBits(tag.Title));
                    tagData.Artist = Encoding.Default.GetString(RemoveNullBits(tag.Artist));
                    tagData.Album = Encoding.Default.GetString(RemoveNullBits(tag.Album));
                    tagData.Year = Encoding.Default.GetString(RemoveNullBits(tag.Year));
                    tagData.Comment = Encoding.Default.GetString(RemoveNullBits(tag.Comment));
                    
                    if (tag.Genre[0] != 0xff)
                    {
                        tagData.Genre = tag.Genre[0].ToString();
                    }
                }

            }
            return tagData;
        }



        public bool Write(Revision Revision, DataType dataType, string Value)
        {
            bool result = true;
            try
            {
                using (FileStream fs = File.OpenWrite(filePath))
                {
                    int offset = getStartOffset(Revision, dataType);

                    if (Revision == Revision.Rev1 && dataType == DataType.Track)
                    {
                        fs.Seek(offset - 1, SeekOrigin.End);
                        fs.WriteByte(0x00); //for v1.1 tag Separation

                        if (String.IsNullOrEmpty(Value))
                        {
                            fs.WriteByte(0x00);
                        }
                        else
                        {
                            byte[] intBytes = BitConverter.GetBytes(Convert.ToInt32(Value));
                            fs.WriteByte(intBytes[0]);
                        }

                    }
                    else
                    {
                        byte[] data = Encoding.Default.GetBytes(Value);
                        int length = getDataSize(Revision, dataType);

                        fs.Seek(offset, SeekOrigin.End);
                        for (int i = 0; i < length; i++)
                        {
                            if (i >= data.Length) fs.WriteByte(0x00);
                            else fs.WriteByte(data[i]);
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                result = false;
            }

            return result;
        }

        private byte[] RemoveNullBits(byte[] source)
        {
            int i = source.Length - 1;
            
            while (source[i] == 0)
            {
                if (i == 0) break;
                --i;
            }

            byte[] tmp = new byte[i + 1];
            Array.Copy(source, tmp, i + 1);

            return tmp;
        }

        private int getStartOffset(Revision Rev, DataType dataType)
        {
            int offset = -128;
            if (dataType == DataType.Title)
                offset += 3;

            else if (dataType == DataType.Artist)
                offset += 33;

            else if (dataType == DataType.Album)
                offset += 63;

            else if (dataType == DataType.Year)
                offset += 93;

            else if (dataType == DataType.Comment)
                offset += 97;

            else if (Rev == Revision.Rev1 && dataType == DataType.Track)
                offset += 126;

            else if (dataType == DataType.Genre)
                offset += 127;

            return offset;
        }

        private int getDataSize(Revision Rev, DataType dataType)
        {
            int length = 0;

            if (dataType == DataType.Title ||
                dataType == DataType.Artist ||
                dataType == DataType.Album ||
                (Rev == Revision.Rev0 && dataType == DataType.Comment))
                length = 30;
            else if (dataType == DataType.Year)
                length = 4;
            else if (Rev == Revision.Rev1 && dataType == DataType.Comment)
                length = 28;
            else if ((Rev == Revision.Rev1 && dataType == DataType.Track) || dataType == DataType.Genre)
                length = 1;

            return length;
        }
    }
}